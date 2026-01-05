using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using UnityEngine.Events;
using System;
using UnityEngine.UI;

/// <summary>
/// Controls the roster screen.
/// </summary> 
public class RosterUIController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RosterRunnerPage rosterRunnerPage;
    [SerializeField] private GameObject rosterListPage;
    [SerializeField] private PoolContext runnerCardPool;
    [SerializeField] private Color lightCardColor;
    [SerializeField] private Color darkCardColor;

    private IEnumerator toggleRoutine;
    private Action onBackButtonAction;

#region Events
    public class ToggleEvent : UnityEvent<bool, Action> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
#endregion

    private void Awake()
    {
        runnerCardPool.Initialize();
        OnToggle(false, null);
        rosterRunnerPage.gameObject.SetActive(false);
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
    private void OnToggle(bool active, Action onBackButtonAction)
    {
        this.onBackButtonAction = onBackButtonAction;

        if (active)
        {
            runnerCardPool.ReturnAllToPool();
            for (int i = 0; i < TeamModel.Instance.PlayerRunners.Count; i++)
            {
                RunnerRosterCard card = runnerCardPool.GetPooledObject<RunnerRosterCard>();
                Runner r = TeamModel.Instance.PlayerRunners[i];
                card.Setup(r, i % 2 == 0 ? lightCardColor : darkCardColor, () =>
                {
                    ShowRosterRunnerPage(r);
                });
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

    public void OnBackButton()
    {
        if (rosterRunnerPage.gameObject.activeSelf)
        {
            rosterRunnerPage.gameObject.SetActive(false);
            rosterListPage.SetActive(true);
        }
        else
        {
            if (onBackButtonAction != null)
            {
                onBackButtonAction();
            }
            OnToggle(false, null);
        }
    }
    
    private void ShowRosterRunnerPage(Runner r)
    {
        rosterRunnerPage.gameObject.SetActive(true);
        rosterListPage.SetActive(false);

        rosterRunnerPage.SetUp(r);
        
    }

    #endregion

}
