using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RosterRunnerPage : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private ExperienceBar experienceBar;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI aerobicStat;
    [SerializeField] private TextMeshProUGUI strengthStat;
    [SerializeField] private TextMeshProUGUI formStat;
    [SerializeField] private TextMeshProUGUI gritStat;
    [SerializeField] private TextMeshProUGUI recoveryStat;
    [SerializeField] private TextMeshProUGUI sorenessStatus;
    [SerializeField] private TextMeshProUGUI sleepStatus;
    [SerializeField] private TextMeshProUGUI nutritionStatus;
    [SerializeField] private TextMeshProUGUI hydrationStatus;

    public void SetUp(Runner runner)
    {
        portraitImage.sprite = runner.GetCurrentConfidenceSprite();
        nameText.text = runner.Name;
        experienceBar.SetBar(runner.experience, runner.GetCurrentLevelExperienceThreshold());
        levelText.text = $"LV {runner.level}";

        aerobicStat.text = runner.currentVO2Max.ToString("0.0");
        strengthStat.text = runner.currentStrength.ToString("0.0");
        formStat.text = runner.currentForm.ToString("0.0");
        gritStat.text = runner.currentGrit.ToString("0.0");
        recoveryStat.text = runner.currentRecovery.ToString("0.0");

        sorenessStatus.text = runner.GetDisplayableCurrentSoreness().ToString("0.0");
        sleepStatus.text = runner.GetDisplayableCurrentSleep().ToString("0.0");
        nutritionStatus.text = runner.GetDisplayableCurrentNutrition().ToString("0.0");
        hydrationStatus.text = runner.GetDisplayableCurrentHydration().ToString("0.0");
    }
}
