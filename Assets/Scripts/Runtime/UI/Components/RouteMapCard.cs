using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// One of the route buttons on the Routes screen
/// </summary>
public class RouteMapCard : MonoBehaviour
{
    private string routeName;
    public string RouteName => routeName;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lengthText;
    [SerializeField] private TextMeshProUGUI elevationText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Setup(Route route)
    {
        routeName = route.DisplayName;
        nameText.text = route.DisplayName;
        lengthText.text = $"{route.Length:F1} miles";
        elevationText.text = $"{(int)route.ElevationGain} ' climbing";
        difficultyText.text = GetDifficultyString(route.Length, route.ElevationGain);
        descriptionText.text = $"\"{route.Description}\"";
    }

    private string GetDifficultyString(float length, float elevation)
    {
        float difficulty = length + elevation / 1000f;

        if (difficulty <= 4.5f) return "Gentle";
        else if (difficulty <= 6.5f) return "Chill";
        else if (difficulty <= 8f) return "Moderate";
        else if (difficulty <= 10f) return "Challenging";
        else if (difficulty <= 12f) return "Aggressive";
        else return "Difficult";
    }
}
