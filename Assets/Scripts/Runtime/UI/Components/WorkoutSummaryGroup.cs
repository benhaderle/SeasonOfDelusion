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
        //TODO: this needs to take grade into account
        targetRow.timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.VDOTToSpeed(group.targetVDOT, 0));

        for (int i = 0; i < Mathf.Min(runnerUpdateRecords.Count, runnerRows.Length); i++)
        {
            runnerRows[i].gameObject.SetActive(true);
            runnerRows[i].labelText.text = runnerUpdateRecords[i].Key.FirstName;
            //TODO: this needs to take grade into account
            runnerRows[i].timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.VDOTToSpeed(runnerUpdateRecords[i].Value.runVDOT, 0));

            for (int j = 0; j < runnerUpdateRecords[i].Value.statUpRecords.Count; j++)
            {
                TextMeshProUGUI statText;
                switch (runnerUpdateRecords[i].Value.statUpRecords[j].statType)
                {
                    default:
                    case WorkoutEffect.Type.Aerobic: statText = runnerRows[i].aeroText; break;
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
