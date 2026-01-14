using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

/// <summary>
/// One of the route buttons on the Routes screen
/// </summary>
public class WorkoutSelectionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    public Button Button => button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private WorkoutEffectIndicator[] effectIndicators;
    [SerializeField] private TextMeshProUGUI popularityText;

    public void Setup(Workout workout, SpriteLibraryAsset iconSpriteLibrary)
    {
        nameText.text = workout.DisplayName;

        difficultyText.text = workout.GetDifficultyString();

        // set up active status effects
        for (int i = 0; i < Mathf.Min(effectIndicators.Length, workout.effects.Length); i++)
        {
            effectIndicators[i].gameObject.SetActive(true);
            effectIndicators[i].Setup(iconSpriteLibrary.GetSprite("Stats", workout.effects[i].type.ToString()), (int)workout.effects[i].amount, workout.effects[i].type.ToString().ToLower());
        }

        // turn off any unused status texts
        for (int i = workout.effects.Length; i < effectIndicators.Length; i++)
        {
            effectIndicators[i].gameObject.SetActive(false);
        }
    }
}
