using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRunnerSaveData", menuName = "ScriptableObjects/RunnerSaveData")]
public class RunnerSaveDataSO : ScriptableObject
{
    public RunnerSaveData data = new();
    public void Initialize(float minVO2Max, float minForm, float minStrength, float minNutrition, float maxShortTermCalories)
    {
        data.initialized = true;

        data.currentVO2Max = minVO2Max;
        data.currentForm = minForm + 10;
        data.currentStrength = minStrength + 10;
        data.strengthChangeRate = 1;
        data.currentNutrition = minNutrition + 10;
        data.hydrationStatus = 4f;
        data.longTermCalories = 100000;
        data.shortTermCalories = maxShortTermCalories;
        data.sleepStatus = 10;
    }

}

[Serializable]
public class RunnerSaveData
{
    public bool initialized;
    public string firstName;
    public string lastName;
    public string teamName;
    public float weight;
    public float currentVO2Max;
    public float currentStrength;
    public float strengthChangeRate;
    public float currentForm;
    public int daysSinceFormPractice;
    public float experience;
    public float currentNutrition;
    public float recovery;
    public float grit;
    public float school;
    public float sleepStatus;
    public float hydrationStatus;
    public float shortTermCalories;
    public float longTermCalories;
    public float longTermSoreness;
}
