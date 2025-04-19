using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System;

public class HeaderController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI headerText;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void Awake()
    {
        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        SimulationModel.dayEventLoadedEvent.AddListener(OnDayEventLoaded);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        SimulationModel.dayEventLoadedEvent.RemoveListener(OnDayEventLoaded);
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

    private void OnDayEventLoaded(SimulationModel.DayEventLoadedEvent.Context context)
    {
        headerText.text = $"{DateTime.Parse(context.date).DayOfWeek.ToString().Substring(0,3)} {context.date} | {context.time}";
    }
}
