using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsible for the guts of the run simulation
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
    [SerializeField] private float maxDeviation = .1f;
    [SerializeField] private float experienceCap = 1000000f;
    [SerializeField] private float maxSoreness = 500f;
    [SerializeField] private float sorenessEffect = .1f;

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
    public static StartWorkoutEvent starWorkoutEvent = new ();
    
    public class WorkoutSimulationUpdatedEvent : UnityEvent<WorkoutSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
        }
    }
    public static WorkoutSimulationUpdatedEvent workoutSimulationUpdatedEvent = new ();

    public class WorkoutSimulationEndedEvent : UnityEvent<WorkoutSimulationEndedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary;
        }
    }
    public static WorkoutSimulationEndedEvent workoutSimulationEndedEvent = new ();

    #endregion

    private void OnEnable()
    {
        starWorkoutEvent.AddListener(OnStartWorkout);
    }

    private void OnDisable()
    {
        starWorkoutEvent.RemoveListener(OnStartWorkout);
    }

    private void OnStartWorkout(StartWorkoutEvent.Context context)
    {
        IEnumerator[] groupWorkoutRoutines = new IEnumerator[context.groups.Count];
        for(int i = 0; i < context.groups.Count; i++)
        {
            groupWorkoutRoutines[i] = SimulateWorkoutRoutine(context.groups[i], context.workout, i); 
            StartCoroutine(groupWorkoutRoutines[i]);
        }
    }

    private IEnumerator SimulateWorkoutRoutine(WorkoutGroup group, Workout workout, int index)
    {
        //wait a frame for the other starts to get going
        yield return null;

        // space each coroutine/group out by 5 seconds
        yield return new WaitForSeconds(index * 5);

        // go through each runner and initialize their state for this interval
        // most of the work here is setting up what the vo2 for this workout should be to begin the run with
        Dictionary<Runner, RunnerState> runnerStates = new();
        foreach(Runner runner in group.runners)
        {

            runnerStates.Add(runner, new RunnerState
            {
                runVO2 = 0,
                currentSpeed = 0,
                desiredSpeed = 0,
                totalDistance = 0,
                distanceTimeSimulationIntervalList = new List<(float, float)> {(0,0)}
            });
        }

        for(int workoutIntervalIndex = 0; workoutIntervalIndex < workout.NumIntervals; workoutIntervalIndex++)
        {

            foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;
                
                // TODO: if the coach guidance is slow already, should heavy soreness make you go slower?
                float statusMean = -Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, runner.LongTermSoreness)) * sorenessEffect;
                float statusDeviation = Mathf.Clamp((1 - (runner.Experience / experienceCap)) * maxDeviation, 0, maxDeviation);
                float roll = 0;//CNExtensions.RandGaussian(statusMean, statusDeviation);
                Debug.Log($"Name: {runner.Name}\tMean: {statusMean}\tDeviation: {statusDeviation}\tRoll: {roll}");

                //this calculates what the vo2 should be for the run for this runner
                //we use the vo2Max, the coach guidance as a percentage of that, then adjust based on the runner's amount of experience
                //amount of experience is based off of how many miles a runner has run
                //right now we just have a linear relationship between number of miles run and variance in runVO2
                state.runVO2 = runner.CurrentVO2Max * Mathf.Max(.5f, (group.targetVO2 / runner.CurrentVO2Max) + roll);
                state.currentSpeed = 0;
                state.desiredSpeed = 0;
                state.workoutIntervalDistance = 0;
                state.percentDone = 0;
            }


            // while all runners have not finished, simulate the run
            while (runnerStates.Values.Any(state => state.workoutIntervalDistance < workout.IntervalLength))
            {
                // first figure out every runner's preferred speed
                foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    float paceChangeStdDev = .03f;
                    float paceChangeMeanMagnitude = .02f;

                    float normalizedSorenessFeel = Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, state.shortTermSoreness + runner.LongTermSoreness));
                    float sorenessPaceChangeFactor = Mathf.Pow(2 * normalizedSorenessFeel - 1, 5);

                    float normalizedIntervalVO2 = Mathf.Clamp01(Mathf.InverseLerp(runner.CurrentVO2Max, runner.CurrentVO2Max, state.lastSimulationIntervalVO2));
                    float vo2PaceChangeFactor = normalizedIntervalVO2 - (group.targetVO2 / runner.CurrentVO2Max);

                    float paceChangeMean = 0;
                    if (sorenessPaceChangeFactor > .1f && vo2PaceChangeFactor > -.05f)
                    {
                        paceChangeMean = sorenessPaceChangeFactor * -paceChangeMeanMagnitude;
                    }
                    else if (sorenessPaceChangeFactor < -.1f && vo2PaceChangeFactor < .05f)
                    {
                        paceChangeMean = (-vo2PaceChangeFactor + .05f) * paceChangeMeanMagnitude;
                    }

                    float roll = CNExtensions.RandGaussian(paceChangeMean, paceChangeStdDev);
                    state.runVO2 += roll * runner.CurrentVO2Max;

                    // clamp the vo2 between some reasonable values
                    state.runVO2 = Mathf.Clamp(state.runVO2, .5f * runner.CurrentVO2Max, 1.25f * runner.CurrentVO2Max);

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

                        if (state.workoutIntervalDistance >= workout.IntervalLength)
                        {
                            continue;
                        }

                        float runningAverage = 0f;
                        float weightTotal = 0f;
                        //go through each runner and add to the running average + total
                        foreach (KeyValuePair<Runner, RunnerState> otherKvp in runnerStates)
                        {
                            //skip if current==other or if this runner is already done running
                            if (kvp.Key == otherKvp.Key || otherKvp.Value.workoutIntervalDistance >= workout.IntervalLength)
                            {
                                continue;
                            }

                            //use a gravity model so runners closer together effect each other more than runners far away
                            float difference = Mathf.Abs(otherKvp.Value.desiredSpeed - state.desiredSpeed) + Mathf.Max(Mathf.Abs(otherKvp.Value.workoutIntervalDistance - state.workoutIntervalDistance), Mathf.Epsilon);
                            float weight = (difference < .001f) ? 1000000f : 1f / Mathf.Max(Mathf.Pow(difference, 2), Mathf.Epsilon);
                            runningAverage += weight * otherKvp.Value.desiredSpeed;
                            weightTotal += weight;
                        }

                        //if we are effected by any runners, figure out how they effect our current speed
                        //the last runner left on the route will not be effected by anyone so they just run at their desired speed
                        if (weightTotal > 0)
                        {
                            state.desiredSpeed = Mathf.Lerp(state.desiredSpeed, runningAverage / weightTotal, 1f - (.75f * group.targetVO2 / runner.CurrentVO2Max));
                        }

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

                            float intervalMilesPerSecond = intervalDistance / Mathf.Max(1, intervalTimeInSeconds);
                            float intervalVO2 = RunUtility.SpeedToOxygenCost(intervalMilesPerSecond);
                            float intervalTimeInMinutes = intervalTimeInSeconds / 60f;

                            state.shortTermSoreness += runner.CalculateShortTermSoreness(intervalVO2, intervalTimeInMinutes);

                            state.hydrationCost += runner.CalculateHydrationCost(intervalVO2, intervalTimeInMinutes);
                            state.calorieCost += runner.CalculateCalorieCost(intervalVO2, intervalTimeInMinutes);
                            state.lastSimulationIntervalVO2 = intervalVO2;
                        }

                        stateString += $"Name: {runner.Name}\tDistance: {state.totalDistance}\tSpeed: {RunUtility.SpeedToMilePaceString(state.currentSpeed)}\tSoreness: {state.shortTermSoreness + runner.LongTermSoreness} ({state.shortTermSoreness},{runner.LongTermSoreness})\n";
                    }
                    Debug.Log(stateString);
                    workoutSimulationUpdatedEvent.Invoke(new WorkoutSimulationUpdatedEvent.Context
                    {
                        runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates)
                    });

                    yield return null;
                    simulationTime -= timePassed;
                }
            }
            
            // rest between intervals
            yield return new WaitForSeconds(workout.RestLength * 60 / simulationSecondsPerRealSeconds);
        }

        // post run update
        Dictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary = new();
        foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            RunnerUpdateRecord record = runner.PostRunUpdate(state);
            runnerUpdateDictionary.Add(runner, record);
        }

        workoutSimulationEndedEvent.Invoke(new WorkoutSimulationEndedEvent.Context()
        {
            runnerUpdateDictionary = new ReadOnlyDictionary<Runner, RunnerUpdateRecord>(runnerUpdateDictionary)
        });
    }
}
