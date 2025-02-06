using System;
using UnityEngine;

/// <summary>
/// Represents a workout that runners can run
/// </summary>
[Serializable]
public class Workout
{   
    public enum Type { DistanceRepeats = 0, TimeRepeats = 1};

    /// <summary>
    /// The name of this workout. Can be used for player display.
    /// </summary>
    [SerializeField] private string name;
    public string Name => name;

    [SerializeField] private Type workoutType;
    public Type WorkoutType => workoutType;
    [SerializeField] private int numIntervals;
    public int NumIntervals => numIntervals;
    /// <summary>
    /// The distance of each interval in this workout, either in minutes or miles depending on the type
    /// </summary>
    [SerializeField] private float intervalLength;
    public float IntervalLength => intervalLength;
    /// <summary>
    /// The amount of rest between intervals in minutes
    /// </summary>
    [SerializeField] private float restLength;
    public float RestLength => restLength;

}
