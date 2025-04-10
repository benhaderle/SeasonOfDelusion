using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

/// <summary>
/// One of the route buttons on the Routes screen
/// </summary>
public class RouteMapCard : MonoBehaviour
{
    private string routeName;
    public string RouteName => routeName;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lengthText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI elevationText;
    [SerializeField] private TextMeshProUGUI surfaceText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Setup(Route route)
    {
        routeName = route.Name;
        nameText.text = route.Name;
        lengthText.text = $"{route.Length:F1} miles";
        difficultyText.text = $"Gentle";
        elevationText.text = $"200 feet";
        surfaceText.text = $"Paved Path";
        descriptionText.text = route.Description;
    }
}
