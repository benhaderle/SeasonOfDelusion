using System;
using System.Collections;
using System.Collections.Generic;
using CreateNeptune;
using UnityEngine;

/// <summary>
/// Holds variables and functionality relevant to runners
/// </summary>
[Serializable]
public class Runner
{
    /// <summary>
    /// Putting these here bc i don't know where else to put it
    /// </summary> 
    private const float MAX_FORM = 100;
    private const float MAX_STRENGTH = 100;
    private const float MAX_SLEEP = 10;
    private const float MIN_SLEEP = 0;
    private const float MAX_SHORT_TERM_CALORIES = 3000;
    /// <summary>
    /// An SO with shared variables between all runners
    /// </summary>
    private RunnerCalculationVariables variables;
    [SerializeField] private string firstName;
    public string FirstName => firstName;
    [SerializeField] private string lastName;
    public string LastName => lastName;
    /// <value>User facing string formatted as "FirstName LastName"</value>
    public string Name => $"{firstName} {lastName}";

    #region Stats

    [SerializeField] private float weight;

    /// <summary>
    /// This runner's minVO2Max. currentVO2Max will never go below this.
    /// </summary>
    [SerializeField] private float minVO2Max;
    /// <summary>
    /// The runner's maxVO2Max. currentVO2Max will never go above this.
    /// </summary> 
    [SerializeField] private float maxVO2Max;
    /// <summary>
    /// The runner's current VO2Max.
    /// </summary>    
    private float currentVO2Max;
    public float CurrentVO2Max => currentVO2Max;

    [SerializeField] private float minStrength;
    [SerializeField] private float maxStrength;
    private float currentStrength;
    public float CurrentStrength => currentStrength;
    /// <summary>
    /// The current rate of strength change. Changes depending on run performance
    /// </summary>
    private float strengthChangeRate;

    [SerializeField] private float minForm;
    [SerializeField] private float maxForm;
    private float currentForm;
    public float CurrentForm => currentForm;
    private int daysSinceFormPractice;

    /// <summary>
    /// A number >= 0 that roughly represents cumulative lifetime miles for this runner
    /// </summary>
    private float experience;
    public float Experience => experience;
    [SerializeField] private float minNutrition;
    [SerializeField] private float maxNutrition;
    private float currentNutrition;
    public float CurrentNutrition => currentNutrition;
    [SerializeField] private float recovery;
    public float Recovery => recovery;
    private float grit;
    public float Grit => grit;
    private float school;
    public float School => school;

    // TODO: impromptu list of stats that might get implemented at some point
    // public int wit { get; private set; }
    // public int spirit { get; private set; }
    // public int smarts { get; private set; }

    // private float emotion;
    // private float intuition;
    private float sleepStatus;
    public float SleepStatus => sleepStatus;
    private float shortTermCalories;
    public float ShortTermCalories => shortTermCalories;
    private float longTermCalories;
    public float LongTermCalories => longTermCalories;
    private float hydrationStatus;
    public float HydrationStatus => hydrationStatus;
    private float longTermSoreness;
    public float LongTermSoreness => longTermSoreness;

    #endregion

    public Runner()
    {
    }

    /// <summary>
    /// Initializes this Runner
    /// </summary>
    /// <param name="variables">The variables to use for calculating updates to Runner stats</param> 
    public void Initialize(RunnerCalculationVariables variables)
    {
        currentVO2Max = minVO2Max;
        currentForm = minForm + 10;
        currentStrength = minStrength + 10;
        strengthChangeRate = 1;
        currentNutrition = minNutrition + 10;
        hydrationStatus = 4f;
        longTermCalories = 100000;
        shortTermCalories = MAX_SHORT_TERM_CALORIES;
        sleepStatus = 10;
        this.variables = variables;
    }

    #region Post Run Functions

