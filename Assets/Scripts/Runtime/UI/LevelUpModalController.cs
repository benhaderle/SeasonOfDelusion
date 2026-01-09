using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using TMPro;
using System.Linq;
using UnityEngine.Playables;

public class LevelUpModalController : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI preAerobicText;
    [SerializeField] private TextMeshProUGUI postAerobicText;
    [SerializeField] private TextMeshProUGUI preStrengthText;
    [SerializeField] private TextMeshProUGUI postStrengthText;
    [SerializeField] private TextMeshProUGUI preFormText;
    [SerializeField] private TextMeshProUGUI postFormText;
    [SerializeField] private TextMeshProUGUI preGritText;
    [SerializeField] private TextMeshProUGUI postGritText;
    [SerializeField] private TextMeshProUGUI preRecoveryText;
    [SerializeField] private TextMeshProUGUI postRecoveryText;
    private int runnerIndex;
    private List<KeyValuePair<Runner, RunnerUpdateRecord>> runnerUpdateRecords;
    private IEnumerator toggleRoutine;

    #region Events
    public class LevelUpEvent : UnityEvent<LevelUpEvent.Context>
    {
        public class Context
        {
            public List<KeyValuePair<Runner, RunnerUpdateRecord>> runnerUpdateRecords;
        }
    }
    public static LevelUpEvent levelUpEvent = new();
    #endregion

    private void Awake()
    {
        Toggle(false);
    }

    private void OnEnable()
    {
        levelUpEvent.AddListener(OnLevelUp);
    }

    private void OnDisable()
    {
        levelUpEvent.RemoveListener(OnLevelUp);
    }

    public void OnContinueButton()
    {
        if (runnerIndex < runnerUpdateRecords.Count - 1)
        {
            SetModalValues(runnerIndex + 1);
        }
        else
        {
            Toggle(false);
        }
    }

    private void OnLevelUp(LevelUpEvent.Context context)
    {
        Toggle(true);

        runnerUpdateRecords = context.runnerUpdateRecords;
        SetModalValues(0);

        playableDirector.Play();
    }

    private void SetModalValues(int newRunnerIndex)
    {
        runnerIndex = newRunnerIndex;

        nameText.text = runnerUpdateRecords[runnerIndex].Key.Name;
        LevelUpRecord r = runnerUpdateRecords[runnerIndex].Value.levelUpRecords.Last();

        levelText.text = $"LV {r.newLevel}";
        preAerobicText.text = r.oldVO2.ToString("0.0");
        postAerobicText.text = r.newVO2.ToString("0.0");
        preStrengthText.text = r.oldStrength.ToString("0.0");
        postStrengthText.text = r.newStrength.ToString("0.0");
        preFormText.text = r.oldForm.ToString("0.0");
        postFormText.text = r.newForm.ToString("0.0");
        preGritText.text = r.oldGrit.ToString("0.0");
        postGritText.text = r.newGrit.ToString("0.0");
        preRecoveryText.text = r.oldRecovery.ToString("0.0");
        postRecoveryText.text = r.newRecovery.ToString("0.0");
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
