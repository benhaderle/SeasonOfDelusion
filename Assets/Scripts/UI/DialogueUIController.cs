using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEngine.UI;
using TMPro;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private Dialogue[] dialogues;
    private Dictionary<DialogueID, Dialogue> dialogueDictionary;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    private Dialogue currentDialogue;
    private int currentNodeIndex;
    private int currentPositionInNode;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();

    public class StartDialogueEvent : UnityEvent<StartDialogueEvent.Context> 
    {
        public class Context
        {
            public DialogueID dialogueID;
        }
    };
    public static StartDialogueEvent startDialgoueEvent = new StartDialogueEvent();
    public class DialogueEndedEvent : UnityEvent<DialogueEndedEvent.Context> 
    {
        public class Context
        {
            public DialogueID dialogueID;
        }
    };
    public static DialogueEndedEvent dialogueEndedEvent = new DialogueEndedEvent();
    #endregion

    private void Awake()
    {
        dialogueDictionary = new();
        for(int i = 0; i < dialogues.Length; i++)
        {
            dialogueDictionary.Add(dialogues[i].dialogueID, dialogues[i]);
        }

        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        startDialgoueEvent.AddListener(OnStartDialogue);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        startDialgoueEvent.RemoveListener(OnStartDialogue);
    }

     private void OnToggle(bool active)
    {
        if(active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private void OnStartDialogue(StartDialogueEvent.Context context)
    {
        if (dialogueDictionary.TryGetValue(context.dialogueID, out currentDialogue))
        {
            currentNodeIndex = 0;
            currentPositionInNode = 0;
            currentPositionInNode = DisplayDialogueNode(currentDialogue, currentNodeIndex, currentPositionInNode);
        }
    }

    private int DisplayDialogueNode(Dialogue dialogue, int nodeIndex, int positionInNode)
    {
        DialogueNode node = dialogue.dialogueNodes[nodeIndex];
        speakerText.text = node.speaker.ToUpper();

        dialogueText.text = node.text.Substring(positionInNode);
        dialogueText.ForceMeshUpdate();

        while(dialogueText.isTextOverflowing)
        {
            dialogueText.text = node.text.Substring(positionInNode, SecondToLastIndexOfAny(dialogueText.text, new char[] { '.', '!', '?' }) + 2);
            dialogueText.ForceMeshUpdate();
        }

        positionInNode += dialogueText.text.Length;

        return positionInNode;
    }


    public void OnContinueButton()
    {
        if(currentNodeIndex == currentDialogue.dialogueNodes.Length - 1 && currentPositionInNode >= currentDialogue.dialogueNodes[currentNodeIndex].text.Length)
        {
            OnToggle(false);
            dialogueEndedEvent.Invoke(new DialogueEndedEvent.Context { dialogueID = currentDialogue.dialogueID });
        }
        else if (currentPositionInNode < currentDialogue.dialogueNodes[currentNodeIndex].text.Length)
        {
            currentPositionInNode = DisplayDialogueNode(currentDialogue, currentNodeIndex, currentPositionInNode);
        }
        else 
        {
            currentNodeIndex++;
            currentPositionInNode = 0;
            currentPositionInNode = DisplayDialogueNode(currentDialogue, currentNodeIndex, currentPositionInNode);
        }
    }

    #region Utility Functions

    private int SecondToLastIndexOfAny(string s, char[] chars)
    {
        return  s.Substring(0, s.LastIndexOfAny(chars)).LastIndexOfAny(chars);
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);
    }

    #endregion
}
