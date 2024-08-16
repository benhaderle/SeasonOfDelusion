using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;

public class RouteUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PoolContext routeCardPool;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void Awake()
    {
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

    public void OnRosterButton()
    {
        OnToggle(false);
        RosterUIController.toggleEvent.Invoke(true);
    }    

    private void OnToggle(bool active)
    {
        if(active)
        {
            routeCardPool.ReturnAllToPool();
            for(int i = 0; i < RouteModel.Instance.Routes.Count; i++)
            {
                RouteCard card = routeCardPool.GetPooledObject<RouteCard>();
                card.Setup(RouteModel.Instance.Routes[i]);
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
}
