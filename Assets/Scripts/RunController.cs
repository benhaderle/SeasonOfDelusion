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
                runVO2 = runner.VO2Max * conditions.coachVO2Guidance + CNExtensions.RandGaussian(0, runner.Experience / 1000000f + .1f),
                currentSpeed = 0, 
                desiredSpeed = 0, 
                distance = 0 
            });
        }

        //while all runners have not finished
        while(runnerStates.Values.All(state => state.distance < route.Length))
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
            }

            //then spend a second simulating before moving on to the next iteration

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="o2Cost"></param>
    /// <returns>Speed in meters per minute</returns>
    private float CaclulateSpeedFromOxygenCost(float o2Cost)
    {
        const float a = -0.000104f;
        const float b = 0.182258f;
        float c = -4.60f - o2Cost;
        const float b_squared = b * b;
        float four_a_c = 4 * a * c;
        const float two_a = 2 * a;

        return (-b + Mathf.Sqrt(b_squared - four_a_c)) / two_a;
    }

   private struct RunnerState
   {
        public float runVO2;
        public float currentSpeed;
        public float desiredSpeed;
        public float distance;
   }
}
