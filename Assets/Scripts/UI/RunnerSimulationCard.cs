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
    [SerializeField] private TextMeshProUGUI aeroText;
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
        aeroText.gameObject.SetActive(true);

        string colorString = record.vo2Change >= 0 ? improvementColor.ToHexString() : regressionColor.ToHexString();
        aeroText.text = $"AERO: {Mathf.FloorToInt(runner.CurrentVO2Max * 10)} <color=#{colorString}>{Mathf.FloorToInt(record.vo2Change * 10)}</color>";
    }
}
