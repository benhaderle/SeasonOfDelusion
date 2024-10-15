using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneUIController : MonoBehaviour
{
    [SerializeField] private Cutscene[] cutscenes;
    private Dictionary<CutsceneID, Cutscene> cutsceneDictionary;
    
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
        CutsceneController.cutsceneEndedEvent.AddListener(OnCutsceneEnded);
    }

    private void OnDisable()
    {
        CutsceneController.cutsceneEndedEvent.RemoveListener(OnCutsceneEnded);
    }

    private void Start()
    {
        if (SimulationModel.Instance.Day == 0)
        {
            LoadCutscene(CutsceneID.Intro);
        }
    }

    private void OnCutsceneEnded(CutsceneController.CutsceneEndedEvent.Context context)
    {
        BackgroundController.toggleEvent.Invoke(true);
        HeaderController.toggleEvent.Invoke(true);
        
        switch(context.cutsceneID)
        {
            case CutsceneID.Intro:
                DialogueUIController.toggleEvent.Invoke(true);
                DialogueUIController.startDialgoueEvent.Invoke(new DialogueUIController.StartDialgoueEvent.Context { dialogueID = DialogueID.Intro });
                break;
        }
    }

    private void LoadCutscene(CutsceneID cutsceneID)
    {
        if(cutsceneDictionary.TryGetValue(cutsceneID, out Cutscene cutscene))
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
