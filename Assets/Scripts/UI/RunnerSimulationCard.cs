using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

/// <summary>
/// One of the UI cards representing one Runner on the Roster screen
/// </summary> 
public class RunnerSimulationCard : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI paceText;
    [SerializeField] private RunnerSimulationCardStat aeroStat;
    [SerializeField] private RectTransform statusContainer;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Color improvementColor;
    [SerializeField] private Color regressionColor;

    public void Setup(Runner runner, Color backgroundColor)
    {
        nameText.text = runner.Name;
        backgroundImage.color = backgroundColor;
    }

    public void UpdatePace(RunnerState runnerState)
    {
        paceText.text = RunUtility.SpeedToMilePaceString(runnerState.currentSpeed);
    }

    public void UpdateListPosition(int orderInList, Color backgroundColor)
    {
        transform.SetSiblingIndex(orderInList);
        backgroundImage.color = backgroundColor;
    }

    public void ShowPostRunUpdate(Runner runner, RunnerUpdateRecord record)
    {
        paceText.gameObject.SetActive(false);

        aeroStat.gameObject.SetActive(true);
        string colorString = regressionColor.ToHexString();
        string prefix = "";
        if(record.vo2Change >= 0)
        {
            colorString = improvementColor.ToHexString();
            prefix = "+";
        } 
        aeroStat.Setup(Mathf.FloorToInt(runner.CurrentVO2Max * 10).ToString(), $"<color=#{colorString}>{prefix}{Mathf.FloorToInt(record.vo2Change * 10)}</color>");
        
        statusContainer.gameObject.SetActive(true);
        statusText.text = RunUtility.ExhaustionToStatusString(runner.Exhaustion);
    }
}
