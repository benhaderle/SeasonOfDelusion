using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkoutGroupRow : MonoBehaviour
{
    [SerializeField] private WorkoutGroupSlot[] slots;

    private float vo2MaxSum;
    private float numSlotsFilled;

    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityText;
    private float groupIntensity = .9f;

    public void Initialize(int groupIndex)
    {
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].Initialize(groupIndex, i);
        }
        intensitySlider.value = 2;
    }

    public void AddRunnerToSlot(WorkoutRunnerCard card, int slotIndex)
    {
        slots[slotIndex].SetRunnerCardToSlot(card);

        vo2MaxSum += card.Runner.CurrentVO2Max * card.Runner.CalculateRunEconomy();
        numSlotsFilled++;
        UpdateRunnerIntensities();
    }

    public void RemoveCardFromSlot(int slotIndex)
    {
        Runner runnerRemoved = slots[slotIndex].RemoveCardFromSlot();
        if(runnerRemoved != null)
        {
            vo2MaxSum -= runnerRemoved.CurrentVO2Max * runnerRemoved.CalculateRunEconomy();
            numSlotsFilled--;
            UpdateRunnerIntensities();
        }
    }

    public void OnEaseSliderValueChanged(float value)
    {
        groupIntensity = value / 4f;
        if(groupIntensity < .2f)
        {
            intensityText.text = "Very Easy";
        }
        else if(groupIntensity < .4f)
        {
            intensityText.text = "Easy";
        }
        else if(groupIntensity < .6f)
        {
            intensityText.text = "Medium";
        }
        else if(groupIntensity < .8f)
        {
            intensityText.text = "Hard";
        }
        else
        {
            intensityText.text = "Very Hard";
        }

        groupIntensity = .85f + (groupIntensity - .35f) * .4f;

        UpdateRunnerIntensities();
    }

    private void UpdateRunnerIntensities()
    {
        float workoutIntensity = vo2MaxSum / numSlotsFilled * groupIntensity;
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].UpdateIntensityText(workoutIntensity);
        }
    }
}
