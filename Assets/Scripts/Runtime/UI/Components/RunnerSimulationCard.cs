using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CreateNeptune;
using System.Linq;

/// <summary>
/// One of the UI cards representing one Runner on the Run screen
/// </summary> 
public class RunnerSimulationCard : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI paceText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject experienceContainer;
    [SerializeField] private Image experienceBarFill;
    [SerializeField] private TextMeshProUGUI experienceText;
    private IEnumerator postRunUpdateRoutine;

    public void Setup(Runner runner, Color backgroundColor)
    {
        nameText.text = runner.Name;
        backgroundImage.color = backgroundColor;
        levelText.text = $"LV {runner.level}";
    }

    public void UpdatePace(RunnerState runnerState)
    {
        paceText.text = RunUtility.SpeedToMilePaceString(runnerState.currentSpeed);
    }

    public void UpdateStatusText(Runner runner, RunnerState runnerState)
    {
        List<string> possibleStateStrings = new();

        if (runnerState.totalPercentDone < .05f)
        {
            possibleStateStrings.Add("Starting Off");
        }
        else if (runnerState.totalPercentDone > .95f)
        {
            possibleStateStrings.Add("Finishing Up");
        }

        float shortTermCalorieDeficit = runner.shortTermCalories - runnerState.calorieCost;
        if (shortTermCalorieDeficit < -500)
        {
            if (runner.longTermCalories + shortTermCalorieDeficit > 1000)
            {
                possibleStateStrings.Add("Burning Fat");
            }
            else
            {
                possibleStateStrings.Add("Shutting Down");
            }
        }
        else if (shortTermCalorieDeficit < 0)
        {
            possibleStateStrings.Add("Running on Empty");
        }
        else if (shortTermCalorieDeficit < 500)
        {
            possibleStateStrings.Add("Getting Hungry");
        }

        float currentDesiredSpeedDifference = runnerState.currentSpeed - runnerState.desiredSpeed;
        if (currentDesiredSpeedDifference > .15f)
        {
            possibleStateStrings.Add("Getting Dragged");
        }
        else if (currentDesiredSpeedDifference > -.15f)
        {
            possibleStateStrings.Add("Holding in the Group");
        }
        else
        {
            possibleStateStrings.Add("Pace Pushing");
        }

        float averageVO2 = runnerState.GetAverageVO2();
        float currentVO2 = runnerState.simulationIntervalList.Last().vo2;
        float currentAverageVO2Difference = currentVO2 - averageVO2;
        if (currentAverageVO2Difference > 1.5f)
        {
            possibleStateStrings.Add("Making a Move");
        }
        else if (currentAverageVO2Difference > -1.5f)
        {
            possibleStateStrings.Add("Holding Steady");
        }
        else
        {
            possibleStateStrings.Add("Holding Back");
        }

        statusText.text = possibleStateStrings[Random.Range(0, possibleStateStrings.Count)];

    }

    public void UpdateListPosition(int orderInList, Color backgroundColor)
    {
        transform.SetSiblingIndex(orderInList);
        backgroundImage.color = backgroundColor;
    }

    public void ShowPostRunUpdate(Runner runner, RunnerUpdateRecord record, float animationSpeed = 1)
    {
        paceText.gameObject.SetActive(false);

        statusText.gameObject.SetActive(true);
        statusText.text = RunUtility.SorenessToStatusString(runner.longTermSoreness);

        experienceContainer.SetActive(true);
        experienceText.text = $"{record.startingExperience} / {record.startingLevelExperienceThreshold}";
        experienceBarFill.fillAmount = (float)record.startingExperience / record.startingLevelExperienceThreshold;

        CNExtensions.SafeStartCoroutine(this, ref postRunUpdateRoutine, PostRunUpdateRoutine(record, animationSpeed));
    }

    private IEnumerator PostRunUpdateRoutine(RunnerUpdateRecord record, float animationSpeed)
    {
        float experienceToAdd = record.experienceChange;
        float currentExperience = record.startingExperience;
        int currentLevelExperienceThreshold = record.startingLevelExperienceThreshold;
        int newLevelIndex = 0;

        while (Mathf.Sign(experienceToAdd) == Mathf.Sign(record.experienceChange))
        {
            // update the experience up or down
            if (experienceToAdd > 0)
            {
                experienceToAdd -= animationSpeed * Time.deltaTime;
                currentExperience += animationSpeed * Time.deltaTime;
            }
            else
            {
                experienceToAdd += animationSpeed * Time.deltaTime;
                currentExperience -= animationSpeed * Time.deltaTime;
            }

            // if needed do a level up
            if (currentExperience >= currentLevelExperienceThreshold)
            {
                levelText.text = $"LV {record.levelUpRecords[newLevelIndex].newLevel}";
                currentExperience = currentLevelExperienceThreshold - currentExperience;
                currentLevelExperienceThreshold = record.levelUpRecords[newLevelIndex].newLevelExperienceThreshold;
                newLevelIndex++;
            }

            // update the UI
            experienceText.text = $"{Mathf.RoundToInt(currentExperience)} / {currentLevelExperienceThreshold}";
            experienceBarFill.fillAmount = currentExperience / currentLevelExperienceThreshold;

            yield return null;
        }
    }
}
