using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRunnerSaveData", menuName = "ScriptableObjects/RunnerSaveData")]
public class RunnerSaveDataSO : ScriptableObject
{
    public RunnerSaveData data = new();
    public void Initialize(float initialVO2Max, float initialForm, float initialStrength, float initialNutrition, float maxShortTermCalories)
    {
        data.initialized = true;
        
        data.level = 1;
        data.experience = 0;

        data.currentVO2Max = initialVO2Max;
        data.currentStrength = initialStrength;
        data.currentForm = initialForm + 10;
        data.currentNutrition = initialNutrition + 10;
        data.hydrationStatus = 4f;
        data.longTermCalories = 100000;
        data.shortTermCalories = maxShortTermCalories;
        data.longTermSoreness = 0;
        data.sleepStatus = 10;
        data.grit = 1;
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
