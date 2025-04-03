using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSimulationSaveData", menuName = "ScriptableObjects/SimulationSaveData")]
[Serializable]
public class SimulationSaveDataSO : ScriptableObject
{
    public SimulationSaveData data;
}

[Serializable]
public class SimulationSaveData
{
    // The index of the day the player is on
    public int dayIndex;
    // The index of the event the player is on
    public int eventIndex;
}
