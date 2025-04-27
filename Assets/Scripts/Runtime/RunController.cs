using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsible for the guts of the "normal" (non-race + non-workout) run simulation
/// TODO: some of this could be broken out be repurposed along with WorkoutController and RaceController
/// </summary>
public class RunController : MonoBehaviour
{
    /// <summary>
    /// How fast the simulation should run at
    /// </summary>
    [SerializeField] private float simulationSecondsPerRealSeconds = 30;
    [SerializeField] private float pauseTime = .5f;
    private float currentSimulationSecondsPerRealSeconds;

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

    private IEnumerator pauseRoutine;



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
    public static StartRunEvent startRunEvent = new();

    public class RunSimulationUpdatedEvent : UnityEvent<RunSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
        }
    }
    public static RunSimulationUpdatedEvent runSimulationUpdatedEvent = new();

    public class RunSimulationEndedEvent : UnityEvent<RunSimulationEndedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary;
        }
    }
    public static RunSimulationEndedEvent runSimulationEndedEvent = new();

    public class RunSimulationResumeEvent : UnityEvent<RunSimulationResumeEvent.Context>
    {
        public class Context
        {
        }
    }
    public static RunSimulationResumeEvent runSimulationResumeEvent = new();

    public class StartRunDialogueEvent : UnityEvent<StartRunDialogueEvent.Context>
    {
        public class Context
        {
            public string dialogueID;
        }
    }
    public static StartRunDialogueEvent startRunDialogueEvent = new();
    #endregion

    private void OnEnable()
    {
        startRunEvent.AddListener(OnStartRun);
        runSimulationResumeEvent.AddListener(OnRunSimulationResume);
        RouteModel.routeUnlockedEvent.AddListener(OnRouteUnlocked);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
    }

    private void OnDisable()
    {
        startRunEvent.RemoveListener(OnStartRun);
        runSimulationResumeEvent.RemoveListener(OnRunSimulationResume);
        RouteModel.routeUnlockedEvent.RemoveListener(OnRouteUnlocked);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
    }

    private void OnStartRun(StartRunEvent.Context context)
    {
        string currentRunDialogueID = SimulationModel.Instance.GetDialogueForCurrentEvent();
        if (string.IsNullOrWhiteSpace(currentRunDialogueID))
        {
            currentRunDialogueID = context.route.GetNextDialogueID();
        }
        float dialogueActivationPercent = Random.Range(0.2f, 0.8f);

        StartCoroutine(SimulateRunRoutine(context.runners, context.route, context.runConditions, currentRunDialogueID, dialogueActivationPercent));
    }

    private void OnRunSimulationResume(RunSimulationResumeEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref pauseRoutine, LerpSimulationSpeed(simulationSecondsPerRealSeconds, pauseTime));
    }

    private void OnRouteUnlocked(RouteModel.RouteUnlockedEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref pauseRoutine, LerpSimulationSpeed(0, pauseTime));
    }

    private IEnumerator LerpSimulationSpeed(float targetSpeed, float duration)
    {
        float startSpeed = currentSimulationSecondsPerRealSeconds;
        float timePassed = 0;

        while (timePassed < duration)
        {
            currentSimulationSecondsPerRealSeconds = Mathf.Lerp(startSpeed, targetSpeed, timePassed / duration);
            timePassed += Time.deltaTime;
            yield return null;
        }

        currentSimulationSecondsPerRealSeconds = targetSpeed;
    }

    private IEnumerator SimulateRunRoutine(List<Runner> runners, Route route, RunConditions conditions, string dialogueID, float dialogueActivationPercent)
    {
        bool dialogueActivated = false;

        //wait a frame for the other starts to get going
        yield return null;

        currentSimulationSecondsPerRealSeconds = simulationSecondsPerRealSeconds;

        // go through each runner and initialize their state for this run
        // most of the work here is setting up what the vo2 for this run should be to begin the run with
        Dictionary<Runner, RunnerState> runnerStates = new();
        foreach (Runner runner in runners)
        {
            // TODO: if the coach guidance is slow already, should heavy soreness make you go slower?
            // TODO: thinkin that maybe this is too random
            float statusMean = -Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, runner.longTermSoreness)) * sorenessEffect;
            float statusDeviation = Mathf.Clamp((1 - (runner.experience / experienceCap)) * maxDeviation, 0, maxDeviation);
            float roll = CNExtensions.RandGaussian(statusMean, statusDeviation);

            Debug.Log($"Name: {runner.Name}\tMean: {statusMean}\tDeviation: {statusDeviation}\tRoll: {roll}");

            RunnerState state = new RunnerState();
            state.desiredVO2 = runner.currentVO2Max * route.Difficulty + roll;
            runnerStates.Add(runner, state);
        }

        // while all runners have not finished, simulate the run
        while (runnerStates.Values.Any(state => state.totalDistance < route.Length))
        {
            // first figure out every runner's preferred speed
            foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                state.desiredVO2 = RunUtility.StepRunnerVO2(runner, state, route.Difficulty, maxSoreness);
                state.desiredSpeed = RunUtility.CaclulateSpeedFromOxygenCost(state.desiredVO2 * runner.CalculateRunEconomy(state));
            }

            // now that we have everyone's desired speed, we use a gravity model to group people
            int numGravityIterations = 5;
            for (int i = 0; i < numGravityIterations; i++)
            {
                foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    state.desiredSpeed = RunUtility.RunGravityModel(runner, state, runnerStates, route.Difficulty, route.Length);

                    // if this is the last iteration, set the current speed
                    if (i == numGravityIterations - 1)
                    {
                        state.currentSpeed = state.desiredSpeed;
                    }
                }
            }

            //then spend a step simulating before moving on to the next iteration
            float simulationTime = simulationStep;
            while (simulationTime > 0)
            {
                if (currentSimulationSecondsPerRealSeconds == 0)
                {
                    yield return null;
                    continue;
                }

                float timePassed = currentSimulationSecondsPerRealSeconds * Time.deltaTime;
                RunUtility.StepRunState(runnerStates, timePassed, route.Length, route.Length);

                if (!string.IsNullOrWhiteSpace(dialogueID) && !dialogueActivated && runnerStates.Values.Any(state => state.percentDone > dialogueActivationPercent))
                {
                    dialogueActivated = true;
                    StartDialogue(dialogueID);
                }

                runSimulationUpdatedEvent.Invoke(new RunSimulationUpdatedEvent.Context
                {
                    runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates)
                });

                yield return null;
                simulationTime -= timePassed;
            }
        }

        // post run update
        Dictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary = new();
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            RunnerUpdateRecord record = runner.PostRunUpdate(state);
            runnerUpdateDictionary.Add(runner, record);
        }

        runSimulationEndedEvent.Invoke(new RunSimulationEndedEvent.Context()
        {
            runnerUpdateDictionary = new ReadOnlyDictionary<Runner, RunnerUpdateRecord>(runnerUpdateDictionary)
        });
    }

    private void StartDialogue(string dialogueID)
    {
        CNExtensions.SafeStartCoroutine(this, ref pauseRoutine, StartDialogueRoutine(dialogueID));
    }

    private IEnumerator StartDialogueRoutine(string dialogueID)
    {
        yield return LerpSimulationSpeed(0, pauseTime);

        Debug.Log($"Starting dialogue with ID: {dialogueID}");

        DialogueUIController.startDialogueEvent.Invoke(new DialogueUIController.StartDialogueEvent.Context
        {
            dialogueID = dialogueID
        });
    }

    private void OnDialogueEnded(DialogueUIController.DialogueEndedEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref pauseRoutine, LerpSimulationSpeed(simulationSecondsPerRealSeconds, pauseTime));
    }
}