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

        //set intensity text to something
    }

    public void OnCardRemovedFromSlot()
    {
        selectionOutline.enabled = false;

        if (cardParent.GetComponentInChildren<WorkoutRunnerCard>() == null)
        {
            runnerCard = null;
            intensityText.text = EMPTY_INTENSITY_STRING;
        }
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
}
