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

    /// <summary>
    /// The name of this workout. Can be used for player display.
    /// </summary>
    [SerializeField] private string displayName;
    public string DisplayName => displayName;
    [SerializeField] private Type workoutType;
    public Type WorkoutType => workoutType;
    [SerializeField] private RouteLineData routeLineData;
    public RouteLineData RouteLineData => routeLineData;
    public List<Interval> intervals = new();
    [SerializeField] private WorkoutEffect[] effects;

    private float totalLength = -1;
    public float GetTotalLength()
    {
        if (totalLength < 0)
        {
            totalLength = intervals.Sum(i => i.repeats * i.length);
        }

        return totalLength;
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