using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreateNeptune;
using TMPro;
using UnityEngine;

public class StatChangeEffectController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PoolContext effectTextPool;
    [SerializeField] private float effectTextHeight = 150;
    [SerializeField] private float timeBetweenTexts = 1.3f;
    private Queue<string> changeStringQueue = new();
    private IEnumerator toggleRoutine;
    private IEnumerator effectRoutine;


    private void Awake()
    {
        effectTextPool.Initialize();
        OnToggle(false);
    }

    private void OnEnable()
    {
        TeamModel.statChangedFromDialogueEvent.AddListener(OnStatChangedFromDialogue);
    }

    private void OnDisable()
    {
        TeamModel.statChangedFromDialogueEvent.RemoveListener(OnStatChangedFromDialogue);
    }
     private void OnToggle(bool active)
    {
        if(active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine,  CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 1, CNEase.EaseType.Linear, true, false, true));
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine,CNAction.FadeObject(canvas, GameManager.Instance.DefaultUIAnimationTime, canvasGroup.alpha, 0, CNEase.EaseType.Linear, false, true, true));
        }
    }

    private void OnStatChangedFromDialogue(TeamModel.StatChangedFromDialogueEvent.Context context)
    {
        string signString = context.statChange > 0 ? "+" : "";
        changeStringQueue.Enqueue($"{context.runnerName} {context.statName} {signString}{context.statChange}");

        if (effectRoutine == null)
        {
            CNExtensions.SafeStartCoroutine(this, ref effectRoutine, EffectRoutine());
        }
    }
    
    private IEnumerator EffectRoutine()
    {
        OnToggle(true);

        while (changeStringQueue.Count > 0)
        {
            string s = changeStringQueue.Dequeue();

            TextMeshProUGUI effectText = effectTextPool.GetPooledObject<TextMeshProUGUI>();
            effectText.text = s;

            effectText.rectTransform.offsetMax = new Vector2(0, 0);
            effectText.rectTransform.offsetMin = new Vector2(0, -effectTextHeight);

            yield return new WaitForSeconds(timeBetweenTexts);
        }

        yield return new WaitForSeconds(5f);
        
        OnToggle(false);

        effectTextPool.ReturnAllToPool();
    }
}
