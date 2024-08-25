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

    [SerializeField] private float vo2Max;
    public float VO2Max => vo2Max;
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
    private float emotion;
    private float intuition;
    private float sleep;
    private float nutrition;
    private float hydration;

    #endregion

    public Runner()
    {
    }
}
