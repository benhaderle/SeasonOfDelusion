using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarouselScrollRect : ScrollRect
{
    public UnityEvent OnDragEnded = new UnityEvent();

    private bool isDragging;
    public bool IsDragging => isDragging;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);

        isDragging = true;
    }

    public override void OnEndDrag(PointerEventData data)
    {
        base.OnEndDrag(data);

        isDragging = false;

        OnDragEnded.Invoke();
    }
}
