using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class RunnerCardStat : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI valueText;

    /// <summary>
    /// Sets the text for the valueText of this stat
    /// </summary>
    /// <param name="text">The text to display</param>
    public void SetValueText(string text)
    {
        valueText.text = text;
    }
}