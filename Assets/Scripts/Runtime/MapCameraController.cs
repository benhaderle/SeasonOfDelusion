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
    [SerializeField] private LayerMask tappingLayerMask;
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
    #endregion

    private void Awake()
    {
        targetPosition = transform.position;
    }

    private void OnEnable()
    {
        dragEvent.AddListener(OnDrag);
        zoomEvent.AddListener(OnZoom);
        tapEvent.AddListener(OnTap);
    }

    private void OnDisable()
    {
        dragEvent.RemoveListener(OnDrag);
        zoomEvent.RemoveListener(OnZoom);
        tapEvent.RemoveListener(OnTap);
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
}
