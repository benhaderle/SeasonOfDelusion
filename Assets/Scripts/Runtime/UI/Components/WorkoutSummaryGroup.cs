using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class WorkoutSummaryGroup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private WorkoutSummaryRow targetRow;
    [SerializeField] private WorkoutSummaryRow[] runnerRows;

    public void Setup(WorkoutGroup group, List<KeyValuePair<Runner, RunnerUpdateRecord>> runnerUpdateRecords, SpriteLibraryAsset spriteLibraryAsset)
    {
        //TODO: this needs to take grade into account
        targetRow.timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.VDOTToSpeed(group.targetVDOT, 0));

        for (int i = 0; i < Mathf.Min(runnerUpdateRecords.Count, runnerRows.Length); i++)
        {
            runnerRows[i].gameObject.SetActive(true);

            runnerRows[i].runnerPortraitImage.sprite = runnerUpdateRecords[i].Key.GetCurrentConfidenceSprite();
            runnerRows[i].labelText.text = runnerUpdateRecords[i].Key.FirstName;
            //TODO: this needs to take grade into account
            runnerRows[i].timeText.text = RunUtility.SpeedToMilePaceString(RunUtility.VDOTToSpeed(runnerUpdateRecords[i].Value.runVDOT, 0));

            for (int j = 0; j < runnerUpdateRecords[i].Value.statUpRecords.Count; j++)
            {
                runnerRows[i].effectIndicators[j].gameObject.SetActive(true);

                StatUpRecord statUpRecord = runnerUpdateRecords[i].Value.statUpRecords[j];
                runnerRows[i].effectIndicators[j].Setup(spriteLibraryAsset.GetSprite("Stats", statUpRecord.statType.ToString()), Mathf.CeilToInt((statUpRecord.newValue - statUpRecord.oldValue) / statUpRecord.oldValue), "");
            }

            for (int j = runnerUpdateRecords[i].Value.statUpRecords.Count; j < runnerRows[i].effectIndicators.Length; j++)
            {
                runnerRows[i].effectIndicators[j].gameObject.SetActive(false);
            }
        }

        for (int i = runnerUpdateRecords.Count; i < runnerRows.Length; i++)
        {
            runnerRows[i].gameObject.SetActive(false);
        }
    }
}
