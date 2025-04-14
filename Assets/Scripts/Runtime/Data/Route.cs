using System;
using Shapes;
using UnityEditor;
using UnityEngine;

public enum SurfaceType { LargeRoad, SmallRoad, LargeTrail, SmallTrail }

/// <summary>
/// Represents a route that runners can run
/// </summary>
[Serializable]
public class Route
{
    public RouteSaveDataSO saveData;
    /// <summary>
    /// The name of this route. Can be used for player display.
    /// </summary>
    [SerializeField] private string name;
    public string Name => name;

    public float Length => lineData.Length;

    [SerializeField] public RouteLineData lineData;
    [SerializeField] private int nodeIDForUnlock;
    [SerializeField] private string description;
    public string Description => description;

    [SerializeField] private float difficulty;
    public float Difficulty => difficulty;

    public bool IsNewRoute => saveData.data != null && saveData.data.numTimesRun == 0 && saveData.data.unlocked;

    public void Validate()
    {
#if UNITY_EDITOR
        if (saveData == null)
        {
            saveData = ScriptableObject.CreateInstance<RouteSaveDataSO>();
            AssetDatabase.CreateAsset(saveData, $"Assets/Data/SaveData/Routes/{name.Replace(" ", "")}SaveData.asset");
        }
#endif

        if (string.IsNullOrWhiteSpace(saveData.data.name))
        {
            saveData.Initialize(name);
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

    // [SerializeField] private AnimationCurve profile;
    // [SerializeField] private float minElevation;
    // [SerializeField] private float maxElevation;
    // [SerializeField] private float beauty;
    // [SerializeField] private float exposure;
    // [SerializeField] private float surfaceQuality;
    // [SerializeField] private SurfaceType surfaceType;

    // private const float FEET_PER_MILE = 5280f;

    // public float GetGradient(float distance)
    // {
    //     float sampleSizeInFeet = 100;
    //     float sampleSizeInMiles = sampleSizeInFeet / FEET_PER_MILE;
    //     return (GetElevationAtDistance(distance + sampleSizeInMiles) - GetElevationAtDistance(distance)) / sampleSizeInFeet;
    // }

    // private float GetElevationAtDistance(float distance)
    // {
    //     return Mathf.Lerp(minElevation, maxElevation, profile.Evaluate(distance / length));
    // }
}
