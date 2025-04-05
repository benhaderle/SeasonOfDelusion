using System.Collections;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using UnityEngine.Events;

public class MapCameraController : MonoBehaviour
{
    [SerializeField] private Camera camera;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float zoomMin;
    [SerializeField] private float zoomMax;
    [SerializeField] private float dampingTime = .1f;
    [SerializeField] private float maxBoundsPadding = 5f;
    [SerializeField] private LayerMask tappingLayerMask;
    private Vector3 lastDragViewportPosition;
    private Vector3 targetPosition;
    private Vector3 dampingVelocity = Vector3.zero;
    private Bounds maxBounds;

    #region Events
    public class SetMaxBoundsEvent : UnityEvent<SetMaxBoundsEvent.Context>
    {
        public class Context
        {
            public Bounds maxBounds;
        }
    };
    public static SetMaxBoundsEvent setMaxBoundsEvent = new();
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
    public class TapEvent : UnityEvent<TapEvent.Context>
    {
        public class Context
        {
            public Vector2 viewportPosition;
        }
    };
    public static TapEvent tapEvent = new();
    public class RouteLineTappedEvent : UnityEvent<RouteLineTappedEvent.Context>
    {
        public class Context
        {
            public string routeName;
        }
    };
    public static RouteLineTappedEvent routeLineTappedEvent = new();
    public class MapUnselectedEvent : UnityEvent
    {
    };
    public static MapUnselectedEvent mapUnselectedEvent = new();
    public class FocusOnBoundsEvent : UnityEvent<FocusOnBoundsEvent.Context>
    {
        public class Context
        {
            public Bounds bounds;
        }
    };
    public static FocusOnBoundsEvent focusOnBoundsEvent = new();
    #endregion

    private void OnEnable()
    {
        setMaxBoundsEvent.AddListener(OnSetMaxBounds);
        dragEvent.AddListener(OnDrag);
        zoomEvent.AddListener(OnZoom);
        tapEvent.AddListener(OnTap);
        focusOnBoundsEvent.AddListener(OnFocusOnBounds);
    }

    private void OnDisable()
    {
        setMaxBoundsEvent.RemoveListener(OnSetMaxBounds);
        dragEvent.RemoveListener(OnDrag);
        zoomEvent.RemoveListener(OnZoom);
        tapEvent.RemoveListener(OnTap);
        focusOnBoundsEvent.RemoveListener(OnFocusOnBounds);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dampingVelocity, dampingTime);
    }

    private void OnSetMaxBounds(SetMaxBoundsEvent.Context context)
    {
        maxBounds = context.maxBounds;
        SetTargetPosition(transform.position);
    }

    private void OnDrag(DragEvent.Context context)
    {

        if (!context.firstFrame)
        {
            Vector3 uvWorldPos = camera.ViewportToWorldPoint(new Vector3(context.uvPosition.x, context.uvPosition.y, -transform.position.z));
            Vector3 lastDragWorldPos = camera.ViewportToWorldPoint(new Vector3(lastDragViewportPosition.x, lastDragViewportPosition.y, -transform.position.z));
            Vector3 newTargetPosition = targetPosition - ((uvWorldPos - lastDragWorldPos) * movementSpeed);
            newTargetPosition.z = targetPosition.z;
            SetTargetPosition(newTargetPosition);
        }
        lastDragViewportPosition = context.uvPosition;
    }

    private void OnZoom(ZoomEvent.Context context)
    {
        Vector3 newTargetPosition = targetPosition;
        newTargetPosition.z += context.zoomAmount;
        SetTargetPosition(newTargetPosition);
    }

    private void OnTap(TapEvent.Context context)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(context.viewportPosition.x, context.viewportPosition.y, 10));

        if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, tappingLayerMask))
        { 
            routeLineTappedEvent.Invoke(new RouteLineTappedEvent.Context
            {
                routeName = hitInfo.collider?.GetComponent<RouteLine>().RouteName
            });
        }
        else
        {
            mapUnselectedEvent.Invoke();
        }
    }

    private void OnFocusOnBounds(FocusOnBoundsEvent.Context context)
    {
        float cameraDistance = 2.0f; // Constant factor
        Vector3 objectSizes = context.bounds.max - context.bounds.min;
        float objectSize = Mathf.Max(objectSizes.x, objectSizes.y, objectSizes.z);
        float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * camera.fieldOfView); // Visible height 1 meter in front
        float distance = cameraDistance * objectSize / cameraView; // Combined wanted distance from the object
        distance += 0.5f * objectSize; // Estimated offset from the center to the outside of the object
        Vector3 newTargetPosition = context.bounds.center - distance * camera.transform.forward;
        SetTargetPosition(newTargetPosition);
    }

    private void SetTargetPosition(Vector3 newTargetPos)
    {
        targetPosition.z = Mathf.Clamp(newTargetPos.z, zoomMin, zoomMax);

        if (maxBounds != null)
        {
            float padding = Mathf.Lerp(0, maxBoundsPadding, Mathf.InverseLerp(zoomMax, zoomMin, targetPosition.z));

            float tanX = Mathf.Tan(.5f * Mathf.Deg2Rad * Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect));
            float length = (maxBounds.center.z - targetPosition.z) * tanX;
            float minX = maxBounds.min.x - length + maxBoundsPadding;
            float maxX = maxBounds.max.x + length - maxBoundsPadding;
            targetPosition.x = Mathf.Clamp(newTargetPos.x, minX, maxX);

            float tanY = Mathf.Tan(.5f * Mathf.Deg2Rad * camera.fieldOfView);
            length = (maxBounds.center.z - targetPosition.z) * tanY;
            float minY = maxBounds.min.y - length + maxBoundsPadding;
            float maxY = maxBounds.max.y + length - maxBoundsPadding;
            targetPosition.y = Mathf.Clamp(newTargetPos.y, minY, maxY);
        }
        else
        {
            targetPosition.x = newTargetPos.x;
            targetPosition.y = newTargetPos.y;
        }
    }
}
