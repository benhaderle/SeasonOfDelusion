using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using System.Linq;

public class WorkoutSelectionUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private Workout[] workouts;
    private Workout[] todaysWorkouts;
    private Workout selectedWorkout;

    [Header("Master UI")]   
     [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform workoutSelectionContainer;
    [SerializeField] private RectTransform groupingContainer;
    [SerializeField] private WorkoutSelectionButton[] workoutSelectionButtons;
    private enum State { WorkoutSelection = 0, Grouping = 1 };
    private State currentState;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void Awake()
    {
        currentState = State.WorkoutSelection;
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
        if(active)
        {switch (currentState)
            {
                case State.WorkoutSelection: SetUpWorkoutSelection(); break;
                case State.Grouping: SetUpGrouping(); break;
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
    }

    public void OnRosterButton()
    {
        OnToggle(false);
        CutsceneUIController.toggleEvent.Invoke(false);
        RosterUIController.toggleEvent.Invoke(true);
    }    

    private void OnWorkoutSelectionButton(Workout workout)
    {
        switch(currentState)
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
    
    private void SetUpWorkoutSelection()
    {
        workoutSelectionContainer.gameObject.SetActive(true);
        groupingContainer.gameObject.SetActive(false);

        // set up today's workouts if necessary
        // TODO: right now this is totally random but should be done with some sense later
        if(todaysWorkouts == null)
        {
            todaysWorkouts = new Workout[3];
            for(int i = 0; i < todaysWorkouts.Length; i++)
            {
                Workout w;
                do
                {
                    w = workouts[Random.Range(0, workouts.Length)];
                } 
                while(todaysWorkouts.Contains(w));

                todaysWorkouts[i] = w;
            }
        }

        // set up each of the workout selection buttons with today's workouts
        for(int i = 0; i < workoutSelectionButtons.Length; i++)
        {
            Workout w = todaysWorkouts[i];
            workoutSelectionButtons[i].Setup(w);
            workoutSelectionButtons[i].Button.onClick.RemoveAllListeners();
            workoutSelectionButtons[i].Button.onClick.AddListener(() => OnWorkoutSelectionButton(w));
        }
    }

    private void SetUpGrouping()
    {
        workoutSelectionContainer.gameObject.SetActive(false);
        groupingContainer.gameObject.SetActive(true);
    }
}
