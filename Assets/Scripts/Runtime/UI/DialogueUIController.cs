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
    public static StartDialogueEvent startDialogueEvent = new StartDialogueEvent();
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
        startDialogueEvent.AddListener(OnStartDialogue);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        startDialogueEvent.RemoveListener(OnStartDialogue);
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
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOnRoutine());
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private void OnStartDialogue(StartDialogueEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, StartDialogueRoutine(context.dialogueID));

    }

    public void OnDialogueCompleted()
    {
        OnToggle(false);
        dialogueEndedEvent.Invoke(new DialogueEndedEvent.Context { dialogueID = currentDialogueID });
    }

    #region Utility Functions

    private IEnumerator StartDialogueRoutine(string dialogueID)
    {
        yield return ToggleOnRoutine();
        currentDialogueID = dialogueID;
        dialogueRunner.StartDialogue($"{dialogueID}Dialogue");
    }

    private IEnumerator ToggleOnRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true);
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);
    }

    #endregion
}
