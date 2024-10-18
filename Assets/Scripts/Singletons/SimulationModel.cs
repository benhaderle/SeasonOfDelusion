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
    private int day;
    public int Day => day;

    #region Events
    public class EndDayEvent : UnityEvent<EndDayEvent.Context> 
    { 
        public class Context
        {
        }
    };
    public static EndDayEvent endDayEvent = new ();
    #endregion

    private void OnEnable()
    {
        CutsceneController.cutsceneEndedEvent.AddListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
    }

    private void OnDisable()
    {
        CutsceneController.cutsceneEndedEvent.RemoveListener(OnCutsceneEnded);
        DialogueUIController.dialogueEndedEvent.RemoveListener(OnDialogueEnded);
    }

    private void Start()
    {
        StartDay();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode loadSceneMode)
    {
        StartDay();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnCutsceneEnded(CutsceneController.CutsceneEndedEvent.Context context)
    {
        BackgroundController.toggleEvent.Invoke(true);
        HeaderController.toggleEvent.Invoke(true);
        
        switch(context.cutsceneID)
        {
            case CutsceneID.Intro:
                DialogueUIController.toggleEvent.Invoke(true);
                DialogueUIController.startDialgoueEvent.Invoke(new DialogueUIController.StartDialogueEvent.Context { dialogueID = DialogueID.Intro });
                break;
            case CutsceneID.Preday:
                StartPractice();
                break;
        }
    }

    private void OnDialogueEnded(DialogueUIController.DialogueEndedEvent.Context context)
    {
        switch(context.dialogueID)
        {
            case DialogueID.Intro:
                StartPractice();
                break;
        }
    }

    private void StartDay()
    {
        CutsceneID cutsceneID;
        if (Day == 0)
        {
            cutsceneID = CutsceneID.Intro;
        }
        else
        {           
            cutsceneID = CutsceneID.Preday;
        }

        CutsceneUIController.startCutsceneEvent.Invoke( new CutsceneUIController.StartCutsceneEvent.Context { cutsceneID = cutsceneID });
    }

    private void StartPractice()
    {
        RouteUIController.toggleEvent.Invoke(true);
        BackgroundController.toggleEvent.Invoke(true);
    }

    public void AdvanceDay()
    {
        endDayEvent.Invoke(new EndDayEvent.Context());

        day++;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
