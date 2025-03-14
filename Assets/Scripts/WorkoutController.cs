using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsible for the guts of the workout simulation
/// </summary>
public class WorkoutController : MonoBehaviour
{
    /// <summary>
    /// How fast the simulation should run at
    /// </summary>
    [SerializeField] private float simulationSecondsPerRealSeconds = 30;

    /// <summary>
    /// How many simulation-seconds should pass before we update people's speeds and such
    /// </summary>
    [SerializeField] private float simulationStep = 60f;

    [Header("Run VO2 Calculation Variables")]
    /// <summary>
    /// The max standard deviation a runner's VO2 can be off from the coach's guidance at the beginning of a run in percentage of that runner's V02.
    /// IE if coach's guidance is .8 and maxDeviation is .1, then at most, the range from -1sigma to 1sigma will be .7-.9
    /// </summary>
    [SerializeField] private float maxDeviation = .1f;
    /// <summary>
    /// The max amount of experience before we consider a runner "fully experienced"
    /// </summary>
    [SerializeField] private float experienceCap = 1000000f;
    /// <summary>
    /// The amount of soreness at which point additional soreness no longer impacts performance
    /// </summary>
    [SerializeField] private float maxSoreness = 500f;
    /// <summary>
    /// The max amount in percentage of a runner's VO2 that soreness will effect
    /// IE if VO2 percent is .8, sorenessEffect is .1, and the runner is half sore, then VO2 will change to .75
    /// </summary>
    [SerializeField] private float sorenessEffect = .1f;

    private int numGroups;
    private Dictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary = new();

    #region Events
    public class StartWorkoutEvent : UnityEvent<StartWorkoutEvent.Context>
    {
        public class Context
        {
            public List<WorkoutGroup> groups;
            public Workout workout;
            public RunConditions runConditions;
        }
    };
    public static StartWorkoutEvent startWorkoutEvent = new ();
    
