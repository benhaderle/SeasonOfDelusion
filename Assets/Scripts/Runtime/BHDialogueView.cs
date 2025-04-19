using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;
using TMPro;

/// <summary>
/// A Dialogue View that presents lines of dialogue, using Unity UI
/// elements.
/// </summary>
public class BHDialogueView : DialogueViewBase
{
    [Header("Animation and Visuals")]
    public MarkupPalette palette;

    /// <summary>
    /// Controls whether the line view should fade in when lines appear, and
    /// fade out when lines disappear.
    /// </summary>
    /// <remarks><para>If this value is <see langword="true"/>, the <see
    /// cref="linesCanvasGroup"/> object's alpha property will animate from 0 to
    /// 1 over the course of <see cref="fadeInTime"/> seconds when lines
    /// appear, and animate from 1 to zero over the course of <see
    /// cref="fadeOutTime"/> seconds when lines disappear.</para>
    /// <para>If this value is <see langword="false"/>, the <see
    /// cref="linesCanvasGroup"/> object will appear instantaneously.</para>
    /// </remarks>
    /// <seealso cref="linesCanvasGroup"/>
    /// <seealso cref="fadeInTime"/>
    /// <seealso cref="fadeOutTime"/>
    public bool useFadeEffect = true;

    /// <summary>
    /// The time that the fade effect will take to fade lines in.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Min(0)]
    public float fadeInTime = 0.25f;

    /// <summary>
    /// The time that the fade effect will take to fade lines out.
    /// </summary>
    /// <remarks>This value is only used when <see cref="useFadeEffect"/> is
    /// <see langword="true"/>.</remarks>
    /// <seealso cref="useFadeEffect"/>
    [Min(0)]
    public float fadeOutTime = 0.05f;

    [SerializeField] private float typewriterAnimationSpeed = 1;

    [Header("Lines View")]
    /// <summary>
    /// The canvas group that contains the UI elements used by this Line
    /// View.
    /// </summary>
    /// <remarks>
    /// If <see cref="useFadeEffect"/> is true, then the alpha value of this
    /// <see cref="CanvasGroup"/> will be animated during line presentation
    /// and dismissal.
    /// </remarks>
    /// <seealso cref="useFadeEffect"/>
    public CanvasGroup linesCanvasGroup;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the text of
    /// dialogue lines.
    /// </summary>
    public TextMeshProUGUI lineText = null;

    /// <summary>
    /// Controls whether the <see cref="lineText"/> object will show the
    /// character name present in the line or not.
    /// </summary>
    /// <remarks>
    /// <para style="note">This value is only used if <see
    /// cref="characterNameText"/> is <see langword="null"/>.</para>
    /// <para>If this value is <see langword="true"/>, any character names
    /// present in a line will be shown in the <see cref="lineText"/>
    /// object.</para>
    /// <para>If this value is <see langword="false"/>, character names will
    /// not be shown in the <see cref="lineText"/> object.</para>
    /// </remarks>
    [UnityEngine.Serialization.FormerlySerializedAs("showCharacterName")]
    public bool showCharacterNameInLineView = true;

    /// <summary>
    /// The <see cref="TextMeshProUGUI"/> object that displays the character
    /// names found in dialogue lines.
    /// </summary>
    /// <remarks>
    /// If the <see cref="LineView"/> receives a line that does not contain
    /// a character name, this object will be left blank.
    /// </remarks>
    public TextMeshProUGUI characterNameText = null;

    /// <summary>
    /// The gameobject that holds the <see cref="characterNameText"/> textfield.
    /// </summary>
    /// <remarks>
    /// This is needed in situations where the character name is contained within an entirely different game object.
    /// Most of the time this will just be the same gameobject as <see cref="characterNameText"/>.
    /// </remarks>
    public GameObject characterNameContainer = null;

    /// <summary>
    /// The game object that represents an on-screen button that the user
    /// can click to continue to the next piece of dialogue.
    /// </summary>
    /// <remarks>
    /// <para>This game object will be made inactive when a line begins
    /// appearing, and active when the line has finished appearing.</para>
    /// <para>
    /// This field will generally refer to an object that has a <see
    /// cref="Button"/> component on it that, when clicked, calls <see
    /// cref="OnContinueClicked"/>. However, if your game requires specific
    /// UI needs, you can provide any object you need.</para>
    /// </remarks>
    /// <seealso cref="autoAdvance"/>
    public GameObject continueButton = null;

