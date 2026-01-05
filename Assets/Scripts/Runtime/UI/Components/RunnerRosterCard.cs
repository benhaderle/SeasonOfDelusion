using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// One of the UI cards representing one Runner on the Roster screen
/// </summary> 
public class RunnerRosterCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image experienceBarFill;
    [SerializeField] private TextMeshProUGUI experienceText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image thirstSprite;
    [SerializeField] private Image hungerSprite;
    [SerializeField] private Image sleepSprite;
    [SerializeField] private Image sorenessSprite;
    [SerializeField] private Image academicSprite;


    public void Setup(Runner runner, Color backgroundColor, UnityAction buttonAction)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(buttonAction);

        portraitImage.sprite = runner.GetCurrentConfidenceSprite();

        nameText.text = $"{runner.FirstName}\n{runner.LastName}";

        experienceBarFill.fillAmount = (float)runner.experience / runner.GetCurrentLevelExperienceThreshold();
        experienceText.text = $"{runner.experience} / {runner.GetCurrentLevelExperienceThreshold()}";

        levelText.text = $"LV {runner.level}";

        backgroundImage.color = backgroundColor;

        thirstSprite.gameObject.SetActive(false);
        hungerSprite.gameObject.SetActive(false);
        sleepSprite.gameObject.SetActive(false);
        sorenessSprite.gameObject.SetActive(false);
        academicSprite.gameObject.SetActive(false);
    }
}
