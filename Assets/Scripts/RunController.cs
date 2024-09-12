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
public class RunController : MonoBehaviour
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
    [SerializeField] private float maxExhaustion = 500f;
    [SerializeField] private float exhaustionEffect = .1f;

    #region Events
    public class StartRunEvent : UnityEvent<StartRunEvent.Context> 
    { 
        public class Context
        {
            public List<Runner> runners;
            public Route route;
            public RunConditions runConditions;
        }
    };
    public static StartRunEvent startRunEvent = new ();
    
    public class RunSimulationUpdatedEvent : UnityEvent<RunSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
        }
    }
    public static RunSimulationUpdatedEvent runSimulationUpdatedEvent = new ();


    public class RunSimulationEndedEvent : UnityEvent<RunSimulationEndedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static RunSimulationEndedEvent runSimulationEndedEvent = new ();
    #endregion

    private void OnEnable()
    {
        startRunEvent.AddListener(OnStartRun);
    }

    private void OnDisable()
    {
        startRunEvent.RemoveListener(OnStartRun);
    }

    private void OnStartRun(StartRunEvent.Context context)
    {
        StartCoroutine(SimulateRunRoutine(context.runners, context.route, context.runConditions));
    }

    private IEnumerator SimulateRunRoutine(List<Runner> runners, Route route, RunConditions conditions)
    {
        //wait a frame for the other starts to get going
        yield return null;

        Dictionary<Runner, RunnerState> runnerStates = new();
        foreach(Runner runner in runners)
        {
            float statusMean = -Mathf.Clamp01(Mathf.InverseLerp(0, maxExhaustion, runner.Exhaustion)) * exhaustionEffect;
            float statusDeviation = Mathf.Clamp((1 - (runner.Experience / experienceCap)) * maxDeviation, 0, maxDeviation);
            float roll = CNExtensions.RandGaussian(statusMean, statusDeviation);

            Debug.Log($"Name: {runner.Name}\tMean: {statusMean}\tDeviation: {statusDeviation}\tRoll: {roll}");
            runnerStates.Add(runner, new RunnerState 
            { 
                //this calculates what the vo2 should be for the run for this runner
                //we use the vo2Max, the coach guidance as a percentage of that, then adjust based on the runner's amount of experience
                //amount of experience is based off of how many miles a runner has run
                //right now we just have a linear relationship between number of miles run and variance in runVO2
                runVO2 = runner.CurrentVO2Max * Mathf.Max(.5f, conditions.coachVO2Guidance + roll),
                currentSpeed = 0, 
                desiredSpeed = 0, 
                distance = 0 
            });
        }

        //while all runners have not finished, simulate the run
        while(runnerStates.Values.Any(state => state.distance < route.Length))
        {
            //first figure out every runner's preferred speed
            foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                // calculating some stuff to be used below
                float milesPerSecond = state.distance / Mathf.Max(1, state.timeInSeconds);
                float runVO2 = RunUtility.SpeedToOxygenCost(milesPerSecond);
                float timeInMinutes = state.timeInSeconds / 60f;

                // figure out the exhaustion for this runner during this run
                state.exhaustion = runner.CalculateExhaustion(runVO2, timeInMinutes);
                float currentTotalExhaustion = state.exhaustion;

                // use the current run exhaustion to get a random roll to see if pace should go up or down
                // below a threshold of exhaustion, it's more likely to speed up, over the threshold, it's more likely to slow down
                float roll = CNExtensions.RandGaussian(-Mathf.Pow(Mathf.InverseLerp(0, maxExhaustion, runner.Exhaustion - 200), 2) * 0.01f, .005f);
                state.runVO2 += roll * runner.CurrentVO2Max;

                // clamp the vo2 between some reasonable values
                state.runVO2 = Mathf.Clamp(state.runVO2, .5f * runner.CurrentVO2Max, 1.25f * runner.CurrentVO2Max);

                state.desiredSpeed = RunUtility.CaclulateSpeedFromOxygenCost(state.runVO2);
            }

            //TODO: then use group's preferred speeds to calculated each group's actual speed
            foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                float runningAverage = 0f;
                float weightTotal = 0f;
                foreach (KeyValuePair<Runner, RunnerState> otherKvp in runnerStates)
                {
                    if(kvp.Key == otherKvp.Key)
                    {
                        continue;
                    }

                    float difference = otherKvp.Value.desiredSpeed - state.desiredSpeed;
                    float weight = 1f / Mathf.Pow(difference, 2);
                    runningAverage += weight * otherKvp.Value.desiredSpeed;
                    weightTotal += weight;                    
                }

                state.currentSpeed = Mathf.Lerp(state.desiredSpeed, runningAverage / weightTotal, .5f);
            }

            //then spend a second simulating before moving on to the next iteration
            float simulationTime = simulationStep;
            while(simulationTime > 0)
            {
                string stateString = "";
                float timePassed = simulationSecondsPerRealSeconds * Time.deltaTime;
                foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    if (state.distance < route.Length)
                    {
                        state.distance += state.currentSpeed * timePassed;
                        state.distance = Mathf.Min(state.distance, route.Length);

                        state.timeInSeconds += timePassed;

                        state.percentDone = state.distance / route.Length;
                    }

                    stateString += $"Name: {runner.Name}\tDistance: {state.distance}\tSpeed: {RunUtility.SpeedToMilePaceString(state.currentSpeed)}\n";
                }
                Debug.Log(stateString);
                runSimulationUpdatedEvent.Invoke(new RunSimulationUpdatedEvent.Context
                {
                    runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates)
                });

                yield return null;
                simulationTime -= timePassed;
            }
        }

        // post run update
        foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            runner.PostRunUpdate(state);
        }

        runSimulationEndedEvent.Invoke(new RunSimulationEndedEvent.Context());
        SimulationModel.Instance.AdvanceDay();
    }
}
public class RunnerState
{
    public float runVO2;
    public float currentSpeed;
    public float desiredSpeed;
    public float distance;
    public float percentDone;
    public float timeInSeconds;
    public float exhaustion;
}
