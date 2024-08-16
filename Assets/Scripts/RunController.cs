using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunController : MonoBehaviour
{
//     private const float METERS_PER_MILE = 1609.34f;
//     private void SimulateRun(List<Runner> runners, Route route, RunConditions conditions)
//     {
        
//     }

//     private IEnumerator SimulateRunRoutine(List<Runner> runners, Route route, RunConditions conditions)
//     {
//         Dictionary<Runner, RunnerState> runnerStates = new();
//         foreach(Runner runner in runners)
//         {
//             runnerStates.Add(runner, new RunnerState { currentSpeed = 0, desiredSpeed = 0, distance = 0 });
//         }

//         //while all runners have not finished
//         while(runnerStates.Values.All(state => state.distance < route.length))
//         {
//             //first figure out every runner's preferred speed
//             foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
//             {
//                 Runner runner = kvp.Key;
//                 RunnerState state = kvp.Value;

//                 state.desiredSpeed = CaclulateSpeedFromOxygenCost(runner.VO2Max * .7f);
//             }

//             //TODO: then group people

//             //TODO: then use group's preferred speeds to calculated each group's actual speed
//             foreach(KeyValuePair<Runner, RunnerState> kvp in runnerStates)
//             {
//                 Runner runner = kvp.Key;
//                 RunnerState state = kvp.Value;

//                 state.currentSpeed = state.desiredSpeed;
//             }

//             //then spend a second simulating before moving on to the next iteration

//             yield return new WaitForSeconds(1);
//         }
//     }

//     /// <summary>
//     /// 
//     /// </summary>
//     /// <param name="o2Cost"></param>
//     /// <returns>Speed in meters per minute</returns>
//     private float CaclulateSpeedFromOxygenCost(float o2Cost)
//     {
//         const float a = -0.000104f;
//         const float b = 0.182258f;
//         float c = -4.60f - o2Cost;
//         const float b_squared = b * b;
//         float four_a_c = 4 * a * c;
//         const float two_a = 2 * a;

//         return (-b + Mathf.Sqrt(b_squared - four_a_c)) / two_a;
//     }

//    private struct RunnerState
//    {
//         public float currentSpeed;
//         public float desiredSpeed;
//         public float distance;
//    }
}
