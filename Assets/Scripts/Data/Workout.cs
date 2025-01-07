using System;
using UnityEngine;

/// <summary>
/// Represents a workout that runners can run
/// </summary>
[Serializable]
public class Workout
{   
    /// <summary>
    /// The name of this workout. Can be used for player display.
    /// </summary>
    [SerializeField] private string name;
    public string Name => name;
    

}
