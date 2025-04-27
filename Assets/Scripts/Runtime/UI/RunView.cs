using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// The view for the run simulation
/// </summary> 
public class RunView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI routeText;
    [SerializeField] private TextMeshProUGUI easeText;
    [SerializeField] private TextMeshProUGUI newRouteText;
    [Header("Map View")]
    [SerializeField] private RawImage mapViewImage;
    [Header("Runner List")]
    [SerializeField] private CanvasGroup runnerListCanvasGroup;
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    private Dictionary<Runner, RunnerSimulationCard> activeRunnerCardDictionary = new();
    [SerializeField] private CanvasGroup continueButtonContainer;

    private IEnumerator toggleRoutine;
    private IEnumerator runnerListToggleRoutine;
    private IEnumerator continueButtonToggleRoutine;

    private bool newRouteUnlocked = false;

    #region Events
    public class PostRunContinueButtonPressedEvent : UnityEvent<PostRunContinueButtonPressedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static PostRunContinueButtonPressedEvent postRunContinueButtonPressedEvent = new();
    #endregion

    private void Awake()
    {
        runnerSimulationCardPool.Initialize();
        Toggle(false);
    }

    private void OnEnable()
    {
        RunController.startRunEvent.AddListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.AddListener(OnRunSimulationUpdated);
        RunController.runSimulationEndedEvent.AddListener(OnRunSimulationEnded);
        RouteModel.routeUnlockedEvent.AddListener(OnRouteUnlocked);
        DialogueUIController.startDialogueEvent.AddListener(OnStartDialogue);
        DialogueUIController.dialogueEndedEvent.AddListener(OnDialogueEnded);
    }

    private void OnDisable()
    {
        RunController.startRunEvent.RemoveListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.RemoveListener(OnRunSimulationUpdated);
        RunController.runSimulationEndedEvent.RemoveListener(OnRunSimulationEnded);
        RouteModel.routeUnlockedEvent.RemoveListener(OnRouteUnlocked);
        DialogueUIController.startDialogueEvent.RemoveListener(OnStartDialogue);
        DialogueUIController.dialogueEndedEvent.RemoveListener(OnDialogueEnded);
    }

    public void OnContinueButton()
    {
        if (!newRouteUnlocked)
        {
            Toggle(false);
            postRunContinueButtonPressedEvent.Invoke(new PostRunContinueButtonPressedEvent.Context { });
        }
        else
        {
            newRouteUnlocked = false;
            easeText.gameObject.SetActive(true);
            routeText.gameObject.SetActive(true);
            newRouteText.gameObject.SetActive(false);

            CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 1, 0, false, true, true));
            RunController.runSimulationResumeEvent.Invoke(new RunController.RunSimulationResumeEvent.Context { });
        }
    }

    private void OnStartRun(RunController.StartRunEvent.Context context)
    {
        routeText.text = $"{context.route.DisplayName} - {context.route.Length:F2} mi";

        easeText.text = "Coach says: ";
        if (context.route.Difficulty <= .7f)
        {
            easeText.text += "\"Talk to the birds\"";
        }
        else if (context.route.Difficulty <= .9f)
        {
            easeText.text += "\"Keep it honest\"";
        }
        else
        {
            easeText.text += "\"Let's get it rolling today\"";
        }

        runnerSimulationCardParent.gameObject.SetActive(true);
        for (int i = 0; i < context.runners.Count; i++)
        {
            // runner card setup
            RunnerSimulationCard card = runnerSimulationCardPool.GetPooledObject<RunnerSimulationCard>();
            card.Setup(context.runners[i], i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
            activeRunnerCardDictionary.Add(context.runners[i], card);
        }

        Toggle(true);
    }

    private void OnRunSimulationUpdated(RunController.RunSimulationUpdatedEvent.Context context)
    {
        List<Runner> orderedRunners = context.runnerStateDictionary.Keys.ToList();
        orderedRunners.Sort((r1, r2) =>
        {
            if (Mathf.Approximately(context.runnerStateDictionary[r1].percentDone, context.runnerStateDictionary[r2].percentDone))
            {
                return context.runnerStateDictionary[r1].timeInSeconds - context.runnerStateDictionary[r2].timeInSeconds >= 0 ? -1 : 1;
            }
            else
            {
                return context.runnerStateDictionary[r1].percentDone - context.runnerStateDictionary[r2].percentDone <= 0 ? -1 : 1;
            }
        });

        for (int i = 0; i < orderedRunners.Count; i++)
        {
            RunnerState state = context.runnerStateDictionary[orderedRunners[i]];

            RunnerSimulationCard card = activeRunnerCardDictionary[orderedRunners[i]];
            card.UpdatePace(state);
            card.UpdateListPosition(orderedRunners.Count - 1 - i, i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
        }
    }

    private void OnRunSimulationEnded(RunController.RunSimulationEndedEvent.Context context)
    {
        foreach (KeyValuePair<Runner, RunnerUpdateRecord> kvp in context.runnerUpdateDictionary)
        {
            activeRunnerCardDictionary[kvp.Key].ShowPostRunUpdate(kvp.Key, kvp.Value);
        }

        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }

    private void OnRouteUnlocked(RouteModel.RouteUnlockedEvent.Context context)
    {
        easeText.gameObject.SetActive(false);
        routeText.gameObject.SetActive(false);
        newRouteText.gameObject.SetActive(true);

        newRouteUnlocked = true;
        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }

    private void OnStartDialogue(DialogueUIController.StartDialogueEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref runnerListToggleRoutine, ToggleRunnerListRoutine(false));
    }

    private void OnDialogueEnded(DialogueUIController.DialogueEndedEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref runnerListToggleRoutine, ToggleRunnerListRoutine(true));
    }

    private void Toggle(bool active)
    {
        if (active)
        {
            CNExtensions.SafeStartCoroutine(this, ref runnerListToggleRoutine, ToggleRunnerListRoutine(true));
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));

            Rect mapPixelRect = RectTransformUtility.PixelAdjustRect(mapViewImage.rectTransform, canvas);
            mapPixelRect.width = mapPixelRect.width * canvas.scaleFactor;
            mapPixelRect.height = mapPixelRect.height * canvas.scaleFactor;

            Rect mapUVRect = new Rect(0, 0, mapPixelRect.width / mapViewImage.texture.width, mapPixelRect.height / mapViewImage.texture.height);
            mapUVRect.x = (1 - mapUVRect.width) / 2f;
            mapUVRect.y = (1 - mapUVRect.height) / 2f;
            mapViewImage.uvRect = mapUVRect;
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true);

        runnerSimulationCardPool.ReturnAllToPool();
        activeRunnerCardDictionary.Clear();
        runnerSimulationCardParent.gameObject.SetActive(false);

        for (int i = 0; i < SceneManager.loadedSceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).buildIndex == (int)Scene.MapScene)
            {
                SceneManager.UnloadSceneAsync((int)Scene.MapScene);
            }
        }
    }

    private IEnumerator ToggleRunnerListRoutine(bool active)
    {
        if (active)
        {
            yield return CNAction.FadeObject(runnerListCanvasGroup, GameManager.Instance.DefaultUIAnimationTime, runnerListCanvasGroup.alpha, 1, CNEase.EaseType.Linear, false, false, true);
        }
        else
        {
            yield return CNAction.FadeObject(runnerListCanvasGroup, GameManager.Instance.DefaultUIAnimationTime, runnerListCanvasGroup.alpha, 0, CNEase.EaseType.Linear, false, false, true);
        }
    }
}
