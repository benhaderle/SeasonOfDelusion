using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsible for the guts of the race simulation
/// </summary>
public class RaceController : MonoBehaviour
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

    private Dictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary = new();

    #region Events
    public class StartRaceEvent : UnityEvent<StartRaceEvent.Context>
    {
        public class Context
        {
            public List<Team> teams;
            public RaceRoute raceRoute;
            public RunConditions runConditions;
        }
    };
    public static StartRaceEvent startRaceEvent = new();

    public class RaceSimulationUpdatedEvent : UnityEvent<RaceSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
            public int groupIndex;
        }
    }
    public static RaceSimulationUpdatedEvent raceSimulationUpdatedEvent = new();

    public class RaceSimulationEndedEvent : UnityEvent<RaceSimulationEndedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary;
            public WorkoutGroup group;
        }
    }
    public static RaceSimulationEndedEvent raceSimulationEndedEvent = new();

    #endregion

    private void OnEnable()
    {
        startRaceEvent.AddListener(OnStartRace);
    }

    private void OnDisable()
    {
        startRaceEvent.RemoveListener(OnStartRace);
    }

    private void OnStartRace(StartRaceEvent.Context context)
    {
        StartCoroutine(SimulateRaceRoutine(context.teams, context.raceRoute));
    }

    /// <summary>
    /// Simulates an entire race for the given teams
    /// </summary>
    private IEnumerator SimulateRaceRoutine(List<Team> teams, RaceRoute raceRoute)
    {
        float targetVO2Percent = 1.1f;

        // wait a frame for the other starts to get going
        yield return null;

        // go through each runner and initialize their state for this workout
        Dictionary<Runner, RunnerState> runnerStates = new();

        teams.ForEach(t => t.Runners.ToList().ForEach(r =>
            runnerStates.Add(r, new RunnerState
            {
                runVO2 = 0,
                currentSpeed = 0,
                desiredSpeed = 0,
                totalDistance = 0,
                distanceTimeSimulationIntervalList = new List<(float, float)> { (0, 0) }
            })
        ));

        // reset speed and initial V02 for every runner
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            //TODO: this sets the V02 perfectly at the start of each interval bc the other way with rolls was too random
            // but this should probably account for experience and soreness in some way
            state.runVO2 = runner.CurrentVO2Max * targetVO2Percent;
            state.currentSpeed = 0;
            state.desiredSpeed = 0;
            state.workoutIntervalDistance = 0;
        }

        // while all runners have not finished, simulate the run
        while (runnerStates.Values.Any(state => state.totalDistance < raceRoute.Length))
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
                float normalizedSorenessFeel = Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, state.shortTermSoreness + runner.LongTermSoreness));
                // a smoothed number between -1 and 1 that represents the magnitude and direction of the soreness effect on pace
                // low numbers mean you can speed up and high numbers mean you gotta slow doen
                float sorenessPaceChangeFactor = Mathf.Pow(Mathf.Lerp(-1, 1, normalizedSorenessFeel), 5);

                // number that represents the percentile of the last interval's VO2 usage
                // example: runner VO2 = 50, last interval was at 45 V02 pace, intervalVO2Percent would then be .9
                float intervalVO2Percent = state.lastSimulationIntervalVO2 / runner.CurrentVO2Max;
                // how far off the last interval was from coach's guidance in percent of VO2
                // low numbers mean you're slow and high numbers mean you're fast
                float vo2PaceChangeFactor = intervalVO2Percent - targetVO2Percent;

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

                    // if this runner is done, continue
                    if (state.totalDistance >= raceRoute.Length)
                    {
                        continue;
                    }

                    float runningAverage = 0f;
                    float weightTotal = 0f;
                    // go through each runner and add to the running average + total
                    foreach (KeyValuePair<Runner, RunnerState> otherKvp in runnerStates)
                    {
                        // skip if current==other or if this runner is already done running
                        if (kvp.Key == otherKvp.Key || otherKvp.Value.totalDistance >= raceRoute.Length)
                        {
                            continue;
                        }

                        // use a gravity model so runners closer together effect each other more than runners far away
                        float difference = Mathf.Abs(otherKvp.Value.desiredSpeed - state.desiredSpeed) + Mathf.Abs(otherKvp.Value.totalDistance - state.totalDistance);
                        // if the difference between the runners is super small, cap weight at a high amount
                        float weight = (difference < .001f) ? 1000000f : 1f / Mathf.Max(Mathf.Pow(difference, 2), Mathf.Epsilon);
                        runningAverage += weight * otherKvp.Value.desiredSpeed;
                        weightTotal += weight;
                    }

                    // if we are effected by any runners, figure out how they effect our current speed
                    // the last runner left on the route will not be effected by anyone so they just run at their desired speed
                    if (weightTotal > 0)
                    {
                        state.desiredSpeed = Mathf.Lerp(state.desiredSpeed, runningAverage / weightTotal, 1f - (.75f * targetVO2Percent));
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
                List<KeyValuePair<Runner, RunnerState>> sortedRunnerStates = runnerStates.ToList();
                sortedRunnerStates.Sort((kvp1, kvp2) =>
                {
                    if (kvp1.Value.totalDistance == kvp2.Value.totalDistance)
                    {
                        if (kvp1.Value.timeInSeconds == kvp2.Value.timeInSeconds)
                        {
                            return 0;
                        }
                        else if (kvp1.Value.timeInSeconds < kvp2.Value.timeInSeconds)
                            return -1;
                        else
                            return 1;
                    }
                    else if (kvp1.Value.totalDistance > kvp2.Value.totalDistance)
                        return -1;
                    else
                        return 1;
                });
                foreach (KeyValuePair<Runner, RunnerState> kvp in sortedRunnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    //if a runner is not done, keep incrementing them along the route
                    if (state.totalDistance < raceRoute.Length)
                    {
                        state.totalDistance += state.currentSpeed * timePassed;
                        state.totalDistance = Mathf.Min(state.totalDistance, raceRoute.Length);

                        state.timeInSeconds += timePassed;

                        state.percentDone = state.totalDistance / raceRoute.Length;

                        state.distanceTimeSimulationIntervalList.Add((state.totalDistance, state.timeInSeconds));

                        // calculating simulation interval data so we can update soreness, hydration, and calories
                        int latestIntervalIndex = state.distanceTimeSimulationIntervalList.Count - 1;
                        float intervalDistance = state.distanceTimeSimulationIntervalList[latestIntervalIndex].Item1 - state.distanceTimeSimulationIntervalList[latestIntervalIndex - 1].Item1;
                        float intervalTimeInSeconds = state.distanceTimeSimulationIntervalList[latestIntervalIndex].Item2 - state.distanceTimeSimulationIntervalList[latestIntervalIndex - 1].Item2;
                        float intervalTimeInMinutes = intervalTimeInSeconds / 60f;
                        float intervalMilesPerSecond = intervalDistance / Mathf.Max(1, intervalTimeInSeconds);
                        float intervalVO2 = RunUtility.SpeedToOxygenCost(intervalMilesPerSecond);

                        state.lastSimulationIntervalVO2 = intervalVO2;
                        state.shortTermSoreness += runner.CalculateShortTermSoreness(intervalVO2, intervalTimeInMinutes);
                        state.hydrationCost += runner.CalculateHydrationCost(intervalVO2, intervalTimeInMinutes);
                        state.calorieCost += runner.CalculateCalorieCost(intervalVO2, intervalTimeInMinutes);
                    }

                    stateString += $"Name: {runner.Name}\tDistance: {state.totalDistance}\tTime: {state.timeInSeconds}\tSpeed: {RunUtility.SpeedToMilePaceString(state.currentSpeed)}\tSoreness: {state.shortTermSoreness + runner.LongTermSoreness} ({state.shortTermSoreness},{runner.LongTermSoreness})\n";
                }
                Debug.Log(stateString);
                raceSimulationUpdatedEvent.Invoke(new RaceSimulationUpdatedEvent.Context
                {
                    runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates),
                });

                yield return null;
                simulationTime -= timePassed;
            }
        }

        // post run update for the player team
        foreach (Runner runner in teams[0].Runners)
        {
            RunnerState state = runnerStates[runner];

            RunnerUpdateRecord record = runner.PostRunUpdate(state);
            runnerUpdateDictionary.Add(runner, record);
        }

        raceSimulationEndedEvent.Invoke(new RaceSimulationEndedEvent.Context()
        {
            runnerUpdateDictionary = new ReadOnlyDictionary<Runner, RunnerUpdateRecord>(runnerUpdateDictionary)
        });
    }
}