using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWorkoutSaveData", menuName = "ScriptableObjects/WorkoutSaveData")]
public class WorkoutSaveDataSO : ScriptableObject
{
    public bool isUnlockedAtStart = false;
    public WorkoutSaveData data = new();
    public void Initialize(string name)
    {
        data.initialized = true;
        data.name = name;
        data.unlocked = isUnlockedAtStart;
        data.numTimesRun = 0;
    }
}

[Serializable]
public class WorkoutSaveData
{
    public bool initialized;
    public string name;
    public bool unlocked;
    public int numTimesRun;
}
