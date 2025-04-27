using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Shapes;
using System;

[CreateAssetMenu(fileName = "Route", menuName = "ScriptableObjects/Route")]
public class Route : ScriptableObject
{
    [HideInInspector] public RouteSaveDataSO saveData;
    /// <summary>
    /// The name of this route. Can be used for player display.
    /// </summary>
    [SerializeField] private string displayName;
    public string DisplayName => displayName;
    public float Length => lineData.Length;
    public RouteLineData lineData;
    [SerializeField] private int nodeIDForUnlock = -1;
    [SerializeField] private string description;
    public string Description => description;
    [SerializeField] private float difficulty;
    public float Difficulty => difficulty;
    public bool IsNewRoute => saveData.data != null && saveData.data.numTimesRun == 0 && saveData.data.unlocked;
    [SerializeField] private RouteDialogue[] routeDialogues;

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (saveData == null)
        {
            saveData = ScriptableObject.CreateInstance<RouteSaveDataSO>();
            AssetDatabase.CreateAsset(saveData, $"Assets/Data/SaveData/Routes/{name}SaveData.asset");
        }
        else
        {
            if (saveData.name != $"{name}SaveData")
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(saveData), $"{name}SaveData");
                AssetDatabase.SaveAssets();
            }
        }

        if (string.IsNullOrWhiteSpace(saveData.data.name))
        {
            saveData.Initialize(displayName);
        }
    }
#endif

    public void LoadSaveData()
    {
        if (string.IsNullOrWhiteSpace(saveData.data.name))
        {
            saveData.Initialize(displayName);
        }

        saveData.LoadRouteDialogueSaveData(ref routeDialogues);
    }

    /// <summary>
    /// If the route is locked, then we check to see if its unlock condition is met and if so we unlock it.
    /// </summary>
    /// <returns>True if the function unlocks the route for the first time, false otherwise </returns>
    public bool CheckUnlock(int nodeID)
    {
        bool gotUnlocked = false;
        if (!saveData.data.unlocked && nodeID == nodeIDForUnlock)
        {
            saveData.data.unlocked = true;
            gotUnlocked = true;
        }

        return gotUnlocked;
    }

    public string GetNextDialogueID()
    {
        if (routeDialogues == null)
        {
            return null;
        }

        for(int i = 0; i < routeDialogues.Length; i++)
        {
            RouteDialogue currentDialogue = routeDialogues[i];
            RouteDialogueSaveData dialogueSaveData = saveData.data.routeDialogueSaveDatas[i];
            if (!dialogueSaveData.hasBeenSeen && currentDialogue.numTimesRunRequired <= saveData.data.numTimesRun)
            {
                return currentDialogue.dialogueID;
            }
        }

        return null;
    }
}

[Serializable]
public class RouteDialogue
{
    public string dialogueID;
    public int numTimesRunRequired = 1;
}