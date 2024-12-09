using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SO for holding shared formula variables between all Runners
/// </summary>
[CreateAssetMenu(fileName = "newRunnerVariables", menuName = "ScriptableObjects/RunnerCalculationVariables")]
public class RunnerCalculationVariables : ScriptableObject
{
    [Header("Post-Run VO2 Update Variables")]
    /// <summary>
    /// The percent of VO2 that a runner must run to improve their VO2
    /// </summary>
    [SerializeField] private float vo2ImprovementThreshold = .9f;
    public float VO2ImprovementThreshold => vo2ImprovementThreshold;
    [SerializeField] private float cubicVO2Slope = 2f;
    public float CubicVO2Slope => cubicVO2Slope;
    [SerializeField] private float linearVO2Slope = 1f;
    public float LinearVO2Slope => linearVO2Slope;
    [SerializeField] private float linearVO2Offset = .5f;
    public float LinearVO2Offset => linearVO2Offset;
    [SerializeField] private float constantVO2Offset = -10f;
    public float ConstantVO2Offset => constantVO2Offset;
    /// <summary>
    /// Final linear amount applied to VO2 update. Higher means more sensitive VO2 adjustment.
    /// </summary>
    [SerializeField] private float vo2UpdateFactor = .001f;
    public float VO2UpdateFactor => vo2UpdateFactor;

    [Header("Long Term Soreness Update Variables")]
    /// <summary>
    /// The percent of VO2 at which point a runner will begin to accumulate exhuastion
    /// </summary>
    [SerializeField] private float longTermSorenessVO2Threshold = .6f;
    public float LongTermSorenessVO2Threshold => longTermSorenessVO2Threshold;
    [SerializeField] private float cubicLongTermSorenessSlope = 3.5f;
    public float CubicLongTermSorenessSlope => cubicLongTermSorenessSlope;
    [SerializeField] private float linearLongTermSorenessSlope = 3f;
    public float LinearLongTermSorenessSlope => linearLongTermSorenessSlope;
    [SerializeField] private float linearLongTermSorenessOffset = .15f;
    public float LinearLongTermSorenessOffset => linearLongTermSorenessOffset;
    [SerializeField] private float linearLongTermSorenessTimeSlope = 3f;
    public float LinearLongTermSorenessTimeSlope => linearLongTermSorenessTimeSlope;
    [SerializeField] private float linearLongTermSorenessTimeOffset = -40;
    public float LinearLongTermSorenessTimeOffset => linearLongTermSorenessTimeOffset;

    [Header("Day End Variables")]
    /// <summary>
    /// How much exhaustion is subtracted at the end of each day
    /// </summary>
    [SerializeField] private float dayEndLongTermSorenessRecovery = 100;
    public float DayEndLongTermSorenessRecovery => dayEndLongTermSorenessRecovery;
}
