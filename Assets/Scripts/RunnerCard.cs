using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions.Must;

public class RunnerCard : MonoBehaviour
{
    private TextMeshProUGUI nameText;

    private TextMeshProUGUI vo2MaxText;

    public void Setup(Runner runner)
    {
        nameText.text = runner.Name;
        vo2MaxText.text = runner.VO2Max.ToString();
    }
}
