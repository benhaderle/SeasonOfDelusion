using System;
using UnityEngine;

public enum SurfaceType { LargeRoad, SmallRoad, LargeTrail, SmallTrail }

[Serializable]
public class RunRoute
{
    public float length;
    public AnimationCurve profile;
    public float minElevation;
    public float maxElevation;
    public float beauty;
    public float exposure;
    public float surfaceQuality;
    public SurfaceType surfaceType;

    private const float FEET_PER_MILE = 5280f;


    public float GetGradient(float distance)
    {
        float sampleSizeInFeet = 100;
        float sampleSizeInMiles = sampleSizeInFeet / FEET_PER_MILE;
        return (GetElevationAtDistance(distance + sampleSizeInMiles) - GetElevationAtDistance(distance)) / sampleSizeInFeet;
    }

    private float GetElevationAtDistance(float distance)
    {
        return Mathf.Lerp(minElevation, maxElevation, profile.Evaluate(distance / length));
    }
}
