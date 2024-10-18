using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;

public class BackgroundController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;

    private IEnumerator toggleRoutine;

    #region Events
    public class ToggleEvent : UnityEvent<bool> { };
    public static ToggleEvent toggleEvent = new ToggleEvent();
    #endregion

    private void OnEnable()
    {
        toggleEvent.AddListener(OnToggle);
    }

    private void OnDisable()
    {
        toggleEvent.RemoveListener(OnToggle);
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
