using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds variables and functionality relevant to runners
/// </summary>
[Serializable]
public class Runner
{
    /// <summary>
    /// An SO with shared variables between all runners
    /// </summary>
    private RunnerCalculationVariables variables;
    [SerializeField] private string firstName;
    public string FirstName => firstName;
    [SerializeField] private string lastName;
    public string LastName => lastName;
    /// <value>FirstName LastName</value>
    public string Name => $"{firstName} {lastName}";

    #region Stats
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
    // public int endurance { get; private set; }
    // public int hills { get; private set; }
    // public int discipline { get; private set; }
    // public int grit { get; private set; }
    // public int wit { get; private set; }
    // public int spirit { get; private set; }
    // public int smarts { get; private set; }

    // private float musclesHealth;
    // private float skeletalHealth;
    // private float respiratoryHealth;
    // private float cardioHealth;
    /// <summary>
    /// A number >= 0 that roughly represents cumulative lifetime miles for this runner
    /// </summary>
    private float experience;
    public float Experience => experience;
    /// <summary>
    /// A number >= 0 that represents how tired a player at the moment
    /// </summary>
    private float exhaustion;
    public float Exhaustion => exhaustion;
    // private float emotion;
    // private float intuition;
    // private float sleep;
    // private float nutrition;
    // private float hydration;

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
        this.variables = variables;
    }

    /// <summary>
    /// Updates this Runner's stats given the information in runState. Assumes the run is done.
    /// </summary>
    /// <param name="runState">The state of this Runner after a run is finished.</param>
    public void PostRunUpdate(RunnerState runState)
    {
        float oldVO2 = currentVO2Max;
        float milesPerSecond = runState.distance / runState.timeInSeconds;
        float runVO2 = RunUtility.SpeedToOxygenCost(milesPerSecond);
        float timeInMinutes = runState.timeInSeconds / 60f;

        // experience is a function of cumulative miles run
        IncreaseExperience(runState.distance);

        // VO2 is moved up or down depending on how far away you were from 90% of your VO2
        UpdateVO2(runVO2, timeInMinutes);

        // exhaustion changes based off of how far away you were from your recovery VO2
        UpdateExhaustion(runVO2, timeInMinutes);
       
        Debug.Log($"Name: {Name}\tExhaustion: {Exhaustion}\tOld VO2: {oldVO2}\tNew VO2: {CurrentVO2Max}");
    }

    /// <summary>
    /// Performs any EOD updates to runner stats.
    /// </summary>
    public void OnEndDay()
    {
        // recover a bit over night
        exhaustion = Mathf.Max(0, exhaustion - variables.DayEndExhaustionRecovery);
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
    private void UpdateExhaustion(float runVO2, float timeInMinutes)
    {
        // add it up
        exhaustion += CalculateExhaustion(runVO2, timeInMinutes);
    }

    public float CalculateExhaustion(float runVO2, float timeInMinutes)
    {
        // go look at desmos if you want to see the shape of this graph
        // basic idea is that exhaustion goes up if you ran harder or longer, goes down if you ran slower or shorter
        float exhaustionGap = (runVO2 / (currentVO2Max * variables.ExhaustionVO2Threshold)) - 1f;
        float exhaustionUpdate = (variables.CubicExhaustionSlope * timeInMinutes * Mathf.Pow(exhaustionGap, 3))
            + (variables.LinearExhaustionSlope * timeInMinutes * (exhaustionGap + variables.LinearExhaustionOffset))
            + variables.ConstantExhaustionOffset;

        return exhaustionUpdate;
    }
}
