using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRouteSaveData", menuName = "ScriptableObjects/RouteSaveData")]
public class RouteSaveDataSO : ScriptableObject
{
    public RouteSaveData data = new();
    public void Initialize(string name)
    {
        data = new RouteSaveData();
        data.name = name;
        data.unlocked = false;
        data.numTimesRun = 0;
    }
}

[Serializable]
public class RouteSaveData
{
    public string name;
    public bool unlocked;
    public int numTimesRun;
}
