using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkoutRunnerCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI runnerInitialText;
    private Runner runner;

    public void Setup(Runner runner)
    {
        this.runner = runner;
        runnerInitialText.text = $"{runner.FirstName[0]}{runner.LastName[0]}";
    }
}
