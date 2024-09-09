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
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI routeText;
    [SerializeField] private TextMeshProUGUI easeText;
    [SerializeField] private RectTransform runCompletionBar;
    [SerializeField] private PoolContext runnerCompletionBubblePool;
    private Dictionary<Runner, RunnerCompletionBubble> activeRunnerBubbleDictionary = new();

    private IEnumerator toggleRoutine;

    private void Awake()
    {
        runnerCompletionBubblePool.Initialize();
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

        for(int i = 0; i < context.runners.Count; i++)
        {
            RunnerCompletionBubble bubble = runnerCompletionBubblePool.GetPooledObject<RunnerCompletionBubble>();

            bubble.labelText.text = $"{context.runners[i].FirstName.ToCharArray()[0]}{context.runners[i].LastName.ToCharArray()[0]}";

            SetBubblePositionAlongBar(bubble, 0);

            activeRunnerBubbleDictionary.Add(context.runners[i], bubble);
        }

        Toggle(true);
    }

    private void OnRunSimulationUpdated(RunController.RunSimulationUpdatedEvent.Context context)
    {
        foreach(KeyValuePair<Runner, RunnerCompletionBubble> keyValuePair in activeRunnerBubbleDictionary)
        {
            Runner runner = keyValuePair.Key;
            RunnerCompletionBubble bubble = keyValuePair.Value;
            RunnerState state = context.runnerStateDictionary[runner];

            SetBubblePositionAlongBar(bubble, state.percentDone);
        }

        List<RunnerCompletionBubble> bubbles = activeRunnerBubbleDictionary.Values.ToList();
        bubbles.Sort((b1, b2) => (int)(b1.GetComponent<RectTransform>().anchoredPosition.y - b2.GetComponent<RectTransform>().anchoredPosition.y));
        for(int i = 0; i < bubbles.Count; i++)
        {
            bubbles[i].transform.SetSiblingIndex(i);
        }
    }

    private void OnRunSimulationEnded(RunController.RunSimulationEndedEvent.Context context)
    {
        Toggle(false);
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
        activeRunnerBubbleDictionary.Clear();
    }

    private void SetBubblePositionAlongBar(RunnerCompletionBubble bubble, float completion)
    {
        float bounds = runCompletionBar.rect.height * .5f;
        bubble.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, Mathf.Lerp(-bounds, bounds, completion));
    }
}
