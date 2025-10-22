using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTeamSaveData", menuName = "ScriptableObjects/TeamSaveData")]
public class TeamSaveDataSO : ScriptableObject
{
    public TeamSaveData data = new();

    public void Initialize(string name, List<string> roster)
    {
        data.initialized = true;

        data.name = name;
        data.roster = roster;
    }
}

[Serializable]
public class TeamSaveData
{
    public bool initialized;
    public string name;
    public List<string> roster;
}
