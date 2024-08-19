using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class RouteCard : MonoBehaviour
{
    [SerializeField] private Button button;
    public Button Button => button;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lengthText;
    private  Route route;
    public void Setup(Route route)
    {
        nameText.text = route.Name;
        lengthText.text = $"{route.Length} mi";
        this.route = route;
    }
}
