using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class CutsceneUIController : MonoBehaviour
{
    [SerializeField] private Cutscene[] cutscenes;
    private Dictionary<CutsceneID, Cutscene> cutsceneDictionary;
    #region Events
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
        startCutsceneEvent.AddListener(OnStartCutscene);
    }

    private void OnDisable()
    {
        startCutsceneEvent.RemoveListener(OnStartCutscene);
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