    /// <summary>
    /// Updates this Runner's stats given the information in runState. Assumes the run is done.
    /// </summary>
    /// <param name="runState">The state of this Runner after a run is finished.</param>
    public RunnerUpdateRecord PostRunUpdate(RunnerState runState)
    {
        // register the old values so we can show how much they changed
        float oldVO2 = currentVO2Max;
        float oldStrength = currentStrength;

        // update hydration and calories
        hydrationStatus -= runState.hydrationCost;
        float longTermCalorieCost = Mathf.Max(0, runState.calorieCost - shortTermCalories);
        shortTermCalories = Mathf.Max(0, shortTermCalories - runState.calorieCost);
        longTermCalories = Mathf.Max(0, longTermCalories - longTermCalorieCost);

        float milesPerSecond = runState.totalDistance / runState.timeInSeconds;
        float runVO2 = RunUtility.SpeedToOxygenCost(milesPerSecond) / CalculateRunEconomy();
        float timeInMinutes = runState.timeInSeconds / 60f;

        // experience is a function of cumulative miles run
        UpdateExperiencePostRun(runState.totalDistance);

        // VO2 is moved up or down depending on how far away you were from 90% of your VO2
        UpdateVO2PostRun(runVO2, timeInMinutes);

        // exhaustion changes based off of how far away you were from your recovery VO2
        UpdateLongTermSorenessPostRun(runVO2, timeInMinutes);

        // strength rate is moved up or down depending on how far away you were from 75% of your VO2
        UpdateStrengthPostRun(runState.distanceTimeSimulationIntervalList);

        Debug.Log($"Name: {Name}\tOld VO2: {oldVO2}\tNew VO2: {CurrentVO2Max}\tOld Strength: {oldStrength}\tNew Strength: {CurrentStrength}\tShort Term Calories: {shortTermCalories}\t Long Term Calories: {longTermCalories}");

        return new RunnerUpdateRecord
        {
            vo2Change = currentVO2Max - oldVO2
        };
    }

    /// <summary>
    /// Updates the Runner's VO2Max with the given runVO2 and time it was run for
    /// </summary>
    /// <param name="runVO2">The VO2 in mL/kg/min for the last run</param>
    /// <param name="timeInMinutes">The length of the run in minutes</param> 
    private void UpdateVO2PostRun(float runVO2, float timeInMinutes)
    {
        // the percent away from the improvement threshold
        float vo2ImprovementGap = (runVO2 / (CurrentVO2Max * variables.VO2ImprovementThreshold)) - 1f;

        // go look at desmos if you want to see the shape of this graph
        // basic idea is that VO2 goes up if you ran harder or longer, goes down if you ran slower or shorter
        float vo2Update = (variables.CubicVO2Slope * timeInMinutes * Mathf.Pow(vo2ImprovementGap, 3))
            + (variables.LinearVO2Slope * timeInMinutes * (vo2ImprovementGap + variables.LinearVO2Offset))
            + variables.ConstantVO2Offset;

        // one last linear tune
        vo2Update *= variables.VO2UpdateFactor;

        // get the slope of change given the Runner's current VO2
        // VO2 changes are more sensitive when the Runner's VO2 is lower
        float normalizedVO2 = Mathf.InverseLerp(minVO2Max, maxVO2Max, currentVO2Max);
        float slope = -10f * Mathf.Pow(normalizedVO2 - 1, 9);

        // final add and clamp
        currentVO2Max += vo2Update * slope;
        currentVO2Max = Mathf.Clamp(currentVO2Max, minVO2Max, maxVO2Max);
    }

    /// <summary>
    /// Increases experience by the amount provided
    /// </summary>
    /// <param name="exp">The amount of experience to add</param>
    private void UpdateExperiencePostRun(float exp)
    {
        experience += exp;
    }

    // <summary>
    /// Updates the Runner's Exhaustion with the given runVO2 and time it was run for
    /// </summary>
    /// <param name="runVO2">The VO2 in mL/kg/min for the last run</param>
    /// <param name="timeInMinutes">The length of the run in minutes</param> 
    private void UpdateLongTermSorenessPostRun(float runVO2, float timeInMinutes)
    {
        longTermSoreness += CalculateLongTermSoreness(runVO2, timeInMinutes);
    }

    /// <summary>
    /// Uses the list of interval distances and times to update the strengthChangeRate and then uses the strengthChangeRate to update currentStrength
    /// </summary>
    /// <param name="distanceTimeSimulationIntervalList">The list of distance-time intervals for the run</param>
    private void UpdateStrengthPostRun(List<(float, float)> distanceTimeSimulationIntervalList)
    {
        // the percent threshold of VO2 at which strength starts increasing
        float strengthUtilisationVO2Threshold = .75f;

        // running sums so we can get an average utilisation for all intervals
        float strengthUtilisationSum = 0;
        float timeSum = 0;
        
        // go through each interval and add the utilisation to the sum
        for (int i = 0; i < distanceTimeSimulationIntervalList.Count - 1; i++)
        {
            float intervalDistance = distanceTimeSimulationIntervalList[i + 1].Item1 - distanceTimeSimulationIntervalList[i].Item1;
            float intervalTime = distanceTimeSimulationIntervalList[i + 1].Item2 - distanceTimeSimulationIntervalList[i].Item2;

            float intervalVO2 = RunUtility.SpeedToOxygenCost(intervalDistance / intervalTime);

            //TODO: this should take into account elevation in the future
            float intervalStrengthUtilisation = intervalVO2 / (strengthUtilisationVO2Threshold * currentVO2Max);
            // if we used strength past the VO2 threhsold, make the utilisation exponential
            // else, make the decrease slowly linear
            if (intervalStrengthUtilisation > 1)
            {
                intervalStrengthUtilisation *= intervalStrengthUtilisation;
            }
            else
            {
                intervalStrengthUtilisation = (.1f * intervalStrengthUtilisation) + .9f;
            }

                // add to the weighted sum
                strengthUtilisationSum += intervalStrengthUtilisation * intervalTime;
            timeSum += intervalTime;
        }

        // lerp the rate towards today's rate
        strengthChangeRate = Mathf.Lerp(strengthChangeRate, strengthUtilisationSum / timeSum, 1/3f);

        // update strength
        currentStrength *= strengthChangeRate;
        currentStrength = Mathf.Clamp(currentStrength, minStrength, maxStrength);
    }

    #endregion

    #region End of Day Functions

    /// <summary>
    /// Performs any EOD updates to runner stats. Currently effects form, hydration, calories, sleep, and soreness
    /// </summary>
    public void OnEndDay()
    {
        UpdateFormEOD();

        // nutrition roll effects hydration and calorie recovery
        float nutritionRoll = CNExtensions.RandGaussian(currentNutrition, 10);

        // hydration updates linearly based on the roll
        hydrationStatus += 2f * Mathf.Clamp(nutritionRoll / 50f, 0, 2);

        float shortTermCalorieDeficit = shortTermCalories - MAX_SHORT_TERM_CALORIES;
        // recover calories based on the deficit (ie how hungry you are) and the nutrition roll
        float shortTermCaloriesToAdd = shortTermCalorieDeficit * (nutritionRoll / 80f);
        // any calories in excess will be stored as long term calories
        float longTermCaloriesToAdd = (shortTermCaloriesToAdd - shortTermCalorieDeficit) * .5f;
        // add up our new calories
        shortTermCalories += shortTermCaloriesToAdd;
        // if we have long term calories to add, cap shortTermCalories and add the long term ones
        if (longTermCaloriesToAdd > 0)
        {
            shortTermCalories = 3000;
            longTermCalories += longTermCaloriesToAdd;
        }

        // recovery roll effects sleep and soreness
        float recoveryRoll = CNExtensions.RandGaussian(recovery, 10);
        // use the roll to approximate how many hours fo sleep you have
        float hoursOfSleep = recoveryRoll * .1f - 6;
        // recover sleep and clamp between reasonable values
        sleepStatus += hoursOfSleep;
        sleepStatus = Mathf.Clamp(sleepStatus, MIN_SLEEP, MAX_SLEEP);

        // recover soreness a bit over night
        // the recovery roll effects the how much soreness is linearly recovered
        float linearRecoverySlope = 1f - (recoveryRoll * .002f);
        // in addition to the linear amount lost, there's also a constant amount lost
        longTermSoreness = (linearRecoverySlope * longTermSoreness) - variables.DayEndLongTermSorenessRecovery;
        longTermSoreness = Mathf.Max(0, longTermSoreness);
    }

    /// <summary>
    /// Updates form and form related stats at the end of the day
    /// </summary> 
    private void UpdateFormEOD()
    {
        // decrement form based on how long it's been since we practiced
        currentForm -= daysSinceFormPractice;
        currentForm = Mathf.Max(currentForm, minForm);

        // increment the counter for how long it's been since we practiced
        daysSinceFormPractice++;

        // if we're 90% of our maxForm, we get to raise the floor of our form potential
        if (currentForm - minForm >= (maxForm - minForm) * .9f)
        {
            minForm++;
            minForm = Mathf.Min(minForm, maxForm - 1);
        }
    }

    #endregion

    #region Calculation Methods

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of short term soreness for this run segment</returns>
    public float CalculateShortTermSoreness(float runVO2, float timeInMinutes)
    {
        return .001f * Mathf.Pow(runVO2, 2) * timeInMinutes;
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of long term soreness for this run segment</returns>
    private float CalculateLongTermSoreness(float runVO2, float timeInMinutes)
    {
        // go look at desmos if you want to see the shape of this graph
        // basic idea is that longTermSoreness goes up if you ran harder or longer, goes down if you ran slower or shorter
        float longTermSorenessGap = (runVO2 / (currentVO2Max * variables.LongTermSorenessVO2Threshold)) - 1f;
        float longTermSoreness = (variables.CubicLongTermSorenessSlope * timeInMinutes * Mathf.Pow(longTermSorenessGap, 3))
            + (variables.LinearLongTermSorenessSlope * timeInMinutes * (longTermSorenessGap + variables.LinearLongTermSorenessOffset))
            + (variables.LinearLongTermSorenessTimeSlope * (timeInMinutes + variables.LinearLongTermSorenessTimeOffset));

        return longTermSoreness;
    }

    /// <summary>
    /// Calculates the economy of the current runner under the given circumstances
    /// </summary>
    /// <param name="hydration">How much hydration the runner has between 0 and 1</param>
    /// <param name="calories">How many calories the runner has between 0 and 1</param>
    /// <returns>A number between 0 and 1 representing the runner's economy under the given circumstances</returns>
    private float CalculateRunEconomy(float hydration, float calories)
    {
        // right now everything is equally weighted, but that might change
        float formWeight = .25f;
        float strengthWeight = .25f;
        float hydrationWeight = .25f;
        float calorieWeight = .25f;

        return formWeight * Mathf.Sqrt(currentForm * Mathf.InverseLerp(MIN_SLEEP, MAX_SLEEP, sleepStatus) / MAX_FORM) +
         strengthWeight * Mathf.Sqrt(currentStrength / MAX_STRENGTH) +
         hydrationWeight * Mathf.Clamp01(hydration) +
         calorieWeight * Mathf.Min(1, calories);
    }

    /// <returns>A number between 0 and 1 representing the runner's economy under the current internal circumstances</returns>
    public float CalculateRunEconomy()
    {
        return CalculateRunEconomy(hydrationStatus, shortTermCalories + longTermCalories);
    }

    /// <returns>A number between 0 and 1 representing the runner's economy under the given circumstances</returns>
    public float CalculateRunEconomy(RunnerState state)
    {
        return CalculateRunEconomy(hydrationStatus - state.hydrationCost, shortTermCalories + longTermCalories - state.calorieCost);
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of hydration cost for this run segment</returns>
    public float CalculateHydrationCost(float runVO2, float timeInMinutes)
    {
        // .02 is .02L per minute of water used per minute of running on average
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * .02f;
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of calories cost for this run segment</returns>
    public float CalculateCalorieCost(float runVO2, float timeInMinutes)
    {
        // 10 is the amount of calories burned per minute at 70% of VO2Max
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * 10f;
    }

    #endregion
}

public struct RunnerUpdateRecord
{
    public float vo2Change;
}
