using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
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

    public void Setup(int value, int change, Color improvementColor, Color regressionColor)
    {
        string colorString = regressionColor.ToHexString();
        string prefix = "";
        if(change >= 0)
        {
            colorString = improvementColor.ToHexString();
            prefix = "+";
        } 
        Setup(value.ToString(), $"<color=#{colorString}>{prefix}{change}</color>");
    }
}
