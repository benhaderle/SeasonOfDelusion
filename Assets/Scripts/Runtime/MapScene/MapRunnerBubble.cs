using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapRunnerBubble : MonoBehaviour
{
    public TextMeshProUGUI initialsText;

    private void OnEnable()
    {
        MapCameraController.focusedOnBoundsEvent.AddListener(OnFocusedOnBounds);
    }

    private void OnDisable()
    {
        MapCameraController.focusedOnBoundsEvent.RemoveListener(OnFocusedOnBounds);
    }

    private void OnFocusedOnBounds(MapCameraController.FocusedOnBoundsEvent.Context context)
    {
        transform.localScale = Vector3.one * context.cameraZoom / 10f;
    }
}