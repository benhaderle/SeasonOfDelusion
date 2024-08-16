using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RouteCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;

    [SerializeField] private TextMeshProUGUI lengthText;

    public void Setup(Route route)
    {
        nameText.text = route.Name;
        lengthText.text = $"{route.Length} mi";
    }
}
