using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The state of a runner during a run
/// </summary>
public class RunnerState
{
    /// <summary>
    /// a list of incremental distance and time markings
    /// </summary>
    public List<(float, float)> distanceTimeSimulationIntervalList;
    /// <summary>
    /// The VO2 the runner is currently using
    /// </summary>
    public float runVO2;
    /// <summary>
    /// The VO2 of the last simulation interval
    /// </summary>
    public float lastSimulationIntervalVO2;
    /// <summary>
    /// The current speed
    /// </summary>
    public float currentSpeed;
    /// <summary>
    /// The desired speed
    /// </summary>
    public float desiredSpeed;
    /// <summary>
    /// How far along the runner is in a given interval in miles
    /// ONLY USED DURING WORKOUTs
    /// </summary>
    public float workoutIntervalDistance;
    /// <summary>
    /// How far along the runner is in the run overall in miles
    /// </summary>
    public float totalDistance;
    /// <summary>
    /// Percent done with the run overall
    /// </summary>
    public float percentDone;
    /// <summary>
    /// Time spent in this run in seconds
    /// </summary>
    public float timeInSeconds;
    /// <summary>
    /// The short term soreness accrued during this run
    /// </summary>
    public float shortTermSoreness;
    /// <summary>
    /// The hydration cost accrued during this run
    /// </summary>
    public float hydrationCost;
    /// <summary>
    /// The calorie cost accrued during this run
    /// </summary>
    public float calorieCost;
}
