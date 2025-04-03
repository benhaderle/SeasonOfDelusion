using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the environmental conditions of a run
/// </summary>
[Serializable]
public class RunConditions
{
    // public float temperature;
    // public float humidity;
    // public float wind;
    // public float uvIndex;
    // public float precipitation;
    // public float airQuality;
    /// <summary>
    /// A number between 0 and 1 that represents what percentage of VO2Max that coach wants the runners to hit on the run
    /// </summary>
    public float coachVO2Guidance;
}
