using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkoutGroupSlot : MonoBehaviour
{
    private static readonly string EMPTY_INTENSITY_STRING = "-";    
    [SerializeField] private RectTransform cardParent;
    [SerializeField] private Image selectionOutline;
    [SerializeField] private TextMeshProUGUI intensityText;
    private int groupIndex;
    private int slotIndex;
    private WorkoutRunnerCard runnerCard;

    public void Initialize(int groupIndex, int slotIndex)
    {
        this.groupIndex = groupIndex;
        this.slotIndex = slotIndex;
        intensityText.text = EMPTY_INTENSITY_STRING;
    }

    public void SetRunnerCardToSlot(WorkoutRunnerCard card)
    {
        runnerCard = card;
        card.transform.SetParent(cardParent);
        card.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);
        card.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
    }

    public Runner RemoveCardFromSlot()
    {
        Runner runner = runnerCard?.Runner;
        runnerCard = null;
        selectionOutline.enabled = false;
        intensityText.text = EMPTY_INTENSITY_STRING;

        return runner;
    }

    public void UpdateIntensityText(float averageGroupVO2)
    {
        if (runnerCard == null)
            return;

        float thresholdVO2 = runnerCard.Runner.currentVO2Max;

        float percentOfGroup = averageGroupVO2 / thresholdVO2;

        if(percentOfGroup <= .9f)
        {
            intensityText.text = "Comfortable";
        }
        else if(percentOfGroup <= .95f)
        {
            intensityText.text = "Confident";
        }
        else if(percentOfGroup <= 1f)
        {
            intensityText.text = "Determined";
        }
        else if(percentOfGroup <= 1.05f)
        {
            intensityText.text = "Nervous";
        }
        else if(percentOfGroup <= 1.1f)
        {
            intensityText.text = "Fearful";
        }
        else
        {
            intensityText.text = "Upset";
        }

        Debug.Log($"Group VO2: {averageGroupVO2}, ThresholdV02: {thresholdVO2}, Intensity: {intensityText.text}");
    }

    public void OnSlotClicked()
    {
        if(runnerCard != null)
        {
            selectionOutline.enabled = !selectionOutline.enabled;

            WorkoutSelectionUIController.runnerCardSelectedEvent.Invoke(new WorkoutSelectionUIController.RunnerCardSelectedEvent.Context 
            {
                card = runnerCard,
                groupIndex = groupIndex,
                slotIndex = slotIndex
            });
        }
        else
        {
            WorkoutSelectionUIController.emptySlotSelectedEvent.Invoke(new WorkoutSelectionUIController.EmptySlotSelectedEvent.Context 
            {
                groupIndex = groupIndex,
                slotIndex = slotIndex
            });
        }
    }

    public Runner GetRunner()
    {
        return runnerCard?.Runner;
    }
}
