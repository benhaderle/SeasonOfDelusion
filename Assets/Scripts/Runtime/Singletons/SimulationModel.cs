using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Model for the day to day simulation. (Not the run simulation)
/// </summary>
public class SimulationModel : Singleton<SimulationModel>
{
    [SerializeField] private TextAsset daysAsset;
    private Day[] days;
    private bool loaded;
    [SerializeField] private SimulationSaveDataSO simulationSaveData;
    public int dayIndex
    {
        get => simulationSaveData.data.dayIndex;
        private set => simulationSaveData.data.dayIndex = value;
    }

    public int eventIndex
    {
        get => simulationSaveData.data.eventIndex;
        private set => simulationSaveData.data.eventIndex = value;
    }

    #region Events
    public class DayEventLoadedEvent : UnityEvent<DayEventLoadedEvent.Context>
    {
        public class Context
        {
            public string date;
            public string time;
        }
    };
    public static DayEventLoadedEvent dayEventLoadedEvent = new();
    public class EndDayEvent : UnityEvent<EndDayEvent.Context>
    {
        public class Context
        {
        }
    };
    public static EndDayEvent endDayEvent = new();
    #endregion

    protected override void OnSuccessfulAwake()
    {
        days = JsonUtility.FromJson<DaySerializationContainer>(daysAsset.text).days;
        loaded = false;
    }

    private void OnEnable()
    {
        SaveDataLoadedEvent.Instance.AddListener(OnSaveDataLoaded);
        CutsceneController.cutsceneEndedEvent.AddListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
        RunView.postRunContinueButtonPressedEvent.AddListener(OnPostRunContinueButtonPressed);
        WorkoutView.postWorkoutContinueButtonPressedEvent.AddListener(OnPostWorkoutContinueButtonPressed);
        RaceResultsView.postRaceContinueButtonPressedEvent.AddListener(OnPostRaceContinueButtonPressed);
    }

    private void OnDisable()
    {
        SaveDataLoadedEvent.Instance.RemoveListener(OnSaveDataLoaded);
        CutsceneController.cutsceneEndedEvent.RemoveListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.RemoveListener(OnDialogueEnded);
        RunView.postRunContinueButtonPressedEvent.RemoveListener(OnPostRunContinueButtonPressed);
        WorkoutView.postWorkoutContinueButtonPressedEvent.RemoveListener(OnPostWorkoutContinueButtonPressed);
        RaceResultsView.postRaceContinueButtonPressedEvent.RemoveListener(OnPostRaceContinueButtonPressed);
    }

    private void Start()
    {
        if (!loaded && SaveData.Instance.loaded)
        {
            OnSaveDataLoaded();
        }
    }

