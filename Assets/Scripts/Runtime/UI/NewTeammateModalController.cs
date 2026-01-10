using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.UI;

public class NewTeammateModalController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI aerobicStatText;
    [SerializeField] private TextMeshProUGUI strengthStatText;
    [SerializeField] private TextMeshProUGUI formStatText;
    [SerializeField] private TextMeshProUGUI gritStatText;
    [SerializeField] private TextMeshProUGUI recoveryStatText;
    [SerializeField] private TextMeshProUGUI academicsStatText;
    private IEnumerator toggleRoutine;

    private void Awake()
    {
        Toggle(false);
    }

    private void OnEnable()
    {
        TeamModel.newTeammateAddedEvent.AddListener(OnNewTeammateAdded);
    }

    private void OnDisable()
    {
        TeamModel.newTeammateAddedEvent.RemoveListener(OnNewTeammateAdded);
    }

    public void OnContinueButton()
    {
        Toggle(false);
    }

    private void OnNewTeammateAdded(TeamModel.NewTeammateAddedEvent.Context context)
    {
        Toggle(true);

        // set values
        nameText.text = context.runner.Name;
        portraitImage.sprite = context.runner.GetCurrentConfidenceSprite();
        levelText.text = $"lv {context.runner.level}";

        aerobicStatText.text = context.runner.currentVO2Max.ToString("0.0");
        strengthStatText.text = context.runner.currentStrength.ToString("0.0");
        formStatText.text = context.runner.currentForm.ToString("0.0");
        gritStatText.text = context.runner.currentGrit.ToString("0.0");
        recoveryStatText.text = context.runner.currentRecovery.ToString("0.0");
        academicsStatText.text = context.runner.currentAcademics.ToString("0.0");

        playableDirector.Play();
    }

    private void Toggle(bool active)
    {
        if (active)
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
}