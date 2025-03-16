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
            runnerStates.Add(runner, new RunnerState());
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
                state.desiredVO2 = runner.currentVO2Max * group.targetVO2 / runner.currentVO2Max;
                state.currentSpeed = 0;
                state.desiredSpeed = 0;
                state.intervalDistance = 0;
            }

            // while all runners have not finished, simulate the run
            while (runnerStates.Values.Any(state => state.intervalDistance < workout.IntervalLength))
            {
                // first figure out every runner's preferred speed
                foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    state.desiredVO2 = RunUtility.StepRunnerVO2(runner, state, group.targetVO2 / runner.currentVO2Max, maxSoreness);
                    state.desiredSpeed = RunUtility.CaclulateSpeedFromOxygenCost(state.desiredVO2 * runner.CalculateRunEconomy(state));
                }

                // now that we have everyone's desired speed, we use a gravity model to group people
                int numGravityIterations = 2;
                for (int i = 0; i < numGravityIterations; i++)
                {
                    foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                    {
                        Runner runner = kvp.Key;
                        RunnerState state = kvp.Value;

                        state.desiredSpeed = RunUtility.RunGravityModel(runner, state, runnerStates, group.targetVO2 / runner.currentVO2Max, workout.IntervalLength);

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
                    float timePassed = simulationSecondsPerRealSeconds * Time.deltaTime;   
                    RunUtility.StepRunState(runnerStates, timePassed, workout.IntervalLength, workout.IntervalLength * workout.NumIntervals);

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
