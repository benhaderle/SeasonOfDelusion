using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public enum CutsceneID { Intro }

public class CutsceneController : MonoBehaviour
{
    [SerializeField] private CutsceneID cutsceneID;
    [SerializeField] private PlayableDirector director;

#region Events
    public class CutsceneEndedEvent : UnityEvent<CutsceneEndedEvent.Context> 
    {
        public class Context
        {
            public CutsceneID cutsceneID;
        }
    };
    public static CutsceneEndedEvent cutsceneEndedEvent = new CutsceneEndedEvent();
#endregion

    private void OnEnable()
    {
        director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        director.stopped -= OnDirectorStopped;
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        cutsceneEndedEvent.Invoke(new CutsceneEndedEvent.Context { cutsceneID = cutsceneID });
    }
}
