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
        RaceController.startRaceOpportunityEvent.AddListener(OnStartRaceOpportunity);
        RaceController.runnerInRaceOpportunityZoneEvent.AddListener(OnRunnerInRaceOpportunityZone);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        RaceController.startRaceOpportunityEvent.RemoveListener(OnStartRaceOpportunity);
        RaceController.runnerInRaceOpportunityZoneEvent.RemoveListener(OnRunnerInRaceOpportunityZone);
    }

    private void OnToggle(bool active)
    {
        if (active)
        {
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

    private void OnStartRaceOpportunity(RaceController.StartRaceOpportunityEvent.Context context)
    {
        OnToggle(true);
    }

    private void OnRunnerInRaceOpportunityZone(RaceController.RunnerInRaceOpportunityZoneEvent.Context context)
    {
        opportunityPromptText.text = $"{context.runner.FirstName} is here.";
    }

    public void OnRaceOpportunityButton()
    {
        raceOpportunityButtonPressedEvent.Invoke(new RaceOpportunityButtonPressedEvent.Context { });
    }

}
