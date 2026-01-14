using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkoutEffectIndicator : MonoBehaviour
{
    [SerializeField] private Image effectTypeImage;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image[] pluses;

    public void Setup(Sprite effectTypeIcon, int effectAmount, string labelText)
    {
        effectTypeImage.sprite = effectTypeIcon;

        for (int i = 0; i < Mathf.Min(effectAmount, pluses.Length); i++)
        {
            pluses[i].gameObject.SetActive(true);
        }

        for (int i = effectAmount; i < pluses.Length; i++)
        {
            pluses[i].gameObject.SetActive(false);
        }
        
        if (label != null)
        {
            label.text = labelText;
        }
    }
}
