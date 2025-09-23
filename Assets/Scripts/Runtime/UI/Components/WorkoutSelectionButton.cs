using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

/// <summary>
/// One of the route buttons on the Routes screen
/// </summary>
public class WorkoutSelectionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    public Button Button => button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private TextMeshProUGUI[] statusEffectTexts;
    [SerializeField] private TextMeshProUGUI popularityText;

    public void Setup(Workout workout)
    {
        nameText.text = workout.DisplayName;
        for (int i = 0; i < Mathf.Min(statusEffectTexts.Length, workout.effects.Length); i++)
        {
            statusEffectTexts[i].gameObject.SetActive(true);
            statusEffectTexts[i].text = $"+{workout.effects[i].amount} {workout.effects[i].type}";
        }

        for (int i = workout.effects.Length; i < statusEffectTexts.Length; i++)
        {
            statusEffectTexts[i].gameObject.SetActive(false);
        }
    }
}
