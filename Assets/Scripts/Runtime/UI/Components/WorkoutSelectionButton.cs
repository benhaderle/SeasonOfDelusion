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

    public void Setup(Workout workout)
    {
        nameText.text = workout.Name;
    }
}
