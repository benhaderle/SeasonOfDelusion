using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;
using System.Linq;
using TMPro;

/// <summary>
/// Controls the Race Opportunity UI
/// </summary>
public class RaceOpportunityUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI opportunityPromptText;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    public class RaceOpportunityButtonPressedEvent : UnityEvent<RaceOpportunityButtonPressedEvent.Context>
    {
        public class Context
        {
            public int ease;
        }
    }
    public static RaceOpportunityButtonPressedEvent raceOpportunityButtonPressedEvent = new();
    #endregion

    private void Awake()
    {
        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        RaceController.raceOpportunityStartedEvent.AddListener(OnRaceOpportunityStarted);
        RaceController.raceOpportunityEndedEvent.AddListener(OnRaceOpportunityEnded);
        RaceController.runnerInRaceOpportunityZoneEvent.AddListener(OnRunnerInRaceOpportunityZone);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        RaceController.raceOpportunityStartedEvent.RemoveListener(OnRaceOpportunityStarted);
        RaceController.raceOpportunityEndedEvent.RemoveListener(OnRaceOpportunityEnded);
        RaceController.runnerInRaceOpportunityZoneEvent.RemoveListener(OnRunnerInRaceOpportunityZone);
    }

    private void OnToggle(bool active)
    {
        if (active)
        {
            opportunityPromptText.text = "";
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);
    }

    private void OnRaceOpportunityStarted(RaceController.RaceOpportunityStartedEvent.Context context)
    {
        OnToggle(true);
    }

    private void OnRunnerInRaceOpportunityZone(RaceController.RunnerInRaceOpportunityZoneEvent.Context context)
    {
        opportunityPromptText.text = $"{context.runner.FirstName} is here.";
    }

    private void OnRaceOpportunityEnded(RaceController.RaceOpportunityEndedEvent.Context context)
    {
        OnToggle(false);
    }

    public void OnRaceOpportunityButton(int ease)
    {
        opportunityPromptText.text = "";
        raceOpportunityButtonPressedEvent.Invoke(new RaceOpportunityButtonPressedEvent.Context { ease = ease });
    }

}