    private void OnSaveDataLoaded()
    {
        loaded = true;

        StartDay(dayIndex);

        SaveDataLoadedEvent.Instance.RemoveListener(OnSaveDataLoaded);
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
    {
        StartDay(dayIndex);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnCutsceneEnded(CutsceneController.CutsceneEndedEvent.Context context)
    {
        BackgroundController.toggleEvent.Invoke(true);
        HeaderController.toggleEvent.Invoke(true);

        LoadNextEventOrAdvanceDay();
    }

    private void OnDialogueEnded(DialogueUIController.DialogueEndedEvent.Context context)
    {
        if(days[dayIndex].events[eventIndex].type != "Dialogue")
        {
            return;
        }
        LoadNextEventOrAdvanceDay();
    }

    private void OnPostRunContinueButtonPressed(RunView.PostRunContinueButtonPressedEvent.Context context)
    {
        LoadNextEventOrAdvanceDay();
    }

    private void OnPostWorkoutContinueButtonPressed(WorkoutView.PostWorkoutContinueButtonPressedEvent.Context context)
    {
        LoadNextEventOrAdvanceDay();
    }

    private void OnPostRaceContinueButtonPressed(RaceResultsView.PostRaceContinueButtonPressedEvent.Context context)
    {
        LoadNextEventOrAdvanceDay();
    }

    private void StartDay(int index)
    {
        dayIndex = index;
        eventIndex = 0;

        LoadEvent(days[dayIndex], eventIndex);
    }

    private void LoadNextEventOrAdvanceDay()
    {
        eventIndex++;

        if (eventIndex < days[dayIndex].events.Count)
        {
            LoadEvent(days[dayIndex], eventIndex);
        }
        else
        {
            AdvanceDay();
        }
    }

    private void LoadEvent(Day day, int eventIndex)
    {
        DayEvent dayEvent = day.events[eventIndex];

        switch (dayEvent.type)
        {
            default:
                Debug.LogError($"UNrecognized event type \"{dayEvent.type}\". Skipping to the next event.");
                LoadNextEventOrAdvanceDay();
                break;
            case "Cutscene": LoadCutsceneEvent(dayEvent); break;
            case "Dialogue": LoadDialogueEvent(dayEvent); break;
            case "Practice": LoadPracticeEvent(dayEvent); break;
            case "Workout": LoadWorkoutEvent(dayEvent); break;
            case "Race": LoadRaceEvent(dayEvent); break;
        }

        dayEventLoadedEvent.Invoke(new DayEventLoadedEvent.Context
        {
            date = GetDate(),
            time = GetTime(),
        });
    }

    private void LoadCutsceneEvent(DayEvent cutsceneEvent)
    {
        Enum.TryParse(cutsceneEvent.cutsceneID, out CutsceneID cutsceneID);

        BackgroundController.toggleEvent.Invoke(false);
        CutsceneUIController.toggleEvent.Invoke(true);
        CutsceneUIController.startCutsceneEvent.Invoke(new CutsceneUIController.StartCutsceneEvent.Context { cutsceneID = cutsceneID });
    }

    private void LoadDialogueEvent(DayEvent dialogueEvent)
    {
        DialogueUIController.startDialogueEvent.Invoke(new DialogueUIController.StartDialogueEvent.Context { dialogueID = dialogueEvent.dialogueID });
    }

    private void LoadPracticeEvent(DayEvent practiceEvent)
    {
        if (!string.IsNullOrWhiteSpace(practiceEvent.routeID))
        {
            try
            {
                Route route = RouteModel.Instance.Routes.First(r => r.DisplayName == practiceEvent.routeID);
                route.saveData.data.numTimesRun++;

                SceneManager.LoadSceneAsync((int)Scene.MapScene, LoadSceneMode.Additive);

                void OnMapSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
                {
                    if (scene.buildIndex == (int)Scene.MapScene)
                    {
                        CutsceneUIController.toggleEvent.Invoke(false);
                        RunController.startRunEvent.Invoke(new RunController.StartRunEvent.Context
                        {
                            runners = TeamModel.Instance.PlayerRunners.ToList(),
                            route = route,
                            runConditions = new RunConditions { }
                        });

                        SceneManager.sceneLoaded -= OnMapSceneLoaded;
                    }
                }

                SceneManager.sceneLoaded += OnMapSceneLoaded;

                return;
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning($"No route with name \"{practiceEvent.routeID}\" exists in RouteModel. Letting player pick route.");
            }
        }

        RouteUIController.toggleEvent.Invoke(true);
        BackgroundController.toggleEvent.Invoke(true);   
    }

    private void OnMapSceneLoaded()
    {
        
    }

    private void LoadWorkoutEvent(DayEvent workoutEvent)
    {
        WorkoutSelectionUIController.toggleEvent.Invoke(true);
        BackgroundController.toggleEvent.Invoke(true);
    }

    private void LoadRaceEvent(DayEvent raceEvent)
    {
        RaceController.startRaceEvent.Invoke(new RaceController.StartRaceEvent.Context
        {
            teams = TeamModel.Instance.GetAllTeams(),
            raceRoute = RouteModel.Instance.GetRaceRoute(raceEvent.routeID)
        });
        CutsceneUIController.toggleEvent.Invoke(false);
        BackgroundController.toggleEvent.Invoke(true);
    }

    public void AdvanceDay()
    {
        endDayEvent.Invoke(new EndDayEvent.Context());

        dayIndex++;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public string GetDialogueForCurrentEvent()
    {
        return days[dayIndex].events[eventIndex].dialogueID;
    }

    private string GetDate()
    {
        return days[dayIndex].date;
    }

    private string GetTime()
    {
        DayEvent dayEvent = days[dayIndex].events[eventIndex];
        return $"{dayEvent.timeHours % 12}:{dayEvent.timeMinutes:00} {(dayEvent.timeHours > 11 ? "PM" : "AM")}";
    }
}

[Serializable]
public class Day
{
    public string date;
    public List<DayEvent> events;
}

[Serializable]
public class DaySerializationContainer
{
    public Day[] days;
}
