using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MapCameraController : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float zoomMin;
    [SerializeField] private float zoomMax;
    [SerializeField] private float dampingTime = .1f;
    private Vector3 lastDragViewportPosition;
    private Vector3 targetPosition;
    private Vector3 dampingVelocity = Vector3.zero;

    #region Events
    public class DragEvent : UnityEvent<DragEvent.Context>
    {
        public class Context
        {
            public Vector2 uvPosition;
            public bool firstFrame;
        }
    };
    public static DragEvent dragEvent = new();
    public class ZoomEvent : UnityEvent<ZoomEvent.Context>
    {
        public class Context
        {
            public float zoomAmount;
        }
    };
    public static ZoomEvent zoomEvent = new();
    #endregion

    private void Awake()
    {
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        dragEvent.AddListener(OnDrag);
        zoomEvent.AddListener(OnZoom);
    }

    private void OnDisable()
    {
        dragEvent.RemoveListener(OnDrag);
        zoomEvent.RemoveListener(OnZoom);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dampingVelocity, dampingTime);
    }

    private void OnDrag(DragEvent.Context context)
    {
        Vector3 uvWorldPos = camera.ViewportToWorldPoint(new Vector3(context.uvPosition.x, context.uvPosition.y, -transform.position.z));

        if (!context.firstFrame)
        {
            float targetZ = targetPosition.z;
            targetPosition -= (uvWorldPos - camera.ViewportToWorldPoint(new Vector3(lastDragViewportPosition.x, lastDragViewportPosition.y, -transform.position.z))) * movementSpeed;
            targetPosition.z = targetZ;
        }
        lastDragViewportPosition = context.uvPosition;
    }

    private void OnZoom(ZoomEvent.Context context)
    {
        targetPosition.z += context.zoomAmount;
        targetPosition.z = Mathf.Clamp(targetPosition.z, zoomMin, zoomMax);
    }
}
