using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using Unity.VisualScripting;
using System.Linq;
using Shapes;

/// <summary>
/// Responsible for static variables and functions relevant to run functionality
/// </summary> 
public class RunUtility
{
    public const float METERS_PER_MILE = 1609.34f;

    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="o2Cost">in mL/kg/min</param>
    /// <returns>Speed in miles per sec</returns>
    public static float VDOTToSpeed(float o2Cost, float grade)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        float c = -4.6f - o2Cost;
        const float b_squared = b * b;
        float four_a_c = 4 * a * c;
        const float two_a = 2 * a;

        return (-b + Mathf.Sqrt(b_squared - four_a_c)) / (two_a * METERS_PER_MILE * 60f) / GameManager.Instance.GradeAdjustedPaceCurve.Evaluate(grade);
    }

    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="speed">in miles per sec</param>
    /// <returns>in mL/kg/min</returns>
    public static float SpeedToVDOT(float speed, float grade)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        const float c = -4.6f;

        speed = speed * GameManager.Instance.GradeAdjustedPaceCurve.Evaluate(grade) * METERS_PER_MILE * 60f;

        return a * Mathf.Pow(speed, 2) + b * speed + c;
    }

    /// <param name="milesPerSec">Speed in miles per second</param>
    /// <returns>A pretty mile pace string in the format of x:xx</returns>
    public static string SpeedToMilePaceString(float milesPerSec)
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

    public static string SorenessToStatusString(float exhaustion)
    {
        string status;
        if (exhaustion < 200)
        {
            status = "Well Rested";
        }
        else if (exhaustion < 400)
        {
            status = "Lightly Fatigued";
        }
        else if (exhaustion < 600)
        {
            status = "Worked Over";
        }
        else if (exhaustion < 800)
        {
            status = "Tired";
        }
        else
        {
            status = "Exhausted";
        }

        return status;
    }

    /// <summary>
    /// Steps the change in a runner's desired VO2 taking into consideration current speed and soreness
    /// </summary>
    /// <param name="runner">The runner whose VO2 we're stepping</param>
    /// <param name="state">The current state of the Runner</param>
    /// <param name="targetVDOTPercent">The target VO2 for this runner</param>
    /// <param name="maxSoreness">The maximum amount of soreness for this runner</param>
    /// <returns>The new desired VO2</returns>
    public static float StepRunnerVO2(Runner runner, RunnerState state, float targetVDOTPercent, float maxSoreness)
    {
        // if we're not past the very first step of the simulation, just return the current desired VO2 and we'll update from there
        if (state.intervalDistance == 0)
        {
            return state.desiredVO2;
        }

        //TODO need to make this more responsive overall or just make it more responsive when the grade is going nuts! FIXME
        // std dev for the roll for how much pace will change in percent of current run VO2
        float paceChangeStdDev = .02f;
        // the base mean for the roll for how much pace will change in percent of current run VO2
        // example: paceMeanMagnitude = .02 and the roll comes up on the mean(disregarding other factors), pace will increase by 2% 
        float paceChangeMeanMagnitude = .5f;

        // a number between 0 and 1 that shows how sore we are, 0 = not sore, 1 = most sore
        float normalizedSorenessFeel = Mathf.Pow(Mathf.Clamp01(Mathf.InverseLerp(0, maxSoreness, state.shortTermSoreness + runner.longTermSoreness)), runner.currentGrit);
        // a smoothed number between -1 and 1 that represents the magnitude and direction of the soreness effect on pace
        // low numbers mean you can speed up and high numbers mean you gotta slow down
        float sorenessPaceChangeFactor = Mathf.Pow(Mathf.Lerp(-1, 1, normalizedSorenessFeel), 5);
        float sorenessConfidenceModifier = runner.currentConfidence * .1f;
        sorenessPaceChangeFactor -= sorenessConfidenceModifier;

        // number that represents the percentile of the last interval's VO2 usage
        // example: runner VO2 = 50, last interval was at 45 V02 pace, intervalVO2Percent would then be .9
        SimulationIntervalData lastIntervalData = state.simulationIntervalList[state.simulationIntervalList.Count - 1];
        float intervalVDOTPercent = lastIntervalData.vdot / runner.GetCurrentVDOTMax();
        // how far off the last interval was from coach's guidance in percent of VO2
        // low numbers mean you're slow and high numbers mean you're fast
        float vdotPaceChangeFactor = intervalVDOTPercent - targetVDOTPercent;
        float vdotConfidenceModifier = runner.currentConfidence * .01f;
        vdotPaceChangeFactor -= vdotConfidenceModifier;

        // you can go look at desmos for the shape of what this is trying to do
        // but the general idea is that vdot will pll you in the opposite direction of where you are relative to the goal pace
        // and soreness will not influence you at all if you're not that sore, but then cubicly try to make you slower as you get more sore
        float paceChangeMean = paceChangeMeanMagnitude * (-.45f * Mathf.Pow(sorenessPaceChangeFactor + .35f, 3) + -2.2f * vdotPaceChangeFactor);

        // do the roll then adjust vo2
        float roll = CNExtensions.RandGaussian(paceChangeMean, paceChangeStdDev);

        Debug.Log($"Runner:{runner.Name}\tSoreness Factor:{sorenessPaceChangeFactor}\tVDOT Factor:{vdotPaceChangeFactor}\tTarget VDOT:{targetVDOTPercent}\tCurrent VDOT:{intervalVDOTPercent}\tNext VDOT:{(lastIntervalData.vdot + roll * runner.GetCurrentVDOTMax()) / runner.GetCurrentVDOTMax()}\tRoll:{roll}");

        return (lastIntervalData.vdot + roll * runner.GetCurrentVDOTMax()) / lastIntervalData.economy;
    }

    /// <summary>
    /// Runs one iteration of the gravity grouping model for one runner
    /// </summary>
    /// <param name="runner">The runner to run the model for</param>
    /// <param name="state">The state of that runner</param>
    /// <param name="runnerStates">The dictionary of other runners to run the model with</param>
    /// <param name="targetVDOTPercent">The current target VO2 for the given runner</param>
    /// <param name="intervalLength">The length of the current interval of the run</param>
    /// <returns>The new desired speed for the runner as determined by the model</returns>
    public static float RunGravityModel(Runner runner, RunnerState state, Dictionary<Runner, RunnerState> runnerStates, float targetVDOTPercent, float intervalLength)
    {
        // if this runner is done, continue
        if (state.intervalDistance >= intervalLength)
        {
            return state.currentSpeed;
        }

        float runningAverage = 0f;
        float weightTotal = 0f;
        //go through each runner and add to the running average + total
        foreach (KeyValuePair<Runner, RunnerState> otherKvp in runnerStates)
        {
            //skip if current==other or if this runner is already done running
            if (runner == otherKvp.Key || otherKvp.Value.intervalDistance >= intervalLength)
            {
                continue;
            }

            // use a gravity model so runners closer together effect each other more than runners far away
            float difference = Mathf.Abs(otherKvp.Value.currentSpeed - state.currentSpeed) + Mathf.Abs(otherKvp.Value.totalDistance - state.totalDistance);
            // if the difference between the runners is super small, cap weight at a high amount
            float weight = (difference < .001f) ? 1000000f : 1f / Mathf.Max(Mathf.Pow(difference, 2), Mathf.Epsilon);
            runningAverage += weight * otherKvp.Value.currentSpeed;
            weightTotal += weight;
        }

        // if we are effected by any runners, figure out how they effect our current speed
        // the last runner left on the route will not be effected by anyone so they just run at their desired speed
        if (weightTotal > 0)
        {
            state.currentSpeed = Mathf.Lerp(state.currentSpeed, runningAverage / weightTotal, 1f - (.75f * targetVDOTPercent));
        }

        return state.currentSpeed;
    }

    /// <summary>
    /// Steps the run state for all runners and logs it to the console
    /// </summary>
    /// <param name="runnerStates">The Dictionary of RunnerStates to step</param>
    /// <param name="timePassed">The amount of time passed in seconds for this simulation step</param>
    /// <param name="intervalLength">The length of the current interval</param>
    /// <param name="totalLength">The total length of the currrent run. This will be the same as intervalLength for non-interval workout runs.</param>
    /// <returns>The updated state dictionary</returns>
    public static Dictionary<Runner, RunnerState> StepRunState(Dictionary<Runner, RunnerState> runnerStates, RouteLineData lineData, float timePassed, float intervalLength, float totalLength)
    {
        string stateString = "";

        List<KeyValuePair<Runner, RunnerState>> sortedRunnerStates = runnerStates.ToList();
        sortedRunnerStates.Sort((kvp1, kvp2) => SortRunnerState(kvp1.Value, kvp2.Value));
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            float grade = 0;

            //if a runner is not done, keep incrementing them along the route
            if (state.intervalDistance < intervalLength)
            {
                float lastIntervalDistance = state.intervalDistance;
                state.intervalDistance += state.currentSpeed * timePassed;
                state.intervalDistance = Mathf.Min(state.intervalDistance, intervalLength);

                state.intervalPercentDone = state.intervalDistance / intervalLength;

                state.totalDistance += state.intervalDistance - lastIntervalDistance;
                state.totalDistance = Mathf.Min(state.totalDistance, totalLength);
                
                state.timeInSeconds += timePassed;

                state.totalPercentDone = state.totalDistance / totalLength;

                // calculating simulation interval data so we can update soreness, hydration, and calories
                int latestIntervalIndex = state.simulationIntervalList.Count - 1;
                float simulationIntervalDistance = state.totalDistance - state.simulationIntervalList[latestIntervalIndex].distanceInMiles;
                float simulationIntervalTimeInSeconds = state.timeInSeconds - state.simulationIntervalList[latestIntervalIndex].timeInSeconds;
                float simulationIntervalTimeInMinutes = simulationIntervalTimeInSeconds / 60f;
                float simulationIntervalMilesPerSecond = simulationIntervalDistance / simulationIntervalTimeInSeconds;

                grade = lineData.GetGrade(state.totalDistance, simulationIntervalDistance);
                float simulationIntervalVDOT = SpeedToVDOT(simulationIntervalMilesPerSecond, grade);
                float simulationIntervalEconomy = runner.CalculateRunEconomy(state);

                state.shortTermSoreness += runner.CalculateShortTermSoreness(simulationIntervalVDOT, simulationIntervalTimeInMinutes);
                state.hydrationCost += runner.CalculateHydrationCost(simulationIntervalVDOT, simulationIntervalTimeInMinutes);
                state.calorieCost += runner.CalculateCalorieCost(simulationIntervalVDOT, simulationIntervalTimeInMinutes);
                
                state.simulationIntervalList.Add(new SimulationIntervalData
                {
                    distanceInMiles = state.totalDistance,
                    timeInSeconds = state.timeInSeconds,
                    vdot = simulationIntervalVDOT,
                    economy = simulationIntervalEconomy
                });
            }

            stateString += $"Name: {runner.Name}\tDistance: {state.totalDistance}\tGrade: {grade}\tSpeed: {SpeedToMilePaceString(state.currentSpeed)}\tSoreness: {state.shortTermSoreness + runner.longTermSoreness} ({state.shortTermSoreness},{runner.longTermSoreness})\n";
        }
        Debug.Log(stateString);

        return runnerStates;
    }
    
    /// <summary>
    /// A comparison method for RunnerStates that sorts first based on totalDistance, then by time
    /// </summary>
    public static int SortRunnerState(RunnerState state1, RunnerState state2)
    {
        if (state1.totalDistance == state2.totalDistance)
        {
            if (state1.timeInSeconds == state2.timeInSeconds)
            {
                return 0;
            }
            else if (state1.timeInSeconds < state2.timeInSeconds)
                return -1;
            else
                return 1;
        }
        else if (state1.totalDistance > state2.totalDistance)
            return -1;
        else
            return 1;
    }
}
