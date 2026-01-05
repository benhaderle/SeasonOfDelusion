using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI experienceText;
    [SerializeField] private Image experienceBarFill;

    public void SetBar(int numExperiencePoints, int numExperiencePointsToNextLevel)
    {
        experienceText.text = $"{numExperiencePoints}/{numExperiencePointsToNextLevel}";
        experienceBarFill.fillAmount = (float)numExperiencePoints / numExperiencePointsToNextLevel;
    }
}
