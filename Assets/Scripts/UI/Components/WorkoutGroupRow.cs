using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkoutGroupRow : MonoBehaviour
{
    [SerializeField] private WorkoutGroupSlot[] slots;

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
    }

    public void OnCardRemovedFromSlot(int slotIndex)
    {
        slots[slotIndex].OnCardRemovedFromSlot();
    }
}
