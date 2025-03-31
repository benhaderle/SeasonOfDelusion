using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapInputHandler : MonoBehaviour, IPointerMoveHandler, IPointerDownHandler
{
    private RectTransform rt;
    private RawImage rawImage;
    private bool firstFrame;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.pointerCurrentRaycast.screenPosition, null, out Vector2 mouseRectPosition))
            {
                Vector2 normalizedPosition = (mouseRectPosition + new Vector2(rt.rect.width, rt.rect.height) * .5f) / new Vector2(rt.rect.width, rt.rect.height);
                Vector2 uv = normalizedPosition * new Vector2(rawImage.uvRect.width, rawImage.uvRect.height) + rawImage.uvRect.position;
                MapCameraController.dragEvent.Invoke(new MapCameraController.DragEvent.Context
                {
                    uvPosition = uv,
                    firstFrame = firstFrame
                });
            }

            firstFrame = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        firstFrame = true;
    }
}
