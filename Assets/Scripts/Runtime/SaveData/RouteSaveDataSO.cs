using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRouteSaveData", menuName = "ScriptableObjects/RouteSaveData")]
public class RouteSaveDataSO : ScriptableObject
{
    public bool isUnlockedAtStart = false;
    public RouteSaveData data = new();
    public void Initialize(string name)
    {
        data = new RouteSaveData();
        data.name = name;
        data.unlocked = isUnlockedAtStart;
        data.numTimesRun = 0;
    }

    public void LoadRouteDialogueSaveData(ref RouteDialogue[] routeDialogues)
    {
        if (routeDialogues == null) return;

        RouteDialogueSaveData[] routeDialogueSaveDatas = new RouteDialogueSaveData[routeDialogues.Length];
        for (int i = 0; i < routeDialogues.Length; i++)
        {
            RouteDialogue currentDialogue = routeDialogues[i];
            RouteDialogueSaveData routeDialogueSaveData = data.routeDialogueSaveDatas?.FirstOrDefault(d => d.dialgoueID == currentDialogue.dialogueID);
            if (routeDialogueSaveData == null)
            {
                routeDialogueSaveData = new RouteDialogueSaveData
                {
                    dialgoueID = currentDialogue.dialogueID,
                    hasBeenSeen = false
                };
            }

            routeDialogueSaveDatas[i] = routeDialogueSaveData;
        }

        data.routeDialogueSaveDatas = routeDialogueSaveDatas;
    }
}

[Serializable]
public class RouteSaveData
{
    public string name;
    public bool unlocked;
    public int numTimesRun;
    public RouteDialogueSaveData[] routeDialogueSaveDatas;
}

[Serializable]
public class RouteDialogueSaveData
{
    public string dialgoueID;
    public bool hasBeenSeen = false;
}
