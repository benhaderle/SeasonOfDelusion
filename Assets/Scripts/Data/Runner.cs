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
    /// Putting this here bc i don't know where else to put it
    /// </summary> 
    private const float MAX_FORM = 100;
    private const float MAX_STRENGTH = 100;
    private const float MAX_SLEEP = 10;
    private const float MIN_SLEEP = 0;
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
    private float strengthMomentum;

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
        currentNutrition = minNutrition + 10;
        hydrationStatus = 4f;
        longTermCalories = 100000;
        shortTermCalories = 3000;
        sleepStatus = 10;
        this.variables = variables;
    }

    /// <summary>
    /// Updates this Runner's stats given the information in runState. Assumes the run is done.
    /// </summary>
    /// <param name="runState">The state of this Runner after a run is finished.</param>
    public RunnerUpdateRecord PostRunUpdate(RunnerState runState)
    {
        float oldVO2 = currentVO2Max;
        float oldStrength = currentStrength;

        hydrationStatus -= runState.hydrationCost;
        float longTermCalorieCost = Mathf.Max(0, runState.calorieCost - shortTermCalories);
        shortTermCalories = Mathf.Max(0, shortTermCalories - runState.calorieCost);
        longTermCalories = Mathf.Max(0, longTermCalories - longTermCalorieCost);

        float milesPerSecond = runState.distance / runState.timeInSeconds;
        float runVO2 = RunUtility.SpeedToOxygenCost(milesPerSecond) / CalculateRunEconomy();
        float timeInMinutes = runState.timeInSeconds / 60f;

        // experience is a function of cumulative miles run
        IncreaseExperience(runState.distance);

        // VO2 is moved up or down depending on how far away you were from 90% of your VO2
        UpdateVO2(runVO2, timeInMinutes);

        // exhaustion changes based off of how far away you were from your recovery VO2
        UpdateLongTermSoreness(runVO2, timeInMinutes);

        UpdateStrength(runState.distance, runVO2);
       
        Debug.Log($"Name: {Name}\tOld VO2: {oldVO2}\tNew VO2: {CurrentVO2Max}\tOld Strength: {oldStrength}\tNew Strength: {CurrentStrength}\tShort Term Calories: {shortTermCalories}\t Long Term Calories: {longTermCalories}");

        return new RunnerUpdateRecord
        {
            vo2Change = currentVO2Max - oldVO2
        };
    }

    /// <summary>
    /// Performs any EOD updates to runner stats.
    /// </summary>
    public void OnEndDay()
    {

        UpdateFormEOD();

        float nutritionRoll = CNExtensions.RandGaussian(currentNutrition, 10);
        hydrationStatus += 2f * Mathf.Clamp(nutritionRoll / 50f, 0, 2);

        float caloriesToAdd = (shortTermCalories - 3000) * (nutritionRoll / 80f);
        float longTermCaloriesToAdd = (caloriesToAdd - (shortTermCalories - 3000)) * .5f;

        shortTermCalories += caloriesToAdd;
        if(longTermCaloriesToAdd > 0)
        {
            shortTermCalories = 3000;
            longTermCalories += longTermCaloriesToAdd;
        }

        float recoveryRoll = CNExtensions.RandGaussian(recovery, 10);
        float hoursOfSleep = recoveryRoll * .1f - 6;
        sleepStatus += hoursOfSleep;
        sleepStatus = Mathf.Clamp(sleepStatus, MIN_SLEEP, MAX_SLEEP);

        // recover a bit over night
        //TODO: this should be effected by the recovery roll and should maybe be proportional to the amount of longterm soreness we already have
        float linearRecoverySlope = 1f - (recoveryRoll * .002f);
        longTermSoreness = Mathf.Max(0, (linearRecoverySlope * longTermSoreness) - variables.DayEndLongTermSorenessRecovery);
    }

    /// <summary>
    /// Increases experience by the amount provided
    /// </summary>
    /// <param name="exp">The amount of experience to add</param>
    private void IncreaseExperience(float exp)
    {
        experience += exp;
    }

    /// <summary>
    /// Updates the Runner's VO2Max with the given runVO2 and time it was run for
    /// </summary>
    /// <param name="runVO2">The VO2 in mL/kg/min for the last run</param>
    /// <param name="timeInMinutes">The length of the run in minutes</param> 
    private void UpdateVO2(float runVO2, float timeInMinutes)
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

    // <summary>
    /// Updates the Runner's Exhaustion with the given runVO2 and time it was run for
    /// </summary>
    /// <param name="runVO2">The VO2 in mL/kg/min for the last run</param>
    /// <param name="timeInMinutes">The length of the run in minutes</param> 
    private void UpdateLongTermSoreness(float runVO2, float timeInMinutes)
    {
        // add it up
        longTermSoreness += CalculateLongTermSoreness(runVO2, timeInMinutes);
    }
    
    public float CalculateShortTermSoreness(float runVO2, float timeInMinutes)
    {
        return .001f * Mathf.Pow(runVO2, 2) * timeInMinutes;
    }

    public float CalculateLongTermSoreness(float runVO2, float timeInMinutes)
    {
        // go look at desmos if you want to see the shape of this graph
        // basic idea is that exhaustion goes up if you ran harder or longer, goes down if you ran slower or shorter
        float exhaustionGap = (runVO2 / (currentVO2Max * variables.LongTermSorenessVO2Threshold)) - 1f;
        float exhaustionUpdate = (variables.CubicLongTermSorenessSlope * timeInMinutes * Mathf.Pow(exhaustionGap, 3))
            + (variables.LinearLongTermSorenessSlope * timeInMinutes * (exhaustionGap + variables.LinearLongTermSorenessOffset))
            + (variables.LinearLongTermSorenessTimeSlope * (timeInMinutes + variables.LinearLongTermSorenessTimeOffset));

        return exhaustionUpdate;
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
        if(currentForm - minForm >= (maxForm - minForm) * .9f)
        {
            minForm++;
            minForm = Mathf.Min(minForm, maxForm - 1);
        }
    }

    /// <summary>
    /// Updates strength and strength related stats at the end of the day
    /// </summary> 
    private void UpdateStrength(float distanceRun, float runVO2)
    {
        float strengthCostToday = distanceRun * 10 * runVO2 / (maxVO2Max  * .8f);
        float strengthDelta = strengthCostToday - currentStrength;
        strengthMomentum = Mathf.Lerp(strengthMomentum, strengthDelta, .15f);
        currentStrength += strengthMomentum;
        currentStrength = Mathf.Clamp(currentStrength, minStrength, maxStrength);
    }

    private float CalculateRunEconomy(float hydration, float calories)
    {
        float formWeight = .25f;
        float strengthWeight = .25f;
        float hydrationWeight = .25f;
        float calorieWeight = .25f;
        
        return formWeight * Mathf.Sqrt(currentForm * Mathf.InverseLerp(MIN_SLEEP, MAX_SLEEP, sleepStatus)/ MAX_FORM) + 
         strengthWeight * Mathf.Sqrt(currentStrength / MAX_STRENGTH) +
         hydrationWeight * Mathf.Clamp01(hydration) +
         calorieWeight * Mathf.Min(1, calories);
    }

    private float CalculateRunEconomy()
    {
        return CalculateRunEconomy(hydrationStatus, shortTermCalories + longTermCalories);
    }

    public float CalculateRunEconomy(RunnerState state)
    {
       return CalculateRunEconomy(hydrationStatus - state.hydrationCost, shortTermCalories + longTermCalories - state.calorieCost);
    }

    public float CalculateHydrationCost(float runVO2, float timeInMinutes)
    {
        // .02 is .02L per minute of water used per minute of running on average
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * .02f;
    }

    public float CalculateCalorieCost(float runVO2, float timeInMinutes)
    {
        // 10 is the amount of calories burned per minute at 70% of VO2Max
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * 10f;
    }
}

public struct RunnerUpdateRecord
{
    public float vo2Change;
}
