using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Runner
{
    private RunnerCalculationVariables variables;
    [SerializeField] private string firstName;
    public string FirstName => firstName;
    [SerializeField] private string lastName;
    public string LastName => lastName;
    public string Name => $"{firstName} {lastName}";

    #region Stats
    [SerializeField] private float minVO2Max;
    [SerializeField] private float maxVO2Max;
    private float currentVO2Max;
    public float CurrentVO2Max => currentVO2Max;
    public int endurance { get; private set; }
    public int hills { get; private set; }
    public int discipline { get; private set; }
    public int grit { get; private set; }
    public int wit { get; private set; }
    public int spirit { get; private set; }
    public int smarts { get; private set; }

    private float musclesHealth;
    private float skeletalHealth;
    private float respiratoryHealth;
    private float cardioHealth;
    private float experience;
    public float Experience => experience;
    private float exhaustion;
    public float Exhaustion => exhaustion;
    private float emotion;
    private float intuition;
    private float sleep;
    private float nutrition;
    private float hydration;

    #endregion

    public Runner()
    {
    }

    public void Initialize(RunnerCalculationVariables variables)
    {
        currentVO2Max = minVO2Max;
        this.variables = variables;
    }

    public void PostRunUpdate(RunnerState runState)
    {
        float oldVO2 = currentVO2Max;
        float milesPerSecond = runState.distance / runState.timeInSeconds;
        float runVO2 = RunController.SpeedToOxygenCost(milesPerSecond);
        float timeInMinutes = runState.timeInSeconds / 60f;

        // experience is a function of cumulative miles run
        IncreaseExperience(runState.distance);

        UpdateVO2(runVO2, timeInMinutes);

        // exhaustion changes based off of how far away you were from your recovery VO2
        UpdateExhaustion(runVO2, timeInMinutes);
       
        Debug.Log($"Name: {Name}\tExhaustion: {Exhaustion}\tOld VO2: {oldVO2}\tNew VO2: {CurrentVO2Max}");
    }

    public void OnEndDay()
    {
        exhaustion = Mathf.Max(0, exhaustion - variables.DayEndExhaustionRecovery);
    }

    private void IncreaseExperience(float exp)
    {
        experience += exp;
    }

    private void UpdateVO2(float runVO2, float timeInMinutes)
    {
        float vo2ImprovementGap = (runVO2 / (CurrentVO2Max * variables.VO2ImprovementThreshold)) - 1f;

        float vo2Update = (variables.CubicVO2Slope * timeInMinutes * Mathf.Pow(vo2ImprovementGap, 3)) 
            + (variables.LinearVO2Slope * timeInMinutes * (vo2ImprovementGap + variables.LinearVO2Offset)) 
            + variables.ConstantVO2Offset;

        vo2Update *= variables.VO2UpdateFactor;

        float normalizedVO2 = Mathf.InverseLerp(minVO2Max, maxVO2Max, currentVO2Max);
        float slope = -10f * Mathf.Pow(normalizedVO2 - 1, 9);

        currentVO2Max += vo2Update * slope;
        currentVO2Max = Mathf.Clamp(currentVO2Max, minVO2Max, maxVO2Max);
    }

    private void UpdateExhaustion(float runVO2, float timeInMinutes)
    {
        float exhaustionGap = (runVO2 / (currentVO2Max * variables.ExhaustionVO2Threshold)) - 1f;
        float exhaustionUpdate = (variables.CubicExhaustionSlope * timeInMinutes * Mathf.Pow(exhaustionGap, 3)) 
            + (variables.LinearExhaustionSlope * timeInMinutes * (exhaustionGap + variables.LinearExhaustionOffset)) 
            + variables.ConstantExhaustionOffset;
        
        exhaustion += exhaustionUpdate;
    }
}
