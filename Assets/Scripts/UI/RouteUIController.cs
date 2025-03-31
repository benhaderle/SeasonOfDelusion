using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Controls both the Route selection and also the coach guidance selection.
/// TODO move coach guidance to its own script
/// </summary>
public class RouteUIController : MonoBehaviour
{
    private enum State { RouteSelection = 0, EaseSelection = 1 };
    private State currentState;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RawImage mapDislayImage;
    private Route selectedRoute;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void Awake()
    {
        currentState = State.RouteSelection;
        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
    }

    private void OnToggle(bool active)
    {
        if (active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));

            Rect mapPixelRect = RectTransformUtility.PixelAdjustRect(mapDislayImage.rectTransform, canvas);
            float scale = Mathf.Min(mapDislayImage.texture.width / mapPixelRect.width, mapDislayImage.texture.height / mapPixelRect.height);
            float offset = (1 - scale) / 2f;
            mapDislayImage.uvRect = new Rect(offset, offset, scale, scale);
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

    public void OnRosterButton()
    {
        OnToggle(false);
        CutsceneUIController.toggleEvent.Invoke(false);
        RosterUIController.toggleEvent.Invoke(true, () =>
        {
            CutsceneUIController.toggleEvent.Invoke(true);
            toggleEvent.Invoke(true);
        });
    }

    private void OnRouteSelectionButton(Route route)
    {
        switch (currentState)
        {
            case State.RouteSelection:
                selectedRoute = route;
                currentState = State.EaseSelection;
                break;
            case State.EaseSelection:
                selectedRoute = null;
                currentState = State.RouteSelection;
                break;
        }
    }

    public void OnEaseButton(float easeGuidance)
    {
        RunController.startRunEvent.Invoke(new RunController.StartRunEvent.Context
        {
            runners = TeamModel.Instance.PlayerRunners.ToList(),
            route = selectedRoute,
            runConditions = new RunConditions
            {
                coachVO2Guidance = easeGuidance
            }
        });

        OnToggle(false);
        CutsceneUIController.toggleEvent.Invoke(false);
    }
}
