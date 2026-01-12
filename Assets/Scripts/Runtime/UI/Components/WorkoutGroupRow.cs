using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class WorkoutGroupRow : MonoBehaviour
{
    [SerializeField] private WorkoutGroupSlot[] slots;

    private float vdotMaxSum;
    private int numSlotsFilled;

    [SerializeField] private Slider intensitySlider;
    [SerializeField] private TextMeshProUGUI intensityText;
    private float groupIntensity = .9f;

    public void Initialize(int groupIndex, float goalVO2)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Initialize(groupIndex, i);
        }
        intensitySlider.value = 2;
    }

    public void AddRunnerToSlot(WorkoutRunnerCard card, int slotIndex)
    {
        slots[slotIndex].SetRunnerCardToSlot(card);

        vdotMaxSum += card.Runner.GetCurrentVDOTMax();
        numSlotsFilled++;
        UpdateRunnerIntensities();
    }

    public void RemoveCardFromSlot(int slotIndex)
    {
        Runner runnerRemoved = slots[slotIndex].RemoveCardFromSlot();
        if(runnerRemoved != null)
        {
            vdotMaxSum -= runnerRemoved.GetCurrentVDOTMax();
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

        groupIntensity = Mathf.Lerp(.8f, 1.2f, groupIntensity);

        UpdateRunnerIntensities();
    }

    private void UpdateRunnerIntensities()
    {
        float workoutIntensity = vdotMaxSum / numSlotsFilled; // * groupIntensity
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].UpdateIntensityText(workoutIntensity);
        }
    }

    public WorkoutGroup GetWorkoutGroup()
    {
        WorkoutGroup group = new WorkoutGroup
        {
            runners = new Runner[numSlotsFilled],
            intensity = groupIntensity,
            targetVDOT = vdotMaxSum / numSlotsFilled //* groupIntensity
        };

        int slotIndex = 0;
        int runnerIndex = 0;
        while(slotIndex < slots.Length && runnerIndex < group.runners.Length)
        {
            Runner runner = slots[slotIndex].GetRunner();
            if (runner != null)
            {
                group.runners[runnerIndex] = runner;
                runnerIndex++;
            }
            slotIndex++;
        }

        return group;
    }
}

public class WorkoutGroup
{
    public Runner[] runners;

    public float intensity;
    public float targetVDOT;
}
