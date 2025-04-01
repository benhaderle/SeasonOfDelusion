using System;
using Shapes;
using UnityEngine;

public enum SurfaceType { LargeRoad, SmallRoad, LargeTrail, SmallTrail }

/// <summary>
/// Represents a route that runners can run
/// </summary>
[Serializable]
public class Route
{
    /// <summary>
    /// The name of this route. Can be used for player display.
    /// </summary>
    [SerializeField] private string name;
    public string Name => name;

    /// <summary>
    /// Length of route in miles.
    /// </summary>
    [SerializeField] private float length;
    public float Length => length;

    [SerializeField] public RouteLineData lineData;

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
