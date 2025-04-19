using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image buttonBackground;
    [SerializeField] private float animationOffset = 25f;
    [SerializeField] private float animationSpeed = 5;
    private bool pointerDown;
    private Vector3 startPosition;
    private Vector3 maxOffsetPosition;
    public void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (buttonBackground == null)
        {
            buttonBackground = button.targetGraphic as Image;
        }
        startPosition = buttonBackground.rectTransform.localPosition;
        maxOffsetPosition = buttonBackground.rectTransform.localPosition + new Vector3(animationOffset, -animationOffset, 0);
    }

    private void Update()
    {
        if(pointerDown || animationSpeed == 0)
            return;

        if (button.interactable)
        {
            buttonBackground.rectTransform.localPosition = Vector3.Lerp(startPosition, maxOffsetPosition, Mathf.InverseLerp(-1, 1, Mathf.Sin(Time.time * animationSpeed)));
        }
        else
        {
            buttonBackground.rectTransform.localPosition = maxOffsetPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDown = true;
        buttonBackground.rectTransform.localPosition = maxOffsetPosition;

    }

	public void OnPointerUp(PointerEventData eventData)
	{
        pointerDown = false;
	}
}
