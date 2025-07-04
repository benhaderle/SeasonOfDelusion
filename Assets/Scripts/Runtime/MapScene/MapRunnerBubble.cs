using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MapRunnerBubble : MonoBehaviour
{
    public TextMeshProUGUI initialsText;

    private void OnEnable()
    {
        MapCameraController.onFocusedOnBoundsEvent.AddListener(OnFocusedOnBounds);
    }

    private void OnDisable()
    {
        MapCameraController.onFocusedOnBoundsEvent.RemoveListener(OnFocusedOnBounds);
    }

    private void OnFocusedOnBounds(MapCameraController.OnFocusedOnBoundsEvent.Context context)
    {
        transform.localScale = Vector3.one * context.cameraZoom / 10f;
    }
}