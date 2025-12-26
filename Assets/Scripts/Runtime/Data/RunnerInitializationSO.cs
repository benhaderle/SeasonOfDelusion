using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRunnerInitializationSO", menuName = "ScriptableObjects/RunnerInitializationSO")]
public class RunnerInitializationSO : ScriptableObject
{
    public string firstName;
    public  string lastName;
    public int level = 1;
    public Sprite[] characterSprites;
    public RunnerSaveDataSO runnerSaveData;
    public float initialVO2Max = 50;
    public float vo2ImprovementMagnitude = .15f;
    public float initialStrength;
    public float strengthImprovementMagnitude;
    public float initialForm;
    public float initialGrit = 1;
    public float initialRecovery;
    public float initialAcademics;
    public float initialConfidence = 0;
}
