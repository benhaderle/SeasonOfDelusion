using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System.Linq;

/// <summary>
/// The view for the workout simulation
/// </summary> 
public class WorkoutView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI workoutText;
    [Header("Completion Bar")]
    [SerializeField] private RectTransform runCompletionBar;
    [SerializeField] private PoolContext runnerCompletionBubblePool;
    private List<RunnerCompletionBubble> activeGroupBubbleDictionary = new();
    [Header("Runner List")]
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    private Dictionary<Runner, RunnerSimulationCard> activeRunnerCardDictionary = new();

    [SerializeField] private CanvasGroup continueButtonContainer;

    private IEnumerator toggleRoutine;
    private IEnumerator continueButtonToggleRoutine;

    #region Events
    public class PostWorkoutContinueButtonPressedEvent : UnityEvent<PostWorkoutContinueButtonPressedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static PostWorkoutContinueButtonPressedEvent postWorkoutContinueButtonPressedEvent = new ();
    #endregion

    private void Awake()
    {
        runnerCompletionBubblePool.Initialize();
        runnerSimulationCardPool.Initialize();
        Toggle(false);
    }


    private void OnEnable()
    {
        WorkoutController.startWorkoutEvent.AddListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.AddListener(OnRunSimulationUpdated);
        WorkoutController.workoutSimulationEndedEvent.AddListener(OnRunSimulationEnded);
    }

    private void OnDisable()
    {
        WorkoutController.startWorkoutEvent.RemoveListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.RemoveListener(OnRunSimulationUpdated);
        WorkoutController.workoutSimulationEndedEvent.RemoveListener(OnRunSimulationEnded);
    }


    private void OnStartWorkout(WorkoutController.StartWorkoutEvent.Context context)
    {
        Toggle(true);

        workoutText.text = context.workout.Name;

        runnerSimulationCardParent.gameObject.SetActive(true);
        int runnerCount = 0;
        for (int i = 0; i < context.groups.Count; i++)
        {
            // bubble setup
            RunnerCompletionBubble bubble = runnerCompletionBubblePool.GetPooledObject<RunnerCompletionBubble>();

            bubble.labelText.text = $"{i+1}";

            SetBubblePositionAlongBar(bubble, 0f);

            activeGroupBubbleDictionary.Add(bubble);

            for (int j = 0; j < context.groups[i].runners.Length; j++)
            {
                // runner card setup
                RunnerSimulationCard card = runnerSimulationCardPool.GetPooledObject<RunnerSimulationCard>();
                card.Setup(context.groups[i].runners[j], i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
                card.transform.SetSiblingIndex(runnerCount);
                activeRunnerCardDictionary.Add(context.groups[i].runners[j], card);
                runnerCount++;
            }
        }
    }

    private void OnRunSimulationUpdated(WorkoutController.WorkoutSimulationUpdatedEvent.Context context)
    {
        RunnerCompletionBubble bubble = activeGroupBubbleDictionary[context.groupIndex];
        float percentDone = context.runnerStateDictionary.Values.Min(state => state.percentDone);
        SetBubblePositionAlongBar(bubble, percentDone);

        List<Runner> orderedRunners = context.runnerStateDictionary.Keys.ToList();
        orderedRunners.Sort((r1, r2) =>
        {
            if (Mathf.Approximately(context.runnerStateDictionary[r1].percentDone, context.runnerStateDictionary[r2].percentDone))
            { 
                return context.runnerStateDictionary[r1].timeInSeconds - context.runnerStateDictionary[r2].timeInSeconds >= 0 ? -1 : 1;
            }
            else
            {
                return context.runnerStateDictionary[r1].percentDone - context.runnerStateDictionary[r2].percentDone >= 0 ? -1 : 1;
            }
        });


        int baseSiblingIndexForRunnersInGroup = orderedRunners.Select(runner => activeRunnerCardDictionary[runner]).Max(card => card.transform.GetSiblingIndex());
        for (int i = 0; i < orderedRunners.Count; i++)
        {
            RunnerState state = context.runnerStateDictionary[orderedRunners[i]];

            RunnerSimulationCard card = activeRunnerCardDictionary[orderedRunners[i]];
            card.UpdatePace(state);
            card.UpdateListPosition(orderedRunners.Count - 1 - (baseSiblingIndexForRunnersInGroup - i), context.groupIndex % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
        }
    }

    private void OnRunSimulationEnded(WorkoutController.WorkoutSimulationEndedEvent.Context context)
    {
        foreach(KeyValuePair<Runner, RunnerUpdateRecord> kvp in context.runnerUpdateDictionary)
        {
            activeRunnerCardDictionary[kvp.Key].ShowPostRunUpdate(kvp.Key, kvp.Value);
        }


        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }
    
    public void OnContinueButton()
    {
        postWorkoutContinueButtonPressedEvent.Invoke(new PostWorkoutContinueButtonPressedEvent.Context { });
    }

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

        runnerCompletionBubblePool.ReturnAllToPool();
        runnerSimulationCardPool.ReturnAllToPool();
        activeGroupBubbleDictionary.Clear();
        activeRunnerCardDictionary.Clear();
        runnerSimulationCardParent.gameObject.SetActive(false);
    }

    private void SetBubblePositionAlongBar(RunnerCompletionBubble bubble, float completion)
    {
        float bounds = runCompletionBar.rect.height * .5f;
        bubble.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Mathf.Lerp(-bounds, bounds, completion));
    }
}
