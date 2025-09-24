using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using CreateNeptune;
using System.Linq;

public class WorkoutSelectionUIController : MonoBehaviour
{
    private const int NUM_SLOTS_PER_GROUP = 3;

    private Workout[] todaysWorkouts;
    private Workout selectedWorkout;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform workoutSelectionContainer;
    [SerializeField] private RectTransform groupingContainer;
    [SerializeField] private PoolContext workoutSelectionButtonPoolContext;
    [SerializeField] private WorkoutGroupRow[] workoutGroupRows;
    [SerializeField] private PoolContext workoutRunnerCardPoolContext;

    private enum State { WorkoutSelection = 0, Grouping = 1 };
    private State currentState;

    private WorkoutRunnerCard selectedCard;
    private int selectedGroupIndex;
    private int selectedSlotIndex;

    private IEnumerator toggleRoutine;
    private bool selectionSetup;
    private bool groupingSetup;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    public class RunnerCardSelectedEvent : UnityEvent<RunnerCardSelectedEvent.Context>
    {
        public class Context
        {
            public WorkoutRunnerCard card;
            public int groupIndex;
            public int slotIndex;
        }
    };
    public static RunnerCardSelectedEvent runnerCardSelectedEvent = new RunnerCardSelectedEvent();
    public class EmptySlotSelectedEvent : UnityEvent<EmptySlotSelectedEvent.Context>
    {
        public class Context
        {
            public int groupIndex;
            public int slotIndex;
        }
    };
    public static EmptySlotSelectedEvent emptySlotSelectedEvent = new EmptySlotSelectedEvent();
    #endregion

    private void Awake()
    {
        currentState = State.WorkoutSelection;
        workoutSelectionButtonPoolContext.Initialize();
        workoutRunnerCardPoolContext.Initialize();
        OnToggle(false);
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        runnerCardSelectedEvent.AddListener(OnRunnerCardSelected);
        emptySlotSelectedEvent.AddListener(OnEmptySlotSelected);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        runnerCardSelectedEvent.RemoveListener(OnRunnerCardSelected);
        emptySlotSelectedEvent.RemoveListener(OnEmptySlotSelected);
    }

    #region Event Listeners

    private void OnToggle(bool active)
    {
        if (active)
        {
            switch (currentState)
            {
                case State.WorkoutSelection: SetUpWorkoutSelection(); break;
                case State.Grouping: SetUpGrouping(); break;
            }
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));

            bool mapSceneLoaded = false;
            for (int j = 0; j < SceneManager.sceneCount; j++)
            {
                if (SceneManager.GetSceneAt(j).buildIndex == (int)Scene.MapScene)
                {
                    mapSceneLoaded = true;
                    break;
                }
            }

            if (!mapSceneLoaded)
            {
                SceneManager.LoadSceneAsync((int)Scene.MapScene, LoadSceneMode.Additive);
            }
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

    private void OnRunnerCardSelected(RunnerCardSelectedEvent.Context context)
    {
        if (selectedCard == null)
        {
            selectedCard = context.card;
            selectedGroupIndex = context.groupIndex;
            selectedSlotIndex = context.slotIndex;
        }
        else
        {
            if (selectedCard != context.card)
            {
                workoutGroupRows[selectedGroupIndex].RemoveCardFromSlot(selectedSlotIndex);
                workoutGroupRows[context.groupIndex].RemoveCardFromSlot(context.slotIndex);
                AddRunnerToSlot(selectedCard, context.groupIndex, context.slotIndex);
                AddRunnerToSlot(context.card, selectedGroupIndex, selectedSlotIndex);
            }

            selectedCard = null;
        }
    }

    private void OnEmptySlotSelected(EmptySlotSelectedEvent.Context context)
    {
        if (selectedCard != null)
        {
            workoutGroupRows[selectedGroupIndex].RemoveCardFromSlot(selectedSlotIndex);
            AddRunnerToSlot(selectedCard, context.groupIndex, context.slotIndex);

            selectedCard = null;
        }
    }

    #endregion

    #region Button Functions

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

    public void OnStartWorkoutButton()
    {
        OnToggle(false);
        CutsceneUIController.toggleEvent.Invoke(false);
        
        WorkoutController.startWorkoutEvent.Invoke(new WorkoutController.StartWorkoutEvent.Context {
            groups = workoutGroupRows.Select(groupRow => groupRow.GetWorkoutGroup()).ToList(),
            workout = selectedWorkout,
            runConditions = new RunConditions()
        });
    }

    private void OnWorkoutSelectionButton(Workout workout)
    {
        switch (currentState)
        {
            case State.WorkoutSelection:
                selectedWorkout = workout;
                currentState = State.Grouping;
                SetUpGrouping();
                break;
            case State.Grouping:
                selectedWorkout = null;
                currentState = State.WorkoutSelection;
                SetUpWorkoutSelection();
                break;
        }
    }
    #endregion

    #region Utility Functions
    private void SetUpWorkoutSelection()
    {
        if (selectionSetup)
            return;
        selectionSetup = true;

        workoutSelectionContainer.gameObject.SetActive(true);
        groupingContainer.gameObject.SetActive(false);

        // today's workouts are just the unlocked workouts
        // maybe in the future we will only show a subset or have sorting options at the very least
        if (todaysWorkouts == null)
        {
            todaysWorkouts = RouteModel.Instance.Workouts.Where(w => w.saveData.data.unlocked).ToArray();
        }

        // set up each of the workout selection buttons with today's workouts
        for (int i = 0; i < todaysWorkouts.Length; i++)
        {
            Workout w = todaysWorkouts[i];
            WorkoutSelectionButton button = workoutSelectionButtonPoolContext.GetPooledObject<WorkoutSelectionButton>();
            button.Setup(w);
            button.Button.onClick.RemoveAllListeners();
            button.Button.onClick.AddListener(() => OnWorkoutSelectionButton(w));
        }
    }

    private void SetUpGrouping()
    {
        if (groupingSetup)
            return;
        groupingSetup = true;

        workoutSelectionContainer.gameObject.SetActive(false);
        groupingContainer.gameObject.SetActive(true);

        for (int i = 0; i < workoutGroupRows.Length; i++)
        {
            workoutGroupRows[i].Initialize(i, selectedWorkout.GoalVO2);
        }

        int groupIndex = 0;
        int slotIndex = 0;
        for (int i = 0; i < TeamModel.Instance.PlayerRunners.Count; i++)
        {
            Runner runner = TeamModel.Instance.PlayerRunners[i];
            WorkoutRunnerCard runnerCard = workoutRunnerCardPoolContext.GetPooledObject<WorkoutRunnerCard>();

            //set up the card with the runner data
            runnerCard.Setup(runner);

            //add the card to the next available slot
            //TODO: in the future we should save the last group config or have a default suggestion
            AddRunnerToSlot(runnerCard, groupIndex, slotIndex);

            //increment the slot + group indices
            slotIndex++;
            if (slotIndex >= NUM_SLOTS_PER_GROUP)
            {
                slotIndex = 0;
                groupIndex++;
            }
        }
    }

    private void AddRunnerToSlot(WorkoutRunnerCard runnerCard, int groupIndex, int slotIndex)
    {
        workoutGroupRows[groupIndex].AddRunnerToSlot(runnerCard, slotIndex);
    }
    #endregion
}
