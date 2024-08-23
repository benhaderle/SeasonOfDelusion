using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

public class RunController : MonoBehaviour
{
    private const float METERS_PER_MILE = 1609.34f;

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
        Dictionary<Runner, RunnerState> runnerStates = new();
        foreach(Runner runner in runners)
        {
            runnerStates.Add(runner, new RunnerState 
            { 
                //this calculates what the vo2 should be for the run for this runner
                //we use the vo2Max, the coach guidance as a percentage of that, then adjust based on the runner's amount of experience
                //amount of experience is based off of how many miles a runner has run
                //right now we just have a linear relationship between number of miles run and variance in runVO2
                //TODO: adjust for exhaustion
                runVO2 = runner.VO2Max * conditions.coachVO2Guidance + CNExtensions.RandGaussian(0, Mathf.Max(-runner.Experience / 1000000f + .1f, 0)),
                currentSpeed = 0, 
                desiredSpeed = 0, 
                distance = 0 
            });
        }

        //while all runners have not finished
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
            float simulationSecondsPerRealSeconds = 30;
            while(simulationTime > 0)
            {
                string stateString = "";
                //TODO: then use group's preferred speeds to calculated each group's actual speed
                foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    if (state.distance < route.Length)
                    {
                        state.distance += state.currentSpeed * simulationSecondsPerRealSeconds * Time.deltaTime;
                        state.distance = Mathf.Min(state.distance, route.Length);
                    }

                    stateString += $"Name:{runner.Name}\tDistance:{state.distance}\tSpeed:{SpeedToMilePaceString(state.currentSpeed)}\n";
                }
                Debug.Log(stateString);

                yield return null;
                simulationTime -= Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o2Cost"></param>
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

   private class RunnerState
   {
        public float runVO2;
        public float currentSpeed;
        public float desiredSpeed;
        public float distance;
   }
}
