using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;
using System.Linq;

public class RouteUIController : MonoBehaviour
{
    private enum State { RouteSelection = 0, EaseSelection = 1 };
    private State currentState;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PoolContext routeCardPool;
    [SerializeField] private RectTransform routeSelectionContainer;
    [SerializeField] private RectTransform easeSelectionContainer;
    private Route selectedRoute;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void Awake()
    {
        currentState = State.RouteSelection;
        routeCardPool.Initialize();
        OnToggle(true);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        toggleEvent.AddListener(OnToggle);
    }

    private void OnToggle(bool active)
    {
        if(active)
        {
            switch (currentState)
            {
                case State.RouteSelection: SetupRouteSelection(); break;
                case State.EaseSelection: SetupEaseSelection(); break;
            }
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
        routeCardPool.ReturnAllToPool();
    }

    private void OnRouteSelectionButton(Route route)
    {
        switch (currentState)
        {
            case State.RouteSelection:
                selectedRoute = route;
                currentState = State.EaseSelection;
                SetupEaseSelection();
                break;
            case State.EaseSelection:
                selectedRoute = null;
                currentState = State.RouteSelection;
                SetupRouteSelection();
                break;
        }
    }

    public void OnEaseButton(float easeGuidance)
    {
        RunController.startRunEvent.Invoke(new RunController.StartRunEvent.Context
        {
            runners = TeamModel.Instance.Runners.ToList(),
            route = selectedRoute,
            runConditions = new RunConditions
            {
                coachVO2Guidance = easeGuidance
            }
        });

        OnToggle(false);
    }    

    private void SetupRouteSelection()
    {
        routeSelectionContainer.gameObject.SetActive(true);
        easeSelectionContainer.gameObject.SetActive(false);

        routeCardPool.ReturnAllToPool();
        for(int i = 0; i < RouteModel.Instance.TodaysRoutes.Count; i++)
        {
            Route r = RouteModel.Instance.TodaysRoutes[i];
            RouteCard card = routeCardPool.GetPooledObject<RouteCard>();
            card.transform.parent = routeSelectionContainer;
            card.Setup(r);
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => OnRouteSelectionButton(r));
        }
    }

    private void SetupEaseSelection()
    {
        routeSelectionContainer.gameObject.SetActive(false);
        easeSelectionContainer.gameObject.SetActive(true);

        routeCardPool.ReturnAllToPool();
        RouteCard card = routeCardPool.GetPooledObject<RouteCard>();
        card.transform.parent = easeSelectionContainer;
        card.Setup(selectedRoute);
        card.Button.onClick.RemoveAllListeners();
        card.Button.onClick.AddListener(() => OnRouteSelectionButton(selectedRoute));
        card.transform.SetSiblingIndex(0);
    }
}