    /// <summary>
    /// The current <see cref="LocalizedLine"/> that this line view is
    /// displaying.
    /// </summary>
    LocalizedLine currentLine = null;

    /// <summary>
    /// A stop token that is used to interrupt the current animation.
    /// </summary>
    Effects.CoroutineInterruptToken currentStopToken = new Effects.CoroutineInterruptToken();

    [Header("Options View")]
    [SerializeField] private CanvasGroup optionsCanvasGroup;
    [SerializeField] private TextMeshProUGUI lastLineText;
    [SerializeField] private GameObject lastLineContainer;
    [SerializeField] private TextMeshProUGUI lastLineCharacterNameText;
    [SerializeField] private GameObject lastLineCharacterNameContainer;

    // A cached pool of OptionView objects so that we can reuse them
    [SerializeField] private List<DialogueOptionButton> optionButtons = new();

    // The method we should call when an option has been selected.
    private Action<int> OnOptionSelected;

    // The line we saw most recently.
    private LocalizedLine lastSeenLine;

    private void Awake()
    {
        linesCanvasGroup.alpha = 0;
        linesCanvasGroup.blocksRaycasts = false;
        optionsCanvasGroup.alpha = 0;
        optionsCanvasGroup.blocksRaycasts = false;
    }

    /// <inheritdoc/>
    public override void DismissLine(Action onDismissalComplete)
    {
        currentLine = null;

        StartCoroutine(DismissLineInternal(onDismissalComplete));
    }

    private IEnumerator DismissLineInternal(Action onDismissalComplete)
    {
        // disabling interaction temporarily while dismissing the line
        // we don't want people to interrupt a dismissal
        var interactable = linesCanvasGroup.interactable;
        linesCanvasGroup.interactable = false;

        // If we're using a fade effect, run it, and wait for it to finish.
        if (useFadeEffect)
        {
            yield return StartCoroutine(Effects.FadeAlpha(linesCanvasGroup, 1, 0, fadeOutTime, currentStopToken));
            currentStopToken.Complete();
        }

        linesCanvasGroup.alpha = 0;
        linesCanvasGroup.blocksRaycasts = false;
        // turning interaction back on, if it needs it
        linesCanvasGroup.interactable = interactable;

        if (onDismissalComplete != null)
        {
            onDismissalComplete();
        }
    }

    /// <inheritdoc/>
    public override void InterruptLine(LocalizedLine dialogueLine, Action onInterruptLineFinished)
    {
        currentLine = dialogueLine;

        // Cancel all coroutines that we're currently running. This will
        // stop the RunLineInternal coroutine, if it's running.
        StopAllCoroutines();

        // for now we are going to just immediately show everything
        // later we will make it fade in
        lineText.gameObject.SetActive(true);
        linesCanvasGroup.gameObject.SetActive(true);

        int length;

        if (characterNameText == null)
        {
            if (showCharacterNameInLineView)
            {
                lineText.text = dialogueLine.Text.Text;
                length = dialogueLine.Text.Text.Length;
            }
            else
            {
                lineText.text = dialogueLine.TextWithoutCharacterName.Text;
                length = dialogueLine.TextWithoutCharacterName.Text.Length;
            }
        }
        else
        {
            characterNameText.text = dialogueLine.CharacterName;
            lineText.text = dialogueLine.TextWithoutCharacterName.Text;
            length = dialogueLine.TextWithoutCharacterName.Text.Length;
        }

        // Show the entire line's text immediately.
        lineText.maxVisibleCharacters = length;

        // Make the canvas group fully visible immediately, too.
        linesCanvasGroup.alpha = 1;

        linesCanvasGroup.interactable = true;
        linesCanvasGroup.blocksRaycasts = true;

        onInterruptLineFinished();
    }

    /// <inheritdoc/>
    public override void RunLine(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        // Stop any coroutines currently running on this line view (for
        // example, any other RunLine that might be running)
        StopAllCoroutines();

        // Begin running the line as a coroutine.
        StartCoroutine(RunLineInternal(dialogueLine, onDialogueLineFinished));
    }

