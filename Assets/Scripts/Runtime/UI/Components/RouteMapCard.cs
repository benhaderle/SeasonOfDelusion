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
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void Setup(Route route)
    {
        routeName = route.DisplayName;
        nameText.text = route.DisplayName;
        lengthText.text = $"{route.Length:F1} miles";
        difficultyText.text = GetDifficultyString(route.Difficulty);
        descriptionText.text = $"\"{route.Description}\"";
    }

    private string GetDifficultyString(float difficulty)
    {
        if (difficulty <= 0.7f)         return "Gentle";
        else if (difficulty <= 0.75f)   return "Chill";
        else if (difficulty <= 0.85f)   return "Moderate";
        else if (difficulty <= 0.9f)    return "Challenging";
        else if (difficulty <= 0.95f)   return "Aggressive";
        else                            return "Difficult";
    }
}
