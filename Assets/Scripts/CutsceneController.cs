using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public enum CutsceneID { Intro = 0, Preday = 1 }

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

    #if UNITY_EDITOR
    private void Update()
    {
        if(director.state == PlayState.Playing && Input.GetKeyDown(KeyCode.Space))
        {
            director.time = director.duration;
            cutsceneEndedEvent.Invoke(new CutsceneEndedEvent.Context { cutsceneID = cutsceneID });
        }
    }
    #endif

    private void OnDirectorStopped(PlayableDirector director)
    {
        cutsceneEndedEvent.Invoke(new CutsceneEndedEvent.Context { cutsceneID = cutsceneID });
    }
}
