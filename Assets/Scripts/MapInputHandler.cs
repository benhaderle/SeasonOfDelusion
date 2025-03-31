using System.Collections;
using System.Collections.Generic;
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
            if (!pinching && RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.pointerCurrentRaycast.screenPosition, null, out Vector2 mouseRectPosition))
            {
                Vector2 normalizedPosition = (mouseRectPosition + new Vector2(rt.rect.width, rt.rect.height) * .5f) / new Vector2(rt.rect.width, rt.rect.height);
                Vector2 uv = normalizedPosition * new Vector2(rawImage.uvRect.width, rawImage.uvRect.height) + rawImage.uvRect.position;
                MapCameraController.dragEvent.Invoke(new MapCameraController.DragEvent.Context
                {
                    uvPosition = uv,
                    firstFrame = firstFrame
                });

                firstFrame = false;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Input.touchCount == 1)
        {
            firstFrame = true;
        }
        else if (Input.touchCount == 2)
        {
            pinching = true;
            lastPinchDistance = (Input.GetTouch(0).position - Input.GetTouch(1).position).sqrMagnitude;
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Input.touchCount < 2)
        {
            pinching = false;

            if (Input.touchCount == 1)
            {
                firstFrame = true;
            }
        }
    }
}
