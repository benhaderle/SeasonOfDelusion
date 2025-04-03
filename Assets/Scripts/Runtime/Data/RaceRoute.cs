using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RaceRoute
{
    /// <summary>
    /// internally used ID
    /// </summary>
    [SerializeField] private string id;
    public string ID => id;

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

    /// <summary>
    /// The list of mile markers where a Race Opportunity is present
    /// </summary>
    [SerializeField] private List<float> opportunityMarkers;
    public List<float> OpportunityMarkers => opportunityMarkers;
}
