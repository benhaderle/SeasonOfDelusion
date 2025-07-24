using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CreateNeptune;

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

    public void UpdateListPosition(int orderInList, Color backgroundColor)
    {
        transform.SetSiblingIndex(orderInList);
        backgroundImage.color = backgroundColor;
    }

    public void ShowPostRunUpdate(Runner runner, RunnerUpdateRecord record)
    {
        paceText.gameObject.SetActive(false);

        statusText.gameObject.SetActive(true);
        statusText.text = RunUtility.ExhaustionToStatusString(runner.longTermSoreness);

        experienceContainer.SetActive(true);
        experienceText.text = $"{record.startingExperience} / {record.startingLevelExperienceThreshold}";
        experienceBarFill.fillAmount = (float)record.startingExperience / record.startingLevelExperienceThreshold;

        CNExtensions.SafeStartCoroutine(this, ref postRunUpdateRoutine, PostRunUpdateRoutine(record));
    }

    private IEnumerator PostRunUpdateRoutine(RunnerUpdateRecord record)
    {
        float animationSpeed = 1;

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
            experienceText.text = $"{(int)currentExperience} / {currentLevelExperienceThreshold}";
            experienceBarFill.fillAmount = currentExperience / currentLevelExperienceThreshold;

            yield return null;
        }
    }
}
