using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
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
    [Header("Map View")]
    [SerializeField] private RawImage mapViewImage;
    [Header("Runner List")]
    [SerializeField] private CanvasGroup runnerListCanvasGroup;
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    private Dictionary<Runner, RunnerSimulationCard> activeRunnerCardDictionary = new();
    [Header("Workout Summary")]
    [SerializeField] private CanvasGroup workoutSummaryCanvasGroup;
    [SerializeField] private WorkoutSummaryGroup[] workoutSummaryGroups;
    [SerializeField] private float levelUpAnimationSpeed = 1;
    [Header("Continue Button")]
    [SerializeField] private CanvasGroup continueButtonContainer;
    [SerializeField] private Button continueButton;

    private IEnumerator toggleRoutine;
    private IEnumerator continueButtonToggleRoutine;
    private IEnumerator levelUpRoutine;

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
        runnerSimulationCardPool.Initialize();
        Toggle(false);
    }

    private void OnEnable()
    {
        WorkoutController.startWorkoutEvent.AddListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.AddListener(OnWorkoutSimulationUpdated);
        WorkoutController.workoutSimulationEndedEvent.AddListener(OnWorkoutSimulationEnded);
    }

    private void OnDisable()
    {
        WorkoutController.startWorkoutEvent.RemoveListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.RemoveListener(OnWorkoutSimulationUpdated);
        WorkoutController.workoutSimulationEndedEvent.RemoveListener(OnWorkoutSimulationEnded);
    }

    private void OnStartWorkout(WorkoutController.StartWorkoutEvent.Context context)
    {
        Toggle(true);

        workoutSummaryCanvasGroup.alpha = 0;
        workoutSummaryCanvasGroup.gameObject.SetActive(false);

        runnerListCanvasGroup.alpha = 1;
        runnerListCanvasGroup.gameObject.SetActive(true);

        workoutText.text = context.workout.DisplayName;

        runnerSimulationCardParent.gameObject.SetActive(true);
        int runnerCount = 0;
        for (int i = 0; i < context.groups.Count; i++)
        {
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

    private void OnWorkoutSimulationUpdated(WorkoutController.WorkoutSimulationUpdatedEvent.Context context)
    {
        List<Runner> orderedRunners = context.runnerStateDictionary.Keys.ToList();
        orderedRunners.Sort((r1, r2) =>
        {
            if (Mathf.Approximately(context.runnerStateDictionary[r1].totalPercentDone, context.runnerStateDictionary[r2].totalPercentDone))
            {
                return context.runnerStateDictionary[r1].timeInSeconds - context.runnerStateDictionary[r2].timeInSeconds >= 0 ? -1 : 1;
            }
            else
            {
                return context.runnerStateDictionary[r1].totalPercentDone - context.runnerStateDictionary[r2].totalPercentDone >= 0 ? -1 : 1;
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

    private void OnWorkoutSimulationEnded(WorkoutController.WorkoutSimulationEndedEvent.Context context)
    {
        runnerListCanvasGroup.alpha = 0;
        runnerListCanvasGroup.gameObject.SetActive(false);

        workoutSummaryCanvasGroup.alpha = 1;
        workoutSummaryCanvasGroup.gameObject.SetActive(true);

        for (int i = 0; i < Mathf.Min(context.groups.Count, workoutSummaryGroups.Length); i++)
        {
            workoutSummaryGroups[i].Setup(context.groups[i], context.runnerUpdateDictionary.Where(kvp => context.groups[i].runners.Contains(kvp.Key)).ToList());
        }

        for (int i = context.groups.Count; i < workoutSummaryGroups.Length; i++)
        {
            workoutSummaryGroups[i].gameObject.SetActive(false);
        }

        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));

        continueButton.onClick.AddListener(() => ShowLevelUpUpdate(context.runnerUpdateDictionary));
    }

    private void ShowLevelUpUpdate(System.Collections.ObjectModel.ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary)
    {
        continueButton.onClick.RemoveAllListeners();
        continueButton.enabled = false;

        runnerListCanvasGroup.alpha = 1;
        runnerListCanvasGroup.gameObject.SetActive(true);

        workoutSummaryCanvasGroup.alpha = 0;
        workoutSummaryCanvasGroup.gameObject.SetActive(false);


        float countUpTime = 0;
        foreach(KeyValuePair<Runner, RunnerUpdateRecord> kvp in runnerUpdateDictionary)
        {
            countUpTime = Mathf.Max(countUpTime, kvp.Value.experienceChange * levelUpAnimationSpeed);
            activeRunnerCardDictionary[kvp.Key].ShowPostRunUpdate(kvp.Key, kvp.Value);
        }

        CNExtensions.SafeStartCoroutine(this, ref levelUpRoutine, LevelUpModalRoutine(runnerUpdateDictionary, countUpTime + .5f));
    }

    private IEnumerator LevelUpModalRoutine(System.Collections.ObjectModel.ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary, float waitTime)
    {
        List<KeyValuePair<Runner, RunnerUpdateRecord>> leveledUpRunners = runnerUpdateDictionary.Where(kvp => kvp.Value.levelUpRecords.Count > 0).ToList();

        if (leveledUpRunners.Count > 0)
        {
            yield return new WaitForSeconds(waitTime);

            LevelUpModalController.levelUpEvent.Invoke(new LevelUpModalController.LevelUpEvent.Context
            {
                runnerUpdateRecords = leveledUpRunners
            });
        }

        continueButton.onClick.AddListener(() =>
        {
            Toggle(false);
            postWorkoutContinueButtonPressedEvent.Invoke(new PostWorkoutContinueButtonPressedEvent.Context { });
        });
        continueButton.enabled = true;
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

        runnerSimulationCardPool.ReturnAllToPool();
        activeRunnerCardDictionary.Clear();
        runnerSimulationCardParent.gameObject.SetActive(false);
    }
}
