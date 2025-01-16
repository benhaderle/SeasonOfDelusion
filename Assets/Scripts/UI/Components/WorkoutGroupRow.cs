using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkoutGroupRow : MonoBehaviour
{
    [SerializeField] private WorkoutGroupSlot[] slots;

    private float vo2MaxSum;
    private float numSlotsFilled;

    private float groupIntensity = .9f;

    public void Initialize(int groupIndex)
    {
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].Initialize(groupIndex, i);
        }
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

    private void UpdateRunnerIntensities()
    {
        float workoutIntensity = vo2MaxSum / numSlotsFilled * groupIntensity;
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].UpdateIntensityText(workoutIntensity);
        }
    }
}
