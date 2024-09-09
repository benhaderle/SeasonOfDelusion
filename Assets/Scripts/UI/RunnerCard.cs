using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// One of the UI cards representing one Runner on the Roster screen
/// </summary> 
public class RunnerCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    [SerializeField] private RunnerCardStat vo2MaxStat;

    public void Setup(Runner runner)
    {
        nameText.text = runner.Name;
        vo2MaxStat.SetValueText(Mathf.FloorToInt(runner.CurrentVO2Max * 10).ToString());
    }
}
