using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEditor.Rendering;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color normalBackgroundColor;
    [SerializeField] private float normalScrollSpeed;
    [SerializeField] private Gradient dayTransitionGradient;
    [SerializeField] private float dayTransitionScrollSpeed;
    [SerializeField] private AnimationCurve dayTransitionCurve;
    [SerializeField] private float dayTranstionLength;
    private Vector2 textureOffset = new();
    private float scrollSpeed;
    private IEnumerator toggleRoutine;
    private IEnumerator dayTransitionRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ();
    public class DayTransitionEndedEvent : UnityEvent { };
    public static DayTransitionEndedEvent dayTransitionEndedEvent = new ();
    #endregion

    private void Awake()
    {
        scrollSpeed = normalScrollSpeed;
    }

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
        SimulationModel.endDayEvent.AddListener(OnEndDay);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
        SimulationModel.endDayEvent.RemoveListener(OnEndDay);
    }

    private void Update()
    {
        textureOffset += Vector2.one * scrollSpeed * Time.deltaTime;
        sprite.material.SetVector("_UVOffset", textureOffset);
    }

    private void OnToggle(bool active)
    {
        if(active)
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOnRoutine());
        }
        else
        {
            CNExtensions.SafeStartCoroutine(this, ref toggleRoutine, ToggleOffRoutine());
        }
    }

    private IEnumerator ToggleOnRoutine()
    {
        yield return MaterialColorChangeRoutine(sprite.material, "_Tint", sprite.material.GetColor("_Tint"), Color.white, GameManager.Instance.DefaultUIAnimationTime);
    }

    private IEnumerator ToggleOffRoutine()
    {
        yield return MaterialColorChangeRoutine(sprite.material, "_Tint", sprite.material.GetColor("_Tint"), Color.black, GameManager.Instance.DefaultUIAnimationTime);
    }
    

    private void OnEndDay(SimulationModel.EndDayEvent.Context context)
    {
        CNExtensions.SafeStartCoroutine(this, ref dayTransitionRoutine, DayTransitionRoutine());
    }
    
    private IEnumerator DayTransitionRoutine()
    {
        float animationTime = 0;
        while (animationTime < dayTranstionLength)
        {
            float t = animationTime / dayTranstionLength;

            scrollSpeed = dayTransitionScrollSpeed;
            if (t < .2f)
            {
                scrollSpeed = Mathf.Lerp(normalScrollSpeed, dayTransitionScrollSpeed, t / .2f);
            }
            else if (t > .8f)
            {
                scrollSpeed = Mathf.Lerp(dayTransitionScrollSpeed, normalScrollSpeed, (t - .8f) / .2f);
            }

            Color backgroundColor = dayTransitionGradient.Evaluate(dayTransitionCurve.Evaluate(t));

            sprite.material.SetColor("_BackgroundColor", backgroundColor);

            animationTime += Time.deltaTime;

            yield return null;
        }
        sprite.material.SetColor("BackgroundColor", normalBackgroundColor);
        sprite.material.SetFloat("Speed", normalScrollSpeed);

        dayTransitionEndedEvent.Invoke();
    }

    private IEnumerator MaterialColorChangeRoutine(Material material, string colorName, Color startColor, Color endColor, float time)
    {
        float t = 0;
        while(t < time)
        {
            t += Time.deltaTime;

            float normalizedT = t / time;

            material.SetColor(colorName, Vector4.Lerp(startColor, endColor, normalizedT));
            yield return null;
        }

        material.SetColor(colorName, endColor);
    }
}
