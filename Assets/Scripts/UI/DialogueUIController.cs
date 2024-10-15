using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEngine.UI;

public enum DialogueID { Intro };

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();

    public class StartDialgoueEvent : UnityEvent<StartDialgoueEvent.Context> 
    {
        public class Context
        {
            public DialogueID dialogueID;
        }
    };
    public static StartDialgoueEvent startDialgoueEvent = new StartDialgoueEvent();
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

    private void OnStartDialogue(StartDialgoueEvent.Context context)
    {
        
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);
    }
}
