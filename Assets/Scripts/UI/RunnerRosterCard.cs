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
    [SerializeField] private Image backgroundImage;

    public void Setup(Runner runner, Color backgroundColor)
    {
        nameText.text = runner.Name;
        vo2MaxStat.SetValueText(Mathf.FloorToInt(runner.CurrentVO2Max * 10).ToString());
        backgroundImage.color = backgroundColor;
    }
}
