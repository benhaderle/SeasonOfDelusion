using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using System.Linq;

/// <summary>
/// The view for the race simulation
/// </summary> 
public class RaceResultsView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PoolContext teamResultCardPool;
    [SerializeField] private RectTransform teamResultCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    [SerializeField] private CanvasGroup continueButtonContainer;
    private IEnumerator toggleRoutine;
    private IEnumerator continueButtonToggleRoutine;

    #region Events
    public class PostRaceContinueButtonPressedEvent : UnityEvent<PostRaceContinueButtonPressedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static PostRaceContinueButtonPressedEvent postRaceContinueButtonPressedEvent = new();
    #endregion

    private void Awake()
    {
        teamResultCardPool.Initialize();
        Toggle(false);
    }

    private void OnEnable()
    {
        RaceController.raceSimulationEndedEvent.AddListener(OnRaceSimulationEnded);
    }

    private void OnDisable()
    {
        RaceController.raceSimulationEndedEvent.RemoveListener(OnRaceSimulationEnded);
    }

    public void OnContinueButton()
    {
        Toggle(false);
        postRaceContinueButtonPressedEvent.Invoke(new PostRaceContinueButtonPressedEvent.Context { });
    }

    private void OnRaceSimulationEnded(RaceController.RaceSimulationEndedEvent.Context context)
    {
        Toggle(true);

        teamResultCardParent.gameObject.SetActive(true);
        for (int i = 0; i < context.sortedTeamRaceResultRecords.Count; i++)
        {
            // runner card setup
            TeamRaceResultsCard card = teamResultCardPool.GetPooledObject<TeamRaceResultsCard>();
            card.Setup(context.sortedTeamRaceResultRecords[i], i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor, i);
        }

        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }

    /// <summary>
    /// Toggles the view on and off and cleans up pools if necessary
    /// </summary>
    /// <param name="active">Whether the view should be active or not</param>
    /// <param name="cleanUp">Whether we should clean up all the pooled objects if we're turning the view inactive</param>
    private void Toggle(bool active)
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

        teamResultCardPool.ReturnAllToPool();
        teamResultCardParent.gameObject.SetActive(false);
    }
}
