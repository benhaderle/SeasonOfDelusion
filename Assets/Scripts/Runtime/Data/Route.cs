using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Shapes;
using System;

[CreateAssetMenu(fileName = "Route", menuName = "ScriptableObjects/Route")]
public class Route : ScriptableObject
{
    public RouteSaveDataSO saveData;
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
    // TODO: right now difficulty is just the desired vo2 target for the route, but this should change when I add elevation. all normal runs should be at the same vo2 target
    [SerializeField] private float difficulty;
    public float Difficulty => difficulty;
    public bool IsNewRoute => saveData.data != null && saveData.data.numTimesRun == 0 && saveData.data.unlocked;
    [SerializeField] private RouteDialogue[] routeDialogues;

    public void LoadSaveData()
    {
        if (!saveData.data.initialized)
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