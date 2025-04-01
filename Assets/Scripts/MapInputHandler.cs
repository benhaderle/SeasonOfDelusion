using System.Collections;
using System.Collections.Generic;
using CreateNeptune;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapInputHandler : MonoBehaviour, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rt;
    private RawImage rawImage;
    private float lastPinchDistance;
    private bool pinching;
    private bool firstFrame;
    private bool pointerOver;

    private float tapTime = .1f;
    private float lastTapTime;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        if (pointerOver)
        {
            MapCameraController.zoomEvent.Invoke(new MapCameraController.ZoomEvent.Context
            {
                zoomAmount = Input.mouseScrollDelta.y
            });
        }

        if (pinching)
        {
            float currentPinchDistance = (Input.GetTouch(0).position - Input.GetTouch(1).position).sqrMagnitude;
            MapCameraController.zoomEvent.Invoke(new MapCameraController.ZoomEvent.Context
            {
                zoomAmount = (currentPinchDistance - lastPinchDistance) / 40000f
            });

            lastPinchDistance = currentPinchDistance;
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0) && Input.touchCount < 2)
        {
            if (!pinching)
            {
                MapCameraController.dragEvent.Invoke(new MapCameraController.DragEvent.Context
                {
                    uvPosition = TransformPointFromScreenToViewport(eventData.position),
                    firstFrame = firstFrame
                });

                firstFrame = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.touchCount < 2)
        {
            firstFrame = true;
            lastTapTime = Time.time;
        }
        else if (Input.touchCount == 2)
        {
            pinching = true;
            lastPinchDistance = (Input.GetTouch(0).position - Input.GetTouch(1).position).sqrMagnitude;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Input.touchCount < 2)
        {
            pinching = false;
            firstFrame = true;

            if (Time.time - lastTapTime < tapTime)
            {
                MapCameraController.tapEvent.Invoke(new MapCameraController.TapEvent.Context
                {
                    viewportPosition = TransformPointFromScreenToViewport(eventData.position)
                });
            }

        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
    }

    private Vector2 TransformPointFromScreenToViewport(Vector2 screenPoint)
    {
        Vector2 viewportPoint = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, null, out Vector2 mouseRectPosition))
        {
            Vector2 normalizedPosition = (mouseRectPosition + new Vector2(rt.rect.width, rt.rect.height) * .5f) / new Vector2(rt.rect.width, rt.rect.height);
            viewportPoint = normalizedPosition * new Vector2(rawImage.uvRect.width, rawImage.uvRect.height) + rawImage.uvRect.position;
        }

        return viewportPoint;
    }
}
