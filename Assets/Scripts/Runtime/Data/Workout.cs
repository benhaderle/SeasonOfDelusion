using System;
using System.Collections.Generic;
using System.Linq;
using Shapes;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Represents a workout that runners can run
/// </summary>
[CreateAssetMenu(fileName = "NewWorkout", menuName = "ScriptableObjects/Workout")]
public class Workout : ScriptableObject
{
    //TODO: currently only distance repeats are supported
    public enum Type { DistanceRepeats = 0, TimeRepeats = 1 };

    public WorkoutSaveDataSO saveData;
    /// <summary>
    /// The name of this workout. Can be used for player display.
    /// </summary>
    [SerializeField] private string displayName;
    public string DisplayName => displayName;
    [SerializeField] private Type workoutType;
    public Type WorkoutType => workoutType;
    [SerializeField] private RouteLineData routeLineData;
    public RouteLineData RouteLineData => routeLineData;
    [SerializeField] private float goalVO2 = .9f;
    public float GoalVO2 => goalVO2;
    public List<Interval> intervals = new();
    public WorkoutEffect[] effects;

    private float totalLength = -1;

    public void LoadSaveData()
    {
        if (!saveData.data.initialized)
        {
            saveData.Initialize(displayName);
        }
    }

    public float GetTotalLength()
    {
        if (totalLength < 0)
        {
            totalLength = intervals.Sum(i => i.repeats * i.length);
        }

        return totalLength;
    }

    public string GetDifficultyString()
    {
        GetTotalLength();

        float difficulty = totalLength * goalVO2;

        if (difficulty <= 2)        return "Gentle";
        else if (difficulty <= 4)   return "Chill";
        else if (difficulty <= 6)   return "Moderate";
        else if (difficulty <= 8)   return "Challenging";
        else if (difficulty <= 10)  return "Aggressive";
        else                        return "Difficult";
    }
}

[Serializable]
public struct Interval
{
    public int repeats;
    public float length;
    public float rest;
}

[Serializable]
public struct WorkoutEffect
{
    public enum Type { VO2 = 0, Strength = 1, Form = 2, Grit = 3 };
    public Type type;
    public float amount;
}