using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

public class RunController : MonoBehaviour
{
    private const float METERS_PER_MILE = 1609.34f;

    [SerializeField] private float simulationSecondsPerRealSeconds = 30;

    [Header("Run VO2 Calculation Variables")]
    [SerializeField] private float experienceCap = 1000000f;
    [SerializeField] private float maxExhaustion = 500f;
    [SerializeField] private float exhaustionEffect = .25f;

    [Header("Exhaustion Calculation Variables")]
    [SerializeField] private float cubicVO2Slope = 2f;
    [SerializeField] private float linearVO2Slope = 1f;
    [SerializeField] private float linearVO2Offset = .5f;
    [SerializeField] private float constantVO2Offset = -10f;

    [Header("Exhaustion Calculation Variables")]
    [SerializeField] private float cubicExhaustionSlope = 3.5f;
    [SerializeField] private float linearExhaustionSlope = 3f;
    [SerializeField] private float linearExhaustionOffset = .15f;
    [SerializeField] private float constantExhaustionOffset = -15f;


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
            float statusDeviation = Mathf.Max(-runner.Experience / experienceCap + .1f, 0);
            float roll = CNExtensions.RandGaussian(statusMean, statusDeviation);

            Debug.Log($"Name: {runner.Name}\tMean: {statusMean}\tDeviation: {statusDeviation}\tRoll: {roll}");
            runnerStates.Add(runner, new RunnerState 
            { 
                //this calculates what the vo2 should be for the run for this runner
                //we use the vo2Max, the coach guidance as a percentage of that, then adjust based on the runner's amount of experience
                //amount of experience is based off of how many miles a runner has run
                //right now we just have a linear relationship between number of miles run and variance in runVO2
                runVO2 = runner.CurrentVO2Max * conditions.coachVO2Guidance + roll,
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

                state.desiredSpeed = CaclulateSpeedFromOxygenCost(state.runVO2);
            }

            //TODO: then group people

            //TODO: then use group's preferred speeds to calculated each group's actual speed
            foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                state.currentSpeed = state.desiredSpeed;
                Debug.Log(state.currentSpeed);
            }

            //then spend a second simulating before moving on to the next iteration
            float simulationTime = 1f;
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

                    stateString += $"Name: {runner.Name}\tDistance: {state.distance}\tSpeed: {SpeedToMilePaceString(state.currentSpeed)}\n";
                }
                Debug.Log(stateString);
                runSimulationUpdatedEvent.Invoke(new RunSimulationUpdatedEvent.Context
                {
                    runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates)
                });

                yield return null;
                simulationTime -= Time.deltaTime;
            }
        }

        //post run update
        foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            float milesPerSecond = route.Length / state.timeInSeconds;
            float runVO2 = SpeedToOxygenCost(milesPerSecond);
            float timeInMinutes = state.timeInSeconds / 60f;

            // experience is a function of cumulative miles run
            runner.IncreaseExperience(route.Length);

            float vo2ImprovementGap = (runVO2 / (runner.CurrentVO2Max * .9f)) - 1f;
            runner.UpdateVO2((cubicExhaustionSlope * timeInMinutes * Mathf.Pow(vo2ImprovementGap, 3)) + (linearExhaustionSlope * timeInMinutes * (vo2ImprovementGap + linearExhaustionOffset)) + constantExhaustionOffset);

            // exhaustion changes based off of how far away you were from your recovery VO2
            // TODO: when we have a day simulation, exhaustion should decrement each night
            float exhaustionGap = (runVO2 / (runner.CurrentVO2Max * .6f)) - 1f;
            runner.UpdateExhaustion((cubicExhaustionSlope * timeInMinutes * Mathf.Pow(exhaustionGap, 3)) + (linearExhaustionSlope * timeInMinutes * (exhaustionGap + linearExhaustionOffset)) + constantExhaustionOffset);

            Debug.Log($"Name: {runner.Name}\tGap: {exhaustionGap}\tExhaustion: {runner.Exhaustion}");
        }
    }

    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="o2Cost">in mL/kg/min</param>
    /// <returns>Speed in miles per sec</returns>
    private float CaclulateSpeedFromOxygenCost(float o2Cost)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        float c = -4.6f - o2Cost;
        const float b_squared = b * b;
        float four_a_c = 4 * a * c;
        const float two_a = 2 * a;

        return (-b + Mathf.Sqrt(b_squared - four_a_c)) / (two_a * METERS_PER_MILE * 60f);
    }

    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="speed">in miles per sec</param>
    /// <returns>in mL/kg/min</returns>
    private float SpeedToOxygenCost(float speed)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        const float c = -4.6f;

        speed = speed * METERS_PER_MILE * 60f;
        return a * Mathf.Pow(speed, 2) + b * speed + c;
    }

    private string SpeedToMilePaceString(float milesPerSec)
    {
        float minPerMile = 1f / (milesPerSec * 60f);
        int minutes = (int)minPerMile;
        int seconds = (int)((minPerMile - minutes) * 60);
        if (seconds < 10)
        {
            return $"{minutes}:0{seconds}";
        }
        else
        {
            return $"{minutes}:{seconds}";
        }
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
}
