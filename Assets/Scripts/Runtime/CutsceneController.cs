using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

// every playable cutscene should have an entry in this enum 
public enum CutsceneID { Intro = 0, Preday = 1 }

public class CutsceneController : MonoBehaviour
{
    /// <summary>
    /// The id for this CutsceneController
    /// </summary>
    [SerializeField] private CutsceneID cutsceneID;
    /// <summary>
    /// The director controlling this cutscene
    /// </summary>
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
        // if we're in the editor, this listens for the space bar to skip to the end of a cutscene
        if(director.state == PlayState.Playing && Input.GetKeyDown(KeyCode.Space))
        {
            director.time = director.duration;
        }
    }
#endif

    /// <summary>
    /// Called when the director reaches the end of the sequence. Sends an event to let everything else know it's done
    /// </summary>
    /// <param name="director">The stopped director</param> 
    private void OnDirectorStopped(PlayableDirector director)
    {
        cutsceneEndedEvent.Invoke(new CutsceneEndedEvent.Context { cutsceneID = cutsceneID });
    }
}