    public class WorkoutSimulationUpdatedEvent : UnityEvent<WorkoutSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
            public int groupIndex;
        }
    }
    public static WorkoutSimulationUpdatedEvent workoutSimulationUpdatedEvent = new ();

    public class WorkoutSimulationEndedEvent : UnityEvent<WorkoutSimulationEndedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary;
            public WorkoutGroup group;
        }
    }
    public static WorkoutSimulationEndedEvent workoutSimulationEndedEvent = new ();

    #endregion

    private void OnEnable()
    {
        startWorkoutEvent.AddListener(OnStartWorkout);
    }

    private void OnDisable()
    {
        startWorkoutEvent.RemoveListener(OnStartWorkout);
    }

    private void OnStartWorkout(StartWorkoutEvent.Context context)
    {
        int numGroups = context.groups.Count;

        // start a routine for each workout group
        IEnumerator[] groupWorkoutRoutines = new IEnumerator[context.groups.Count];
        for (int i = 0; i < context.groups.Count; i++)
        {
            groupWorkoutRoutines[i] = SimulateWorkoutRoutine(context.groups[i], context.workout, i);
            StartCoroutine(groupWorkoutRoutines[i]);
        }
    }

    /// <summary>
    /// Simulates an entire workout for the given group
    /// </summary>
    /// <param name="group">The group of runners</param>
    /// <param name="workout">The workout to do</param>
    /// <param name="groupIndex">The index of the group in the list. Used to delay the start of the workout</param>
    private IEnumerator SimulateWorkoutRoutine(WorkoutGroup group, Workout workout, int groupIndex)
    {
        // wait a frame for the other starts to get going
        yield return null;

        // space each coroutine/group out by 5 seconds
        yield return new WaitForSeconds(groupIndex * 5);

        // go through each runner and initialize their state for this workout
        Dictionary<Runner, RunnerState> runnerStates = new();
        foreach (Runner runner in group.runners)
        {

            runnerStates.Add(runner, new RunnerState
            {
                runVO2 = 0,
                currentSpeed = 0,
                desiredSpeed = 0,
                totalDistance = 0,
                distanceTimeSimulationIntervalList = new List<(float, float)> { (0, 0) }
            });
        }

        // simulate each interval
        for (int workoutIntervalIndex = 0; workoutIntervalIndex < workout.NumIntervals; workoutIntervalIndex++)
        {
            // reset speed and initial V02 for every runner
            foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                //TODO: this sets the V02 perfectly at the start of each interval bc the other way with rolls was too random
                // but this should probably account for experience and soreness in some way
                state.runVO2 = runner.currentVO2Max * group.targetVO2 / runner.currentVO2Max;
                state.currentSpeed = 0;
                state.desiredSpeed = 0;
                state.workoutIntervalDistance = 0;
            }


            // while all runners have not finished, simulate the run
            while (runnerStates.Values.Any(state => state.workoutIntervalDistance < workout.IntervalLength))
            {
                // first figure out every runner's preferred speed
                foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    // std dev for the roll for how much pace will change in percent of current run VO2
                float paceChangeStdDev = .03f;
                // the base mean for the roll for how much pace will change in percent of current run VO2
                // example: paceMeanMagnitude = .02 and the roll comes up on the mean(disregarding other factors), pace will increase by 2% 
                float paceChangeMeanMagnitude = .02f;

                // a number between 0 and 1 that shows how sore we are, 0 = not sore, 1 = most sore
                float normalizedSorenessFeel = Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, state.shortTermSoreness + runner.longTermSoreness));
                // a smoothed number between -1 and 1 that represents the magnitude and direction of the soreness effect on pace
                // low numbers mean you can speed up and high numbers mean you gotta slow doen
                float sorenessPaceChangeFactor = Mathf.Pow(Mathf.Lerp(-1, 1, normalizedSorenessFeel), 5);

                // number that represents the percentile of the last interval's VO2 usage
                // example: runner VO2 = 50, last interval was at 45 V02 pace, intervalVO2Percent would then be .9
                float intervalVO2Percent = state.lastSimulationIntervalVO2 / runner.currentVO2Max;
                // how far off the last interval was from coach's guidance in percent of VO2
                // low numbers mean you're slow and high numbers mean you're fast
                float vo2PaceChangeFactor = intervalVO2Percent - (group.targetVO2 / runner.currentVO2Max);

                // these if statements check if there are extremes being hit with soreness and pace
                // if there are, then roll to change pace will be affected
                // if both of them are in the middle, then pace change will be somewhat random
                float paceChangeMean = 0;
                // if soreness is high and pace is not too slow, then slow down
                if (sorenessPaceChangeFactor > .1f && vo2PaceChangeFactor > -.05f)
                {
                    // the mean here will be somewhere between -paceChangeMeanMagnitude and -.1 * paceChangeMeanMagnitude
                    paceChangeMean = -sorenessPaceChangeFactor * paceChangeMeanMagnitude;
                }
                // if soreness is low and pace is not too fast, then speed up
                else if (sorenessPaceChangeFactor < -.1f && vo2PaceChangeFactor < .05f)
                {
                    // the mean here will be somewhere between 0 and ~.5 * paceChangeMeanMagnitude
                    paceChangeMean = (-vo2PaceChangeFactor + .05f) * paceChangeMeanMagnitude;
                }

                // do the roll then adjust vo2
                float roll = CNExtensions.RandGaussian(paceChangeMean, paceChangeStdDev);
                state.runVO2 += roll * runner.currentVO2Max;

                // clamp the vo2 between some reasonable values
                state.runVO2 = Mathf.Clamp(state.runVO2, .5f * runner.currentVO2Max, 1.25f * runner.currentVO2Max);

                state.desiredSpeed = RunUtility.CaclulateSpeedFromOxygenCost(state.runVO2 * runner.CalculateRunEconomy(state));
                }

                // now that we have everyone's desired speed, we use a gravity model to group people
                int numGravityIterations = 2;
                for (int i = 0; i < numGravityIterations; i++)
                {
                    foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                    {
                        Runner runner = kvp.Key;
                        RunnerState state = kvp.Value;

                        // if this runner is done, continue
                        if (state.workoutIntervalDistance >= workout.IntervalLength)
                        {
                            continue;
                        }

                        float runningAverage = 0f;
                        float weightTotal = 0f;
                        // go through each runner and add to the running average + total
                        foreach (KeyValuePair<Runner, RunnerState> otherKvp in runnerStates)
                        {
                            // skip if current==other or if this runner is already done running
                            if (kvp.Key == otherKvp.Key || otherKvp.Value.workoutIntervalDistance >= workout.IntervalLength)
                            {
                                continue;
                            }

                            // use a gravity model so runners closer together effect each other more than runners far away
                            float difference = Mathf.Abs(otherKvp.Value.desiredSpeed - state.desiredSpeed) + Mathf.Abs(otherKvp.Value.workoutIntervalDistance - state.workoutIntervalDistance);
                            // if the difference between the runners is super small, cap weight at a high amount
                            float weight = (difference < .001f) ? 1000000f : 1f / Mathf.Max(Mathf.Pow(difference, 2), Mathf.Epsilon);
                            runningAverage += weight * otherKvp.Value.desiredSpeed;
                            weightTotal += weight;
                        }

                        // if we are effected by any runners, figure out how they effect our current speed
                        // the last runner left on the route will not be effected by anyone so they just run at their desired speed
                        if (weightTotal > 0)
                        {
                            state.desiredSpeed = Mathf.Lerp(state.desiredSpeed, runningAverage / weightTotal, 1f - (.75f * group.targetVO2 / runner.currentVO2Max));
                        }

                        // if this is the last iteration, set the current speed
                        if (i == numGravityIterations - 1)
                        {
                            state.currentSpeed = state.desiredSpeed;
                        }
                    }
                }

                //then spend a second simulating before moving on to the next iteration
                float simulationTime = simulationStep;
                while (simulationTime > 0)
                {
                    string stateString = "";
                    float timePassed = simulationSecondsPerRealSeconds * Time.deltaTime;
                    foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                    {
                        Runner runner = kvp.Key;
                        RunnerState state = kvp.Value;

                        //if a runner is not done, keep incrementing them along the route
                        if (state.workoutIntervalDistance < workout.IntervalLength)
                        {
                            state.workoutIntervalDistance += state.currentSpeed * timePassed;
                            state.workoutIntervalDistance = Mathf.Min(state.workoutIntervalDistance, workout.IntervalLength);

                            state.totalDistance += state.currentSpeed * timePassed;
                            state.totalDistance = Mathf.Min(state.totalDistance, workout.IntervalLength * workout.NumIntervals);

                            state.timeInSeconds += timePassed;

                            state.percentDone = state.totalDistance / (workout.IntervalLength * workout.NumIntervals);

                            state.distanceTimeSimulationIntervalList.Add((state.totalDistance, state.timeInSeconds));

                            // calculating simulation interval data so we can update soreness, hydration, and calories
                            int latestIntervalIndex = state.distanceTimeSimulationIntervalList.Count - 1;
                            float intervalDistance = state.distanceTimeSimulationIntervalList[latestIntervalIndex].Item1 - state.distanceTimeSimulationIntervalList[latestIntervalIndex - 1].Item1;
                            float intervalTimeInSeconds = state.distanceTimeSimulationIntervalList[latestIntervalIndex].Item2 - state.distanceTimeSimulationIntervalList[latestIntervalIndex - 1].Item2;
                            float intervalTimeInMinutes = intervalTimeInSeconds / 60f;
                            float intervalMilesPerSecond = intervalDistance / intervalTimeInSeconds;
                            float intervalVO2 = RunUtility.SpeedToOxygenCost(intervalMilesPerSecond);

                            state.lastSimulationIntervalVO2 = intervalVO2;
                            state.shortTermSoreness += runner.CalculateShortTermSoreness(intervalVO2, intervalTimeInMinutes);
                            state.hydrationCost += runner.CalculateHydrationCost(intervalVO2, intervalTimeInMinutes);
                            state.calorieCost += runner.CalculateCalorieCost(intervalVO2, intervalTimeInMinutes);
                        }

                        stateString += $"Name: {runner.Name}\tDistance: {state.totalDistance}\tSpeed: {RunUtility.SpeedToMilePaceString(state.currentSpeed)}\tSoreness: {state.shortTermSoreness + runner.longTermSoreness} ({state.shortTermSoreness},{runner.longTermSoreness})\n";
                    }
                    Debug.Log(stateString);
                    workoutSimulationUpdatedEvent.Invoke(new WorkoutSimulationUpdatedEvent.Context
                    {
                        runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates),
                        groupIndex = groupIndex
                    });

                    yield return null;
                    simulationTime -= timePassed;
                }
            }

            // rest between intervals
            yield return new WaitForSeconds(workout.RestLength * 60 / simulationSecondsPerRealSeconds);
        }


        // post run update
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            RunnerUpdateRecord record = runner.PostRunUpdate(state);
            runnerUpdateDictionary.Add(runner, record);
        }

        //if this is the last group to finish, send the ended event
        groupIndex--;
        if (groupIndex == 0)
        {
            workoutSimulationEndedEvent.Invoke(new WorkoutSimulationEndedEvent.Context()
            {
                runnerUpdateDictionary = new ReadOnlyDictionary<Runner, RunnerUpdateRecord>(runnerUpdateDictionary)
            });
        }
    }
}
