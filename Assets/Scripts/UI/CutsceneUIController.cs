using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using CreateNeptune;

public class CutsceneUIController : MonoBehaviour
{
    [SerializeField] private Cutscene[] cutscenes;
    private Dictionary<CutsceneID, Cutscene> cutsceneDictionary;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    public class StartCutsceneEvent : UnityEvent<StartCutsceneEvent.Context> 
    {
        public class Context
        {
            public CutsceneID cutsceneID;
        }
    };
    public static StartCutsceneEvent startCutsceneEvent = new StartCutsceneEvent();
    #endregion
    private void Awake()
    {
        cutsceneDictionary = new Dictionary<CutsceneID, Cutscene>();
        for(int i = 0; i < cutscenes.Length; i++)
        {
            cutsceneDictionary.TryAdd(cutscenes[i].id, cutscenes[i]);
        }
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        startCutsceneEvent.AddListener(OnStartCutscene);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        startCutsceneEvent.RemoveListener(OnStartCutscene);
    }

    private void OnToggle(bool active)
    {
        if(active)
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
    }

    private void OnStartCutscene(StartCutsceneEvent.Context context)
    {
        if(cutsceneDictionary.TryGetValue(context.cutsceneID, out Cutscene cutscene))
        {
            SceneManager.LoadSceneAsync((int)cutscene.scene, LoadSceneMode.Additive);
        }
    }

    [Serializable]
    private class Cutscene
    {
        public CutsceneID id;
        public Scene scene;
    }
}
