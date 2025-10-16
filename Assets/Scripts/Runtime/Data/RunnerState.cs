using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The state of a runner during a run
/// </summary>
public class RunnerState
{
    /// <summary>
    /// a list of incremental distance and time markings
    /// </summary>
    public List<SimulationIntervalData> simulationIntervalList;
    /// <summary>
    /// The VO2 the runner currently desires to run at, not taking run economy into account
    /// </summary>
    public float desiredVO2;
    /// <summary>
    /// The current speed in miles per sec
    /// </summary>
    public float currentSpeed;
    /// <summary>
    /// The desired speed in miles per sec
    /// </summary>
    public float desiredSpeed;
    /// <summary>
    /// How far along the runner is in a given interval in miles
    /// For normal runs and races, this is the same as totalDistance, but for workouts with intervals it is the distance through the current interval
    /// </summary>
    public float intervalDistance;
    /// <summary>
    /// How far along the runner is in a given interval in miles
    /// For normal runs and races, this is the same as totalDistance, but for workouts with intervals it is the distance through the current interval
    /// </summary>
    public float intervalPercentDone;
    /// <summary>
    /// How far along the runner is in the run overall in miles
    /// </summary>
    public float totalDistance;
    /// <summary>
    /// Percent done with the run overall
    /// </summary>
    public float totalPercentDone;
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

    public RunnerState()
    {
        // initialize this list with a marker for the beginning of the simulation
        simulationIntervalList = new()
        {
            new SimulationIntervalData
            {
                timeInSeconds = 0,
                distanceInMiles = 0,
                vo2 = 0
            }
        };
        desiredVO2 = 0;
        desiredSpeed = 0;
        currentSpeed = 0;
        intervalDistance = 0;
        totalDistance = 0;
        totalPercentDone = 0;
        shortTermSoreness = 0;
        hydrationCost = 0;
        calorieCost = 0;
    }

    public float GetAverageVO2()
    {
        return simulationIntervalList.Average(d => d.vo2);
    }
}

public struct SimulationIntervalData
{
    public float timeInSeconds;
    public float distanceInMiles;
    public float vo2;
}
