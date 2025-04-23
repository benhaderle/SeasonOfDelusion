using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Shapes;

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

    public void OnValidate()
    {


#if UNITY_EDITOR
        if (saveData == null)
        {
            saveData = ScriptableObject.CreateInstance<RouteSaveDataSO>();
            AssetDatabase.CreateAsset(saveData, $"Assets/Data/SaveData/Routes/{displayName.Replace(" ", "")}SaveData.asset");
        }
        else
        {
            if (saveData.name != $"{displayName.Replace(" ", "")}SaveData")
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(saveData), $"{displayName.Replace(" ", "")}SaveData");
                AssetDatabase.SaveAssets();
            }
        }
#endif

        if (string.IsNullOrWhiteSpace(saveData.data.name))
        {
            saveData.Initialize(displayName);
        }
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
}
