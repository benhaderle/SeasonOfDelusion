using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SO for holding shared formula variables between all Runners
/// </summary>
[CreateAssetMenu(fileName = "newRunnerVariables", menuName = "ScriptableObjects/RunnerCalculationVariables")]
public class RunnerCalculationVariables : ScriptableObject
{
    public int[] levelExperienceThresholds;
    public AnimationCurve vo2ImprovementCurve;
    public AnimationCurve strengthImprovementCurve;

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
