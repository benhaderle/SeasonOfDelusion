using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;

/// <summary>
/// Controls the roster screen.
/// </summary> 
public class RosterUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PoolContext runnerCardPool;
    [SerializeField] private Color lightCardColor;
    [SerializeField] private Color darkCardColor;

    private IEnumerator toggleRoutine;

#region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
#endregion

    private void Awake()
    {
        runnerCardPool.Initialize();
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

    #region Event Callbacks
    private void OnToggle(bool active)
    {
        if(active)
        {
            runnerCardPool.ReturnAllToPool();
            for(int i = 0; i < TeamModel.Instance.PlayerRunners.Count; i++)
            {
                RunnerRosterCard card = runnerCardPool.GetPooledObject<RunnerRosterCard>();
                card.Setup(TeamModel.Instance.PlayerRunners[i], i % 2 == 0 ? lightCardColor : darkCardColor);
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
        runnerCardPool.ReturnAllToPool();
    }

    #endregion

    #region Button Callbacks
    
    public void OnBackToRoutesButton()
    {
        OnToggle(false);
        RouteUIController.toggleEvent.Invoke(true);
        CutsceneUIController.toggleEvent.Invoke(true);
    }

    #endregion

}
