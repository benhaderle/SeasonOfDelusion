using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using Yarn.Unity;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    private string currentDialogueID;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();

    public class StartDialogueEvent : UnityEvent<StartDialogueEvent.Context> 
    {
        public class Context
        {
            public string dialogueID;
        }
    };
    public static StartDialogueEvent startDialgoueEvent = new StartDialogueEvent();
    public class DialogueEndedEvent : UnityEvent<DialogueEndedEvent.Context> 
    {
        public class Context
        {
            public string dialogueID;
        }
    };
    public static DialogueEndedEvent dialogueEndedEvent = new DialogueEndedEvent();
    #endregion

    private void Awake()
    {
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

    #if UNITY_EDITOR
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && dialogueRunner.IsDialogueRunning)
        {
            dialogueRunner.Stop();
        }
    }
    #endif

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
        currentDialogueID = context.dialogueID;
        dialogueRunner.StartDialogue($"{context.dialogueID}Dialogue");
    }

    public void OnDialogueCompleted()
    {
        OnToggle(false);
        dialogueEndedEvent.Invoke(new DialogueEndedEvent.Context { dialogueID = currentDialogueID });
    }

    #region Utility Functions

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);
    }

    #endregion
}
