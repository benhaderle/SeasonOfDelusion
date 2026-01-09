using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRunnerSaveData", menuName = "ScriptableObjects/RunnerSaveData")]
public class RunnerSaveDataSO : ScriptableObject
{
    public RunnerSaveData data = new();

    public void Initialize(RunnerInitializationSO initializationSO, float maxShortTermCalories, string teamName)
    {
        data.initialized = true;

        data.firstName = initializationSO.firstName;
        data.lastName = initializationSO.lastName;
        data.teamName = teamName;

        data.level = initializationSO.level;
        data.experience = 0;

        data.currentVO2Max = initializationSO.initialVO2Max;
        data.currentStrength = initializationSO.initialStrength;
        data.currentForm = initializationSO.initialForm;
        data.currentGrit = initializationSO.initialGrit;
        data.currentRecovery = initializationSO.initialRecovery;
        data.currentAcademics = initializationSO.initialAcademics;

        data.vo2ImprovementMagnitude = initializationSO.vo2ImprovementMagnitude;
        data.strengthImprovementMagnitude = initializationSO.strengthImprovementMagnitude;

        data.hydrationStatus = 4f;
        data.longTermCalories = Runner.INIT_LONG_TERM_CALORIES;
        data.shortTermCalories = maxShortTermCalories;
        data.longTermSoreness = 0;
        data.sleepStatus = 10;
        data.confidence = initializationSO.initialConfidence;
    }
}

[Serializable]
public class RunnerSaveData
{
    public bool initialized;

    public string firstName;
    public string lastName;
    public string teamName;

    public int level;
    public int experience;

    public float weight;
    public float currentVO2Max;
    public float currentStrength;
    public float currentForm;
    public float currentRecovery;
    public float currentGrit;
    public float currentAcademics;

    public float vo2ImprovementMagnitude;
    public float strengthImprovementMagnitude;
    public float formImprovementMagnitude;
    public float gritImprovementMagnitude;
    public float recoveryImprovementMagnitude;

    public float sleepStatus;
    public float hydrationStatus;
    public float shortTermCalories;
    public float longTermCalories;
    public float longTermSoreness;
    public float confidence;
    

    // public float school;
}
