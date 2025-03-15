using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentContainer : MonoBehaviour
{
    private void Start()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if ((float)Screen.height / Screen.width < 1.5f)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 2150);
        }
    }
}
