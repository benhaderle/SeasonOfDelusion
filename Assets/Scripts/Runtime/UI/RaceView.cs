using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System.Linq;

/// <summary>
/// The view for the race simulation
/// </summary> 
public class RaceView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI routeText;
    [Header("Completion Bar")]
    [SerializeField] private RectTransform runCompletionBar;
    [SerializeField] private PoolContext runnerCompletionBubblePool;
    [SerializeField] private PoolContext anonymousRunnerCompletionBubblePool;
    private Dictionary<Runner, RunnerCompletionBubble> activeRunnerBubbleDictionary = new();
    [Header("Runner List")]
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    private Dictionary<Runner, RunnerRaceSimulationCard> activeRunnerCardDictionary = new();
    private IEnumerator toggleRoutine;

    #region Events
    public class PostRaceContinueButtonPressedEvent : UnityEvent<PostRaceContinueButtonPressedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static PostRaceContinueButtonPressedEvent postRaceContinueButtonPressedEvent = new ();
    #endregion

    private void Awake()
    {
        anonymousRunnerCompletionBubblePool.Initialize();
        runnerCompletionBubblePool.Initialize();
        runnerSimulationCardPool.Initialize();
        Toggle(false);
    }

    private void OnEnable()
    {
        RaceController.startRaceEvent.AddListener(OnStartRace);
        RaceController.raceSimulationUpdatedEvent.AddListener(OnRaceSimulationUpdated);
        RaceController.raceSimulationEndedEvent.AddListener(OnRaceSimulationEnded);
        RaceController.raceOpportunityStartedEvent.AddListener(OnRaceOpportunityStarted);
        RaceController.raceOpportunityEndedEvent.AddListener(OnRaceOpportunityEnded);
    }

    private void OnDisable()
    {
        RaceController.startRaceEvent.RemoveListener(OnStartRace);
        RaceController.raceSimulationUpdatedEvent.RemoveListener(OnRaceSimulationUpdated);
        RaceController.raceSimulationEndedEvent.RemoveListener(OnRaceSimulationEnded);
        RaceController.raceOpportunityStartedEvent.RemoveListener(OnRaceOpportunityStarted);
        RaceController.raceOpportunityEndedEvent.RemoveListener(OnRaceOpportunityEnded);
    }

    public void OnContinueButton()
    {
        Toggle(false);
        postRaceContinueButtonPressedEvent.Invoke(new PostRaceContinueButtonPressedEvent.Context { });
    }

    private void OnStartRace(RaceController.StartRaceEvent.Context context)
    {
        routeText.text = $"{context.raceRoute.Name} - {context.raceRoute.Length} mi";

        runnerSimulationCardParent.gameObject.SetActive(true);
        ReadOnlyCollection<Runner> playerRunners = context.teams[0].Runners;
        for (int i = 0; i < playerRunners.Count; i++)
        {
            // bubble setup
            RunnerCompletionBubble bubble = runnerCompletionBubblePool.GetPooledObject<RunnerCompletionBubble>();

            bubble.labelText.text = $"{playerRunners[i].FirstName.ToCharArray()[0]}{playerRunners[i].LastName.ToCharArray()[0]}";

            SetBubblePositionAlongBar(bubble, 0);

            activeRunnerBubbleDictionary.Add(playerRunners[i], bubble);

            // runner card setup
            RunnerRaceSimulationCard card = runnerSimulationCardPool.GetPooledObject<RunnerRaceSimulationCard>();
            card.Setup(playerRunners[i], i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
            activeRunnerCardDictionary.Add(playerRunners[i], card);
        }

        List<Runner> otherRunners = new();
        context.teams.Skip(1).ToList().ForEach(t => otherRunners.AddRange(t.Runners));
        foreach (Runner runner in otherRunners)
        {
            // bubble setup
            RunnerCompletionBubble bubble = anonymousRunnerCompletionBubblePool.GetPooledObject<RunnerCompletionBubble>();

            SetBubblePositionAlongBar(bubble, 0);

            activeRunnerBubbleDictionary.Add(runner, bubble);
        }

        Toggle(true);
    }

    private void OnRaceSimulationUpdated(RaceController.RaceSimulationUpdatedEvent.Context context)
    {
        List<Runner> orderedRunners = context.runnerStateDictionary.Keys.ToList();
        orderedRunners.Sort((r1, r2) =>
        {
            if (Mathf.Approximately(context.runnerStateDictionary[r1].totalPercentDone, context.runnerStateDictionary[r2].totalPercentDone))
            {
                if (context.runnerStateDictionary[r1].timeInSeconds == context.runnerStateDictionary[r2].timeInSeconds)
                {
                    return 0;
                } 
                
                return context.runnerStateDictionary[r1].timeInSeconds - context.runnerStateDictionary[r2].timeInSeconds >= 0 ? -1 : 1;
            }
            else
            {
                return context.runnerStateDictionary[r1].totalPercentDone - context.runnerStateDictionary[r2].totalPercentDone <= 0 ? -1 : 1;
            }
        });

        int cardIndex = 0;
        for (int i = 0; i < orderedRunners.Count; i++)
        {
            RunnerState state = context.runnerStateDictionary[orderedRunners[i]];

            RunnerCompletionBubble bubble = activeRunnerBubbleDictionary[orderedRunners[i]];
            SetBubblePositionAlongBar(bubble, state.totalPercentDone);
            bubble.transform.SetSiblingIndex(i);

            if (activeRunnerCardDictionary.TryGetValue(orderedRunners[i], out RunnerRaceSimulationCard card))
            {
                card.UpdatePace(state);
                card.UpdatePlace(orderedRunners.Count - i);
                card.UpdateListPosition(activeRunnerCardDictionary.Count - 1 - cardIndex, cardIndex % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
                cardIndex++;
            }
        }
    }

    private void OnRaceSimulationEnded(RaceController.RaceSimulationEndedEvent.Context context)
    {
        Toggle(false);
    }

    private void OnRaceOpportunityStarted(RaceController.RaceOpportunityStartedEvent.Context context)
    {
        //toggle off, but don't clean up the view
        Toggle(false, false);
    }

    private void OnRaceOpportunityEnded(RaceController.RaceOpportunityEndedEvent.Context context)
    {
        Toggle(true);
    }

    /// <summary>
    /// Toggles the view on and off and cleans up pools if necessary
    /// </summary>
    /// <param name="active">Whether the view should be active or not</param>
    /// <param name="cleanUp">Whether we should clean up all the pooled objects if we're turning the view inactive</param>
    private void Toggle(bool active, bool cleanUp = true)
    {
        if (active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine(cleanUp));
        }
    }

    private IEnumerator ToggleOffRoutine(bool cleanUp)
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);

        if (cleanUp)
        {
            runnerCompletionBubblePool.ReturnAllToPool();
            anonymousRunnerCompletionBubblePool.ReturnAllToPool();
            runnerSimulationCardPool.ReturnAllToPool();
            activeRunnerBubbleDictionary.Clear();
            activeRunnerCardDictionary.Clear();
            runnerSimulationCardParent.gameObject.SetActive(false);
        }
    }

    private void SetBubblePositionAlongBar(RunnerCompletionBubble bubble, float completion)
    {
        float bounds = runCompletionBar.rect.height * .5f;
        bubble.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Mathf.Lerp(-bounds, bounds, completion));
    }
}
