using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentContainer : MonoBehaviour
{
    private static readonly float squareScreenUIContentWidth;
    private void Awake()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        if ((float)Screen.height / Screen.width < 1.5f)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.height / 3f);
        }
    }
}
