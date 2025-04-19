using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MapCameraController : MonoBehaviour
{
    [SerializeField] new private Camera camera;
    
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float zoomMin;
    [SerializeField] private float zoomMax;
    [SerializeField] private float menuDampingTime = .1f;
    [SerializeField] private float simulationDampingTime = .5f;
    private float currentDampingTime;
    [SerializeField] private float maxBoundsPadding = 5f;
    [SerializeField] private LayerMask tappingLayerMask;
    private Vector3 lastDragViewportPosition;
    private Vector3 targetPosition;
    private Vector3 dampingVelocity = Vector3.zero;
    private float targetZoom = 0;
    private float dampingZoomVelocity = 0;
    private Bounds maxBounds;
    private Bounds currentRouteBounds;

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

    private void Awake()
    {
        currentDampingTime = menuDampingTime;
    }
    private void OnEnable()
    {
        setMaxBoundsEvent.AddListener(OnSetMaxBounds);
        dragEvent.AddListener(OnDrag);
        zoomEvent.AddListener(OnZoom);
        tapEvent.AddListener(OnTap);
        focusOnBoundsEvent.AddListener(OnFocusOnBounds);
        RouteModel.routeUnlockedEvent.AddListener(OnRouteUnlocked);
        RunController.runSimulationResumeEvent.AddListener(OnRunSimulationResume);
    }

    private void OnDisable()
    {
        setMaxBoundsEvent.RemoveListener(OnSetMaxBounds);
        dragEvent.RemoveListener(OnDrag);
        zoomEvent.RemoveListener(OnZoom);
        tapEvent.RemoveListener(OnTap);
        focusOnBoundsEvent.RemoveListener(OnFocusOnBounds);
        RouteModel.routeUnlockedEvent.RemoveListener(OnRouteUnlocked);
        RunController.runSimulationResumeEvent.RemoveListener(OnRunSimulationResume);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref dampingVelocity, currentDampingTime);
        camera.orthographicSize = Mathf.SmoothDamp(camera.orthographicSize, targetZoom, ref dampingZoomVelocity, currentDampingTime);
    }

    private void OnSetMaxBounds(SetMaxBoundsEvent.Context context)
    {
        maxBounds = context.maxBounds;
        SetTargetPosition(transform.position);
        targetZoom = camera.orthographicSize;
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
        targetZoom -= context.zoomAmount * .4f;
        targetZoom = Mathf.Clamp(targetZoom, zoomMin, zoomMax);   
    }

    private void OnTap(TapEvent.Context context)
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(context.viewportPosition.x, context.viewportPosition.y, 10));

        if (Physics.SphereCast(ray, .5f, out RaycastHit hitInfo, Mathf.Infinity, tappingLayerMask))
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
        FocusOnBounds(context.bounds);
        currentRouteBounds = context.bounds;
    }

    private void OnRouteUnlocked(RouteModel.RouteUnlockedEvent.Context context)
    {
        currentDampingTime = simulationDampingTime;
        FocusOnBounds(new Bounds(new Vector3(context.unlockedPoint.point.x, context.unlockedPoint.point.y), new Vector3(1, 1)));
    }

    private void OnRunSimulationResume(RunController.RunSimulationResumeEvent.Context context)
    {
        FocusOnBounds(currentRouteBounds);
    }

    private void FocusOnBounds(Bounds bounds)
    {
        SetTargetPosition(bounds.center);

        if (bounds.size.x > bounds.size.y)
        {
            targetZoom = Mathf.Abs(bounds.size.x) / camera.aspect / 2f;
        }
        else
        {
            targetZoom = Mathf.Abs(bounds.size.y) / 2f;
        }
        targetZoom *= 2f;
        targetZoom = Mathf.Clamp(targetZoom, zoomMin, zoomMax);
    }

    private void SetTargetPosition(Vector3 newTargetPos)
    {
        targetPosition.z = -10;

        if (maxBounds != null)
        {
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
