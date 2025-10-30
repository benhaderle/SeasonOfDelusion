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
    [SerializeField] private float timeOnView;
    private float lastViewSwitchTime;
    [SerializeField] private RawImage mapViewImage;
    [SerializeField] private ElevationGraphView elevationGraphView;
    [Header("Runner List")]
    [SerializeField] private CanvasGroup runnerListCanvasGroup;
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    [SerializeField] private float statusTextUpdateTime = 1f;
    private float statusTextUpdateTimer;
    [SerializeField] private float levelUpAnimationSpeed = 1;
    private Dictionary<Runner, RunnerSimulationCard> activeRunnerCardDictionary = new();
    [SerializeField] private CanvasGroup continueButtonContainer;

    private IEnumerator toggleRoutine;
    private IEnumerator runnerListToggleRoutine;
    private IEnumerator continueButtonToggleRoutine;
    private IEnumerator levelUpRoutine;

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
        if (context.route.Length <= 6f)
        {
            easeText.text += "\"Talk to the birds\"";
        }
        else if (context.route.Length <= 10)
        {
            easeText.text += "\"Keep it honest\"";
        }
        else
        {
            easeText.text += "\"Let's get it rolling today\"";
        }

        mapViewImage.gameObject.SetActive(true);
        elevationGraphView.SetElevationLine(context.route.lineData.ElevationCurve);
        elevationGraphView.InitializeRunnerMarkers(context.runners);
        elevationGraphView.gameObject.SetActive(false);

        lastViewSwitchTime = Time.time;

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
        // switch the view between the map and the elevation graph every once in a while
        if (lastViewSwitchTime > 0)
        {
            lastViewSwitchTime -= Time.deltaTime;
        }
        else
        {
            lastViewSwitchTime = timeOnView;

            mapViewImage.gameObject.SetActive(!mapViewImage.isActiveAndEnabled);
            elevationGraphView.gameObject.SetActive(!elevationGraphView.isActiveAndEnabled);
        }

        List<Runner> orderedRunners = context.runnerStateDictionary.Keys.ToList();
        orderedRunners.Sort((r1, r2) =>
        {
            if (Mathf.Approximately(context.runnerStateDictionary[r1].totalPercentDone, context.runnerStateDictionary[r2].totalPercentDone))
            {
                return context.runnerStateDictionary[r1].timeInSeconds - context.runnerStateDictionary[r2].timeInSeconds >= 0 ? -1 : 1;
            }
            else
            {
                return context.runnerStateDictionary[r1].totalPercentDone - context.runnerStateDictionary[r2].totalPercentDone <= 0 ? -1 : 1;
            }
        });

        bool updateStatusText = Time.time - statusTextUpdateTimer > statusTextUpdateTime;
        if (updateStatusText)
        {
            statusTextUpdateTimer = Time.time;
        }

        for (int i = 0; i < orderedRunners.Count; i++)
            {
                RunnerState state = context.runnerStateDictionary[orderedRunners[i]];

                RunnerSimulationCard card = activeRunnerCardDictionary[orderedRunners[i]];
                card.UpdatePace(state);
                card.UpdateListPosition(orderedRunners.Count - 1 - i, i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);

                if (updateStatusText)
                {
                    card.UpdateStatusText(orderedRunners[i], state);
                }
            }
        
        elevationGraphView.UpdateRunners(context.runnerStateDictionary);
    }

    private void OnRunSimulationEnded(RunController.RunSimulationEndedEvent.Context context)
    {
        float countUpTime = 0;
        foreach (KeyValuePair<Runner, RunnerUpdateRecord> kvp in context.runnerUpdateDictionary)
        {
            countUpTime = Mathf.Max(countUpTime, kvp.Value.experienceChange * levelUpAnimationSpeed);
            activeRunnerCardDictionary[kvp.Key].ShowPostRunUpdate(kvp.Key, kvp.Value, levelUpAnimationSpeed);
        }

        CNExtensions.SafeStartCoroutine(this, ref levelUpRoutine, LevelUpModalRoutine(context.runnerUpdateDictionary, countUpTime + .5f));
    }

    private IEnumerator LevelUpModalRoutine(System.Collections.ObjectModel.ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary, float waitTime)
    {
        List<KeyValuePair<Runner, RunnerUpdateRecord>> leveledUpRunners = runnerUpdateDictionary.Where(kvp => kvp.Value.levelUpRecords.Count > 0).ToList();

        if (leveledUpRunners.Count > 0)
        {
            yield return new WaitForSeconds(waitTime);

            LevelUpModalController.levelUpEvent.Invoke(new LevelUpModalController.LevelUpEvent.Context
            {
                runnerUpdateRecords = leveledUpRunners
            });
        }

        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }

    private void OnRouteUnlocked(RouteModel.RouteUnlockedEvent.Context context)
    {
        easeText.gameObject.SetActive(false);
        routeText.gameObject.SetActive(false);
        newRouteText.gameObject.SetActive(true);

        mapViewImage.gameObject.SetActive(true);
        elevationGraphView.gameObject.SetActive(false);
        lastViewSwitchTime = timeOnView;

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

            MapCameraController.changeMapRenderResolutionEvent.Invoke(new MapCameraController.ChangeMapRenderResolutionEvent.Context
            {
                width = (int)mapPixelRect.width,
                height = (int)mapPixelRect.height
            });
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
        elevationGraphView.CleanUp();

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
