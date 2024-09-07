using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Runner
{
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

    public void Initialize()
    {
        currentVO2Max = minVO2Max;
    }

    public void IncreaseExperience(float exp)
    {
        experience += exp;
    }

    public void UpdateVO2(float vo2Update)
    {
        vo2Update /= 1000f;
        float normalizedVO2 = Mathf.InverseLerp(minVO2Max, maxVO2Max, currentVO2Max);
        float slope = -10f * Mathf.Pow(normalizedVO2 - 1, 9);
        currentVO2Max += vo2Update * slope;
    }

    public void UpdateExhaustion(float exhaustion)
    {
       this.exhaustion += exhaustion;
    }
}
