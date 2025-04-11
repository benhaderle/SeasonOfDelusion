using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using System;
using System.Linq;

public class SaveData : SaveDataSingleton<SaveData, SerializedSaveData>
{
    [SerializeField] private bool shouldSaveLoadOnPause;
    private string userID;
    [SerializeField] private SimulationSaveDataSO simulationSaveData;
    [SerializeField] private MapSaveDataSO mapSaveData;
    [SerializeField] private RouteSaveDataSO[] routeSaveDatas;
    [SerializeField] private RunnerSaveDataSO[] playerRunnerSaveDatas;
    private void Start()
    {
        LoadGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (shouldSaveLoadOnPause)
        {
            if (pause)
            {
                SaveGame();
            }
            else
            {
                LoadGame();
            }
        }
    }

    // This will only get called in editor or on devices we're not building for, like PCs.
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    /// <summary>
    /// This function is used to reset all the values in the save data to their default values 
    /// (without clearing the loaded flag like SetDefaultValues always does)
    /// </summary>
    public void ResetValues()
    {
        simulationSaveData.data = new();
        for (int i = 0; i < playerRunnerSaveDatas.Length; i++)
        {
            playerRunnerSaveDatas[i].data = new();
        }
        for (int i = 0; i < routeSaveDatas.Length; i++)
        {
            routeSaveDatas[i].data = new();
        }
        mapSaveData.data = new();
    }

    /// <summary>
    /// WARNING: this is meant to be used just for clearing data, 
    /// Call ResetValues if you are not clearing data (i.e., you want loaded=true to stay the case)
    /// </summary>
    protected override void SetDefaultValues()
    {
        base.SetDefaultValues();
        
        // Reset all of the fields to defaults.
        ResetValues();
    }

    protected override void Deserialize(SerializedSaveData serializedSaveData)
    {
        SafeLoadDatum(ref simulationSaveData.data, serializedSaveData.simulationSaveData);
        for (int i = 0; i < playerRunnerSaveDatas.Length; i++)
        {
            SafeLoadDatum(ref playerRunnerSaveDatas[i].data, serializedSaveData.playerRunnerSaveDatas.ToList().Find(d => d.firstName == playerRunnerSaveDatas[i].data.firstName));
        }
        for (int i = 0; i < routeSaveDatas.Length; i++)
        {
            SafeLoadDatum(ref routeSaveDatas[i].data, serializedSaveData.routeSaveDatas.ToList().Find(d => d.name == routeSaveDatas[i].data.name));
        }
        SafeLoadDatum(ref mapSaveData.data, serializedSaveData.mapSaveData);
    }

    protected override SerializedSaveData Serialize()
    {
        SerializedSaveData saveDataObject = new SerializedSaveData
        {
            userID = userID,
            timestamp = DateTime.UtcNow.ToString(),
            simulationSaveData = simulationSaveData.data,
            playerRunnerSaveDatas = playerRunnerSaveDatas.Select(so => so.data).ToArray(),
            routeSaveDatas = routeSaveDatas.Select(so => so.data).ToArray(),
            mapSaveData = mapSaveData.data
        };

        return saveDataObject;
    }
}

