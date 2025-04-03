using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// One of the UI cards representing a Team on the Race Results screen
/// </summary> 
public class TeamRaceResultsCard : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI placeText;
    [SerializeField] private TextMeshProUGUI[] runnerPlaceTexts;
    [SerializeField] private TextMeshProUGUI teamPointsText;

    public void Setup(TeamRaceResultRecord teamRaceResultRecord, Color backgroundColor, int place)
    {
        nameText.text = teamRaceResultRecord.teamName;
        backgroundImage.color = backgroundColor;
        transform.SetSiblingIndex(place);
        placeText.text = (place + 1).ToString();
        teamPointsText.text = teamRaceResultRecord.teamScore.ToString();

        for (int i = 0; i < runnerPlaceTexts.Length; i++)
        {
            if (i >= teamRaceResultRecord.runnerScores.Length)
            {
                runnerPlaceTexts[i].text = "-";
            }
            else
            {
                runnerPlaceTexts[i].text = teamRaceResultRecord.runnerScores[i].ToString();
            }
        }
    }
}
