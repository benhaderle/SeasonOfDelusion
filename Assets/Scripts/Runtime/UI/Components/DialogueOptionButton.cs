using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueOptionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI optionText;
    private DialogueOption option;

    public void Setup(DialogueOption option, MarkupPalette palette, UnityAction action)
    {
        this.option = option;

        // When we're given an Option, use its text and update our
        // interactibility.
        Yarn.Markup.MarkupParseResult line;
        line = option.Line.TextWithoutCharacterName;

        if (palette != null)
        {
            optionText.text = LineView.PaletteMarkedUpText(line, palette, false);
        }
        else
        {
            optionText.text = line.Text;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
