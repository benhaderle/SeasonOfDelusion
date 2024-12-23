using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEngine.SceneManagement;

/// <summary>
/// Model for the day to day simulation. (Not the run simulation)
/// </summary>
public class SimulationModel : Singleton<SimulationModel>
{
    [SerializeField] private TextAsset daysAsset;
    private Day[] days;

    private int dayIndex;
    public int DayIndex => dayIndex;

    private int eventIndex;

    #region Events
    public class EndDayEvent : UnityEvent<EndDayEvent.Context> 
    { 
        public class Context
        {
        }
    };
    public static EndDayEvent endDayEvent = new ();
    #endregion

    private void Awake()
    {
        days = JsonUtility.FromJson<DaySerializationContainer>(daysAsset.text).days;
    }

    private void OnEnable()
    {
        CutsceneController.cutsceneEndedEvent.AddListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
        RunController.practiceEndedEvent.AddListener(OnPracticeEnded);
    }

    private void OnDisable()
    {
        CutsceneController.cutsceneEndedEvent.RemoveListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.RemoveListener(OnDialogueEnded);
        RunController.practiceEndedEvent.RemoveListener(OnPracticeEnded);
    }

    private void Start()
    {
        StartDay(dayIndex);
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
        LoadNextEventOrAdvanceDay();
    }

    private void OnPracticeEnded(RunController.PracticeEndedEvent.Context context)
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

        if(eventIndex < days[dayIndex].events.Count)
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

        switch(dayEvent.type)
        {
            default:
                Debug.LogError($"UNrecognized event type \"{dayEvent.type}\". Skipping to the next event.");
                LoadNextEventOrAdvanceDay();
                break;
            case "Cutscene": LoadCutsceneEvent(dayEvent); break;
            case "Dialogue": LoadDialogueEvent(dayEvent); break;
            case "Practice": LoadPracticeEvent(dayEvent); break;
        }
    }

    private void LoadCutsceneEvent(DayEvent cutsceneEvent)
    {
        Enum.TryParse(cutsceneEvent.cutsceneID, out CutsceneID cutsceneID);

        BackgroundController.toggleEvent.Invoke(false);
        CutsceneUIController.startCutsceneEvent.Invoke( new CutsceneUIController.StartCutsceneEvent.Context { cutsceneID = cutsceneID });
    }

    private void LoadDialogueEvent(DayEvent dialogueEvent)
    {
        Enum.TryParse(dialogueEvent.dialogueID, out DialogueID dialogueID);

        DialogueUIController.toggleEvent.Invoke(true);
        DialogueUIController.startDialgoueEvent.Invoke(new DialogueUIController.StartDialogueEvent.Context { dialogueID = dialogueID });
    }

    private void LoadPracticeEvent(DayEvent practiceEvent)
    {
        RouteUIController.toggleEvent.Invoke(true);
        BackgroundController.toggleEvent.Invoke(true);
    }

    public void AdvanceDay()
    {
        endDayEvent.Invoke(new EndDayEvent.Context());

        dayIndex++;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
