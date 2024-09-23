using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// One of the stats on a Runner card
/// </summary>
public class RunnerSimulationCardStat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI changeText;

    public void Setup(string value, string change)
    {
        valueText.text = value;
        changeText.text = change;
    }
}
