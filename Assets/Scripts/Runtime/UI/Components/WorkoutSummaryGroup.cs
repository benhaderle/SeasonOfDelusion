using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WorkoutSummaryGroup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private WorkoutSummaryRow targetRow;
    [SerializeField] private WorkoutSummaryRow[] runnerRows;

    public void Setup(WorkoutGroup group, List<KeyValuePair<Runner, RunnerUpdateRecord>> runnerUpdateRecords)
    {
        targetRow.timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.CaclulateSpeedFromOxygenCost(group.targetVO2));

        for (int i = 0; i < Mathf.Min(runnerUpdateRecords.Count, runnerRows.Length); i++)
        {
            runnerRows[i].gameObject.SetActive(true);
            runnerRows[i].labelText.text = runnerUpdateRecords[i].Key.FirstName;
            runnerRows[i].timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.CaclulateSpeedFromOxygenCost(runnerUpdateRecords[i].Value.runVO2));

            for (int j = 0; j < runnerUpdateRecords[i].Value.statUpRecords.Count; j++)
            {
                TextMeshProUGUI statText;
                switch (runnerUpdateRecords[i].Value.statUpRecords[j].statType)
                {
                    default:
                    case WorkoutEffect.Type.Aero: statText = runnerRows[i].aeroText; break;
                    case WorkoutEffect.Type.Strength: statText = runnerRows[i].strengthText; break;
                    case WorkoutEffect.Type.Form: statText = runnerRows[i].formText; break;
                    case WorkoutEffect.Type.Grit: statText = runnerRows[i].gritText; break;
                }

                string statName = runnerUpdateRecords[i].Value.statUpRecords[j].statType.ToString().Substring(0, 4).ToLower();
                string statIncrease = $"+{((runnerUpdateRecords[i].Value.statUpRecords[j].newValue - runnerUpdateRecords[i].Value.statUpRecords[j].oldValue) * 10).ToString("0")}";
                statText.text = $"{statName}\t{statIncrease}";
            }
        }

        for (int i = runnerUpdateRecords.Count; i < runnerRows.Length; i++)
        {
            runnerRows[i].gameObject.SetActive(false);
        }
    }
}
