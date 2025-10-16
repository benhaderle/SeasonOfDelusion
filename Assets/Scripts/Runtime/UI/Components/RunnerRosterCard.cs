using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// One of the UI cards representing one Runner on the Roster screen
/// </summary> 
public class RunnerRosterCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private RunnerRosterCardStat vo2MaxStat;
    [SerializeField] private RunnerRosterCardStat strengthStat;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image backgroundImage;

    public void Setup(Runner runner, Color backgroundColor)
    {
        nameText.text = runner.Name;
        vo2MaxStat.SetValueText(Mathf.FloorToInt(runner.currentVO2Max * 10).ToString());
        strengthStat.SetValueText(Mathf.FloorToInt(runner.currentStrength * 10).ToString());
        statusText.text = RunUtility.SorenessToStatusString(runner.longTermSoreness);
        backgroundImage.color = backgroundColor;
    }
}
