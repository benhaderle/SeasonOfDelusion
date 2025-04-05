using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreateNeptune;
using Shapes;
using UnityEngine;
using UnityEngine.Events;

public class MapController : MonoBehaviour
{
    private static int MAP_LAYER = 7;

    [SerializeField] private LineMap lineMap;
    [Header("Line Variables and References")]
    [SerializeField] private PoolContext polylinePool;
    [SerializeField] private Color selectedLineColor;
    [SerializeField] private Color unselectedLineColor;
    [SerializeField] private float selectedLineThickness;
    [SerializeField] private float unselectedLineThickness;
    private RouteLine selectedLine;
    private List<RouteLine> activeRouteLines = new();
    [Header("Runner Bubble Variables and References")]
    [SerializeField] private PoolContext runnerBubblePool;
    [SerializeField] private Dictionary<Runner, MapRunnerBubble> activeRunnerBubbleDictionary = new();

    #region Events
    public class ShowRoutesEvent : UnityEvent<ShowRoutesEvent.Context>
    {
        public class Context
        {
            public List<Route> routes;
        }
    };
    public static ShowRoutesEvent showRoutesEvent = new();
    #endregion

    private void Awake()
    {
        polylinePool.Initialize();
        runnerBubblePool.Initialize();
    }

    private void OnEnable()
    {
        showRoutesEvent.AddListener(OnShowRoutes);
        RouteUIController.routeSelectedEvent.AddListener(OnRouteSelected);
        RunController.startRunEvent.AddListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.AddListener(OnRunSimulationUpdated);
    }

    private void OnDisable()
    {
        showRoutesEvent.RemoveListener(OnShowRoutes);
        RouteUIController.routeSelectedEvent.RemoveListener(OnRouteSelected);
        RunController.startRunEvent.RemoveListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.RemoveListener(OnRunSimulationUpdated);
    }

    private void Start()
    {
        MapCameraController.setMaxBoundsEvent.Invoke(new MapCameraController.SetMaxBoundsEvent.Context
        {
            maxBounds = lineMap.GetBounds()
        });
    }

    #region Event Listeners

    private void OnShowRoutes(ShowRoutesEvent.Context context)
    {
        activeRouteLines.Clear();
        polylinePool.ReturnAllToPool();

        for (int i = 0; i < context.routes.Count; i++)
        {
            InstantiateRouteLine(context.routes[i]);
        }
    }

    private void OnRouteSelected(RouteUIController.RouteSelectedEvent.Context context)
    {
        SelectLine(context.route == null ? null : activeRouteLines.First(rl => rl.RouteName == context.route.Name));
    }

    private void OnStartRun(RunController.StartRunEvent.Context context)
    {
        activeRouteLines.Clear();
        polylinePool.ReturnAllToPool();

        InstantiateRouteLine(context.route);

        MapCameraController.focusOnBoundsEvent.Invoke(new MapCameraController.FocusOnBoundsEvent.Context
        {
            bounds = activeRouteLines[0].Polyline.GetBounds()
        });

        for (int i = 0; i < context.runners.Count; i++)
        {
            MapRunnerBubble bubble = runnerBubblePool.GetPooledObject<MapRunnerBubble>();
            bubble.gameObject.layer = MAP_LAYER;
            bubble.initialsText.text = $"{context.runners[i].FirstName[0]}{context.runners[i].LastName[0]}";

            SetBubblePositionAlongLine(activeRouteLines[0], bubble, 0);

            activeRunnerBubbleDictionary.Add(context.runners[i], bubble);
        }
    }

    private void OnRunSimulationUpdated(RunController.RunSimulationUpdatedEvent.Context context)
    {
        foreach(KeyValuePair<Runner, RunnerState> keyValuePair in context.runnerStateDictionary)
        {
            MapRunnerBubble bubble = activeRunnerBubbleDictionary[keyValuePair.Key];
            float positionAlongLine = keyValuePair.Value.percentDone;

            SetBubblePositionAlongLine(activeRouteLines[0], bubble, positionAlongLine);
        }
    }

    #endregion

    private void SelectLine(RouteLine rl)
    {
        if (selectedLine != null)
        {
            selectedLine.SetLineStyle(unselectedLineColor, unselectedLineThickness);
        }

        selectedLine = rl;

        if (selectedLine != null)
        {
            selectedLine.SetLineStyle(selectedLineColor, selectedLineThickness);
            MapCameraController.focusOnBoundsEvent.Invoke(new MapCameraController.FocusOnBoundsEvent.Context
            {
                bounds = selectedLine.Polyline.GetBounds()
            });
        }
    }

    private void InstantiateRouteLine(Route route)
    {
        RouteLine rl = polylinePool.GetPooledObject<RouteLine>();
        rl.Setup(route.Name, lineMap.GetPolylinePointsFromIndices(route.lineData.pointIDs), unselectedLineColor, unselectedLineThickness);
        activeRouteLines.Add(rl);
    }

    private void SetBubblePositionAlongLine(RouteLine routeLine, MapRunnerBubble bubble, float normalizedPosition)
    {
        Vector3 pos = routeLine.GetPositionAlongRoute(normalizedPosition);
        pos.z -= 1;

        bubble.transform.position = pos;
    }
}
