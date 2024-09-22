using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;
using TMPro;
using System.Linq;

/// <summary>
/// The view for the run simulation
/// </summary> 
public class RunView : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [Header("Info Panel")]
    [SerializeField] private TextMeshProUGUI routeText;
    [SerializeField] private TextMeshProUGUI easeText;
    [Header("Completion Bar")]
    [SerializeField] private RectTransform runCompletionBar;
    [SerializeField] private PoolContext runnerCompletionBubblePool;
    private Dictionary<Runner, RunnerCompletionBubble> activeRunnerBubbleDictionary = new();
    [Header("Runner List")]
    [SerializeField] private PoolContext runnerSimulationCardPool;
    [SerializeField] private RectTransform runnerSimulationCardParent;
    [SerializeField] private Color lightBackgroundColor;
    [SerializeField] private Color darkBackgroundColor;
    private Dictionary<Runner, RunnerSimulationCard> activeRunnerCardDictionary = new();

    [SerializeField] private CanvasGroup continueButtonContainer;

    private IEnumerator toggleRoutine;
    private IEnumerator continueButtonToggleRoutine;

    private void Awake()
    {
        runnerCompletionBubblePool.Initialize();
        runnerSimulationCardPool.Initialize();
        Toggle(false);
    }

    private void OnEnable()
    {
        RunController.startRunEvent.AddListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.AddListener(OnRunSimulationUpdated);
        RunController.runSimulationEndedEvent.AddListener(OnRunSimulationEnded);
    }

    private void OnDisable()
    {
        RunController.startRunEvent.RemoveListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.RemoveListener(OnRunSimulationUpdated);
        RunController.runSimulationEndedEvent.RemoveListener(OnRunSimulationEnded);
    }

    public void OnContinueButton()
    {
        SimulationModel.Instance.AdvanceDay();
    }

    private void OnStartRun(RunController.StartRunEvent.Context context)
    {
        routeText.text = $"{context.route.Name} - {context.route.Length} mi";

        easeText.text = "Coach says: ";
        if(context.runConditions.coachVO2Guidance <= .7f)
        {
            easeText.text += "\"Talk to the birds\"";
        }
        else if(context.runConditions.coachVO2Guidance <= .9f)
        {
            easeText.text += "\"Keep it honest\"";
        }
        else 
        {
            easeText.text += "\"Let's get it rolling today\"";
        }

        runnerSimulationCardParent.gameObject.SetActive(true);
        for(int i = 0; i < context.runners.Count; i++)
        {   
            // bubble setup
            RunnerCompletionBubble bubble = runnerCompletionBubblePool.GetPooledObject<RunnerCompletionBubble>();

            bubble.labelText.text = $"{context.runners[i].FirstName.ToCharArray()[0]}{context.runners[i].LastName.ToCharArray()[0]}";

            SetBubblePositionAlongBar(bubble, 0);

            activeRunnerBubbleDictionary.Add(context.runners[i], bubble);

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

        for(int i = 0; i < orderedRunners.Count; i++)
        {
            RunnerState state = context.runnerStateDictionary[orderedRunners[i]];

            RunnerCompletionBubble bubble = activeRunnerBubbleDictionary[orderedRunners[i]];
            SetBubblePositionAlongBar(bubble, state.percentDone);
            bubble.transform.SetSiblingIndex(i);

            RunnerSimulationCard card = activeRunnerCardDictionary[orderedRunners[i]];
            card.UpdatePace(state);
            card.UpdateListPosition(orderedRunners.Count - 1 - i, i % 2 == 0 ? lightBackgroundColor : darkBackgroundColor);
        }
    }

    private void OnRunSimulationEnded(RunController.RunSimulationEndedEvent.Context context)
    {
        foreach(KeyValuePair<Runner, RunnerUpdateRecord> kvp in context.runnerUpdateDictionary)
        {
            activeRunnerCardDictionary[kvp.Key].ShowPostRunUpdate(kvp.Key, kvp.Value);
        }


        CNExtensions.SafeStartCoroutine(this, ref continueButtonToggleRoutine, CNAction.FadeObject(continueButtonContainer.gameObject, GameManager.Instance.DefaultUIAnimationTime, 0, 1, true, false, true));
    }

    private void Toggle(bool active)
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

        runnerCompletionBubblePool.ReturnAllToPool();
        runnerSimulationCardPool.ReturnAllToPool();
        activeRunnerBubbleDictionary.Clear();
        activeRunnerCardDictionary.Clear();
        runnerSimulationCardParent.gameObject.SetActive(false);
    }

    private void SetBubblePositionAlongBar(RunnerCompletionBubble bubble, float completion)
    {
        float bounds = runCompletionBar.rect.height * .5f;
        bubble.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Mathf.Lerp(-bounds, bounds, completion));
    }
}