    private IEnumerator RunLineInternal(LocalizedLine dialogueLine, Action onDialogueLineFinished)
    {
        IEnumerator PresentLine()
        {
            lineText.gameObject.SetActive(true);
            linesCanvasGroup.gameObject.SetActive(true);

            // Hide the continue button until presentation is complete (if
            // we have one).
            if (continueButton != null)
            {
                continueButton.SetActive(false);
            }

            Yarn.Markup.MarkupParseResult text = dialogueLine.TextWithoutCharacterName;
            if (characterNameContainer != null && characterNameText != null)
            {
                // we are set up to show a character name, but there isn't one
                // so just hide the container
                if (string.IsNullOrWhiteSpace(dialogueLine.CharacterName))
                {
                    characterNameContainer.SetActive(false);
                }
                else
                {
                    // we have a character name text view, show the character name
                    characterNameText.text = dialogueLine.CharacterName;
                    characterNameContainer.SetActive(true);
                }
            }
            else
            {
                // We don't have a character name text view. Should we show
                // the character name in the main text view?
                if (showCharacterNameInLineView)
                {
                    // Yep! Show the entire text.
                    text = dialogueLine.Text;
                }
            }

            // if we have a palette file need to add those colours into the text
            if (palette != null)
            {
                lineText.text = LineView.PaletteMarkedUpText(text, palette);
            }
            else
            {
                lineText.text = LineView.AddLineBreaks(text);
            }

            // If we're using the fade effect, start it, and wait for it to
            // finish.
            if (useFadeEffect)
            {
                yield return StartCoroutine(Effects.FadeAlpha(linesCanvasGroup, 0, 1, fadeInTime, currentStopToken));
                if (currentStopToken.WasInterrupted)
                {
                    // The fade effect was interrupted. Stop this entire
                    // coroutine.
                    yield break;
                }
            }
            else
            {
                linesCanvasGroup.alpha = 1f;
            }

            yield return StartCoroutine(WordTypewriterRoutine(lineText, typewriterAnimationSpeed));
        }

        currentLine = dialogueLine;
        lastSeenLine = dialogueLine;

        // Run any presentations as a single coroutine. If this is stopped,
        // which UserRequestedViewAdvancement can do, then we will stop all
        // of the animations at once.
        yield return StartCoroutine(PresentLine());

        currentStopToken.Complete();

        // All of our text should now be visible.
        lineText.maxVisibleCharacters = int.MaxValue;

        // Our view should at be at full opacity.
        linesCanvasGroup.alpha = 1f;
        linesCanvasGroup.blocksRaycasts = true;

        // Show the continue button, if we have one.
        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    /// <inheritdoc/>
    public override void UserRequestedViewAdvancement()
    {
        // We received a request to advance the view. If we're in the middle of
        // an animation, skip to the end of it. If we're not current in an
        // animation, interrupt the line so we can skip to the next one.

        // we have no line, so the user just mashed randomly
        if (currentLine == null)
        {
            return;
        }

        // we may want to change this later so the interrupted
        // animation coroutine is what actually interrupts
        // for now this is fine.
        // Is an animation running that we can stop?
        if (currentStopToken.CanInterrupt)
        {
            // Stop the current animation, and skip to the end of whatever
            // started it.
            currentStopToken.Interrupt();
        }
        else
        {
            // No animation is now running. Signal that we want to
            // interrupt the line instead.
            requestInterrupt?.Invoke();
        }
    }

    /// <summary>
    /// Called when the <see cref="continueButton"/> is clicked.
    /// </summary>
    public void OnContinueClicked()
    {
        // When the Continue button is clicked, we'll do the same thing as
        // if we'd received a signal from any other part of the game (for
        // example, if a DialogueAdvanceInput had signalled us.)
        UserRequestedViewAdvancement();
    }

    /// <inheritdoc />
    /// <remarks>
    /// If a line is still being shown dismisses it.
    /// </remarks>
    public override void DialogueComplete()
    {
        // do we still have a line lying around?
        if (currentLine != null)
        {
            currentLine = null;
            StopAllCoroutines();
            StartCoroutine(DismissLineInternal(null));
        }
    }

    /// <summary>
    /// Applies the <paramref name="palette"/> to the line based on it's markup.
    /// </summary>
    /// <remarks>
    /// This is static so that other dialogue views can reuse this code.
    /// While this is simplistic it is useful enough that multiple pieces might well want it.
    /// </remarks>
    /// <param name="line">The parsed marked up line with it's attributes.</param>
    /// <param name="palette">The palette mapping attributes to colours.</param>
    /// <param name="applyLineBreaks">If the [br /] marker is found in the line should this be replaced with a line break?</param>
    /// <returns>A TMP formatted string with the palette markup values injected within.</returns>
    public static string PaletteMarkedUpText(Yarn.Markup.MarkupParseResult line, MarkupPalette palette, bool applyLineBreaks = true)
    {
        string lineOfText = line.Text;
        line.Attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // we have a colour that matches the current marker
            Color markerColour;
            if (palette.ColorForMarker(attribute.Name, out markerColour))
            {
                // we use the range on the marker to insert the TMP <color> tags
                // not the best approach but will work ok for this use case
                lineOfText = lineOfText.Insert(attribute.Position + attribute.Length, "</color>");
                lineOfText = lineOfText.Insert(attribute.Position, $"<color=#{ColorUtility.ToHtmlStringRGB(markerColour)}>");
            }

            if (applyLineBreaks && attribute.Name == "br")
            {
                lineOfText = lineOfText.Insert(attribute.Position, "<br>");
            }
        }
        return lineOfText;
    }

    public static string AddLineBreaks(Yarn.Markup.MarkupParseResult line)
    {
        string lineOfText = line.Text;
        line.Attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes.Where(a => a.Name == "br"))
        {
            // we then replace the marker with the tmp <br>
            lineOfText = lineOfText.Insert(attribute.Position, "<br>");
        }
        return lineOfText;
    }

    /// <summary>
    /// Creates a stack of typewriter pauses to use to temporarily halt the typewriter effect.
    /// </summary>
    /// <remarks>
    /// This is intended to be used in conjunction with the <see cref="Effects.PausableTypewriter"/> effect.
    /// The stack of tuples created are how the typewriter effect knows when, and for how long, to halt the effect.
    /// <para>
    /// The pause duration property is in milliseconds but all the effects code assumes seconds
    /// So here we will be dividing it by 1000 to make sure they interconnect correctly.
    /// </para>
    /// </remarks>
    /// <param name="line">The line from which we covet the pauses</param>
    /// <returns>A stack of positions and duration pause tuples from within the line</returns>
    public static Stack<(int position, float duration)> GetPauseDurationsInsideLine(Yarn.Markup.MarkupParseResult line)
    {
        var pausePositions = new Stack<(int, float)>();
        var label = "pause";

        // sorting all the attributes in reverse positional order
        // this is so we can build the stack up in the right positioning
        var attributes = line.Attributes;
        attributes.Sort((a, b) => (b.Position.CompareTo(a.Position)));
        foreach (var attribute in line.Attributes)
        {
            // if we aren't a pause skip it
            if (attribute.Name != label)
            {
                continue;
            }

            // did they set a custom duration or not, as in did they do this:
            //     Alice: this is my line with a [pause = 1000 /]pause in the middle
            // or did they go:
            //     Alice: this is my line with a [pause /]pause in the middle
            if (attribute.Properties.TryGetValue(label, out Yarn.Markup.MarkupValue value))
            {
                // depending on the property value we need to take a different path
                // this is because they have made it an integer or a float which are roughly the same
                // note to self: integer and float really ought to be convertible...
                // but they also might have done something weird and we need to handle that
                switch (value.Type)
                {
                    case Yarn.Markup.MarkupValueType.Integer:
                        float duration = value.IntegerValue;
                        pausePositions.Push((attribute.Position, duration / 1000));
                        break;
                    case Yarn.Markup.MarkupValueType.Float:
                        pausePositions.Push((attribute.Position, value.FloatValue / 1000));
                        break;
                    default:
                        Debug.LogWarning($"Pause property is of type {value.Type}, which is not allowed. Defaulting to one second.");
                        pausePositions.Push((attribute.Position, 1));
                        break;
                }
            }
            else
            {
                // they haven't set a duration, so we will instead use the default of one second
                pausePositions.Push((attribute.Position, 1));
            }
        }
        return pausePositions;
    }
    public override void RunOptions(DialogueOption[] dialogueOptions, Action<int> onOptionSelected)
    {
        for (int i = 0; i < dialogueOptions.Length; i++)
        {
            DialogueOptionButton optionButton = optionButtons[i];
            DialogueOption option = dialogueOptions[i];

            if (!option.IsAvailable)
            {
                // Don't show this option.
                continue;
            }

            optionButton.gameObject.SetActive(true);
            optionButton.Setup(option, palette, () =>
            {
                StartCoroutine(ToggleOptionsOffRoutine(option));
            });
        }

        for (int i = dialogueOptions.Length; i < optionButtons.Count; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
        }

        // Update the last line, if one is configured
        if (lastLineContainer != null)
        {
            if (lastSeenLine != null)
            {
                // if we have a last line character name container
                // and the last line has a character then we show the nameplate
                // otherwise we turn off the nameplate
                var line = lastSeenLine.Text;
                if (lastLineCharacterNameContainer != null)
                {
                    if (string.IsNullOrWhiteSpace(lastSeenLine.CharacterName))
                    {
                        lastLineCharacterNameContainer.SetActive(false);
                    }
                    else
                    {
                        line = lastSeenLine.TextWithoutCharacterName;
                        lastLineCharacterNameContainer.SetActive(true);
                        lastLineCharacterNameText.text = lastSeenLine.CharacterName;
                    }
                }

                if (palette != null)
                {
                    lastLineText.text = LineView.PaletteMarkedUpText(line, palette);
                }
                else
                {
                    lastLineText.text = line.Text;
                }

                lastLineContainer.SetActive(true);
            }
            else
            {
                lastLineContainer.SetActive(false);
            }
        }

        // Note the delegate to call when an option is selected
        OnOptionSelected = onOptionSelected;

        // Fade it all in
        StartCoroutine(Effects.FadeAlpha(optionsCanvasGroup, 0, 1, fadeInTime));
    }

    /// <summary>
    /// Fades canvas and then disables all option views.
    /// </summary>
    private IEnumerator FadeOptionsGroupRoutine(CanvasGroup canvasGroup, float from, float to, float fadeTime)
    {
        yield return Effects.FadeAlpha(canvasGroup, from, to, fadeTime);
    }

    private IEnumerator ToggleOptionsOffRoutine(DialogueOption selectedOption)
    {
        yield return StartCoroutine(FadeOptionsGroupRoutine(optionsCanvasGroup, 1, 0, fadeOutTime));
        OnOptionSelected(selectedOption.DialogueOptionID);
    }

    private IEnumerator WordTypewriterRoutine(TextMeshProUGUI text, float lettersPerSecond)
    {
        // Start with everything invisible
        text.maxVisibleCharacters = 0;

        // Wait a single frame to let the text component process its
        // content, otherwise text.textInfo.characterCount won't be
        // accurate
        yield return null;

        // How many visible characters are present in the text?
        int characterCount = text.textInfo.characterCount;

        // Early out if letter speed is zero, text length is zero
        if (lettersPerSecond <= 0 || characterCount == 0)
        {
            // Show everything and return
            text.maxVisibleCharacters = characterCount;
            yield break;
        }

        // Convert 'letters per second' into its inverse
        float secondsPerLetter = 1.0f / lettersPerSecond;

        // If lettersPerSecond is larger than the average framerate, we
        // need to show more than one letter per frame, so simply
        // adding 1 letter every secondsPerLetter won't be good enough
        // (we'd cap out at 1 letter per frame, which could be slower
        // than the user requested.)
        //
        // Instead, we'll accumulate time every frame, and display as
        // many letters in that frame as we need to in order to achieve
        // the requested speed.
        float accumulator = Time.deltaTime;

        int[] wordLengths = text.text.Split(" ").Select(word => word.Length).ToArray();
        int currentWordIndex = 0;

        while (text.maxVisibleCharacters < characterCount)
        {
            // We need to show as many letters as we have accumulated
            // time for.
            while (currentWordIndex < wordLengths.Length && accumulator >= secondsPerLetter * wordLengths[currentWordIndex])
            {
                text.maxVisibleCharacters += wordLengths[currentWordIndex]+1; // +1 for the space after the word
                accumulator -= secondsPerLetter * wordLengths[currentWordIndex];
                currentWordIndex++;
            }
            accumulator += Time.deltaTime;

            yield return null;
        }

        // We either finished displaying everything, or were
        // interrupted. Either way, display everything now.
        text.maxVisibleCharacters = characterCount;
    }

}

