using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using Shapes;
using UnityEngine;
using UnityEngine.Events;

public class MapController : MonoBehaviour
{
    private static int MAP_LAYER = 7;

    [SerializeField] private LineMap lineMap;
    [SerializeField] private MapSaveDataSO mapSaveData;
    [SerializeField] private GameObject newRouteSimulationMarker;
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
    [SerializeField] private Dictionary<string, MapRunnerBubble> activeBubbleDictionary = new();

    #region Events
    public class ShowRoutesEvent : UnityEvent<ShowRoutesEvent.Context>
    {
        public class Context
        {
            public List<Route> routes;
        }
    };
    public static ShowRoutesEvent showRoutesEvent = new();
    public class ToggleRoutesEvent : UnityEvent
    {
    };
    public static ToggleRoutesEvent toggleRoutesEvent = new();
    public class MapPointDiscoveredEvent : UnityEvent<MapPointDiscoveredEvent.Context>
    {
        public class Context
        {
            public MapPoint point;
        }
    };
    public static MapPointDiscoveredEvent mapPointDiscoveredEvent = new();
    #endregion

    private void Awake()
    {
        polylinePool.Initialize();
        runnerBubblePool.Initialize();

        mapSaveData.Load(lineMap.points.GetDictionary().Keys.ToList());

        for (int i = 0; i < mapSaveData.data.mapPointSaveDataList.Count; i++)
        {
            lineMap.SetPointDiscovered(mapSaveData.data.mapPointSaveDataList[i].id, mapSaveData.data.mapPointSaveDataList[i].discovered);
        }
    }

    private void OnEnable()
    {
        showRoutesEvent.AddListener(OnShowRoutes);
        toggleRoutesEvent.AddListener(OnToggleRoutes);
        RouteUIController.routeSelectedEvent.AddListener(OnRouteSelected);
        RunController.startRunEvent.AddListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.AddListener(OnRunSimulationUpdated);
        WorkoutController.startWorkoutEvent.AddListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.AddListener(OnWorkoutSimulationUpdated);
        RouteModel.routeUnlockedEvent.AddListener(OnRouteUnlocked);
    }

    private void OnDisable()
    {
        showRoutesEvent.RemoveListener(OnShowRoutes);
        toggleRoutesEvent.RemoveListener(OnToggleRoutes);
        RouteUIController.routeSelectedEvent.RemoveListener(OnRouteSelected);
        RunController.startRunEvent.RemoveListener(OnStartRun);
        RunController.runSimulationUpdatedEvent.RemoveListener(OnRunSimulationUpdated);
        WorkoutController.startWorkoutEvent.RemoveListener(OnStartWorkout);
        WorkoutController.workoutSimulationUpdatedEvent.RemoveListener(OnWorkoutSimulationUpdated);
        RouteModel.routeUnlockedEvent.RemoveListener(OnRouteUnlocked);
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
            InstantiateRouteLine(context.routes[i], true);
        }
    }

    private void OnToggleRoutes()
    {
        activeRouteLines.ForEach(rl => rl.gameObject.SetActive(!rl.gameObject.activeSelf));
        
        if(!activeRouteLines[0].gameObject.activeSelf)
        {
            MapCameraController.mapUnselectedEvent.Invoke();
        }
    }

    private void OnRouteSelected(RouteUIController.RouteSelectedEvent.Context context)
    {
        SelectLine(context.route == null ? null : activeRouteLines.First(rl => rl.RouteName == context.route.DisplayName));
    }

    private void OnStartRun(RunController.StartRunEvent.Context context)
    {
        OnSimulationStart(() => InstantiateRouteLine(context.route, false), () =>
        {
            for (int i = 0; i < context.runners.Count; i++)
            {
                MapRunnerBubble bubble = runnerBubblePool.GetPooledObject<MapRunnerBubble>();
                bubble.gameObject.layer = MAP_LAYER;
                bubble.initialsText.text = $"{context.runners[i].FirstName[0]}{context.runners[i].LastName[0]}";

                SetBubblePositionAlongLine(activeRouteLines[0], bubble, 0);

                activeBubbleDictionary.Add(bubble.initialsText.text, bubble);
            }
        });
    }

    private void OnRunSimulationUpdated(RunController.RunSimulationUpdatedEvent.Context context)
    {
        foreach(KeyValuePair<Runner, RunnerState> keyValuePair in context.runnerStateDictionary)
        {
            Runner runner = keyValuePair.Key;
            MapRunnerBubble bubble = activeBubbleDictionary[$"{runner.FirstName[0]}{runner.LastName[0]}"];
            float positionAlongLine = keyValuePair.Value.totalPercentDone;

            SetBubblePositionAlongLine(activeRouteLines[0], bubble, positionAlongLine);
        }
    }

    private void OnStartWorkout(WorkoutController.StartWorkoutEvent.Context context)
    {
        OnSimulationStart(() => InstantiateRouteLine(context.workout.RouteLineData, context.workout.DisplayName, false), () =>
        {
            for (int i = 0; i < context.groups.Count; i++)
            {
                MapRunnerBubble bubble = runnerBubblePool.GetPooledObject<MapRunnerBubble>();
                bubble.gameObject.layer = MAP_LAYER;
                bubble.initialsText.text = $"{i+1}";

                SetBubblePositionAlongLine(activeRouteLines[0], bubble, 0);

                activeBubbleDictionary.Add(bubble.initialsText.text, bubble);
            }
        });
    }

    private void OnWorkoutSimulationUpdated(WorkoutController.WorkoutSimulationUpdatedEvent.Context context)
    {
        MapRunnerBubble bubble = activeBubbleDictionary[$"{context.groupIndex + 1}"];
        float positionAlongLine = context.runnerStateDictionary.Values.Min(state => state.intervalPercentDone);
        SetBubblePositionAlongLine(activeRouteLines[0], bubble, positionAlongLine);
    }

    private void OnSimulationStart(Action setupRouteLineAction, Action setUpBubblesAction)
    {
        activeRouteLines.Clear();
        polylinePool.ReturnAllToPool();

        setupRouteLineAction();

        setUpBubblesAction();

        MapCameraController.focusOnBoundsEvent.Invoke(new MapCameraController.FocusOnBoundsEvent.Context
        {
            bounds = activeRouteLines[0].Polyline.GetBounds()
        });

    }

    private void OnRouteUnlocked(RouteModel.RouteUnlockedEvent.Context context)
    {
        newRouteSimulationMarker.SetActive(true);
        int nextPointID = context.route.lineData.pointIDs.FindIndex(id => id == context.unlockedPoint.id) + 1;
        Vector3 offset = (lineMap.GetMapPointFromID(context.route.lineData.pointIDs[nextPointID]).point - context.unlockedPoint.point).normalized;
        newRouteSimulationMarker.transform.position = context.unlockedPoint.point + offset;
    }

    #endregion

    private void SelectLine(RouteLine rl)
    {
        if (selectedLine != null)
        {
            selectedLine.SetLineStyle(unselectedLineColor, unselectedLineThickness, 0);
        }

        selectedLine = rl;

        if (selectedLine != null)
        {
            selectedLine.SetLineStyle(selectedLineColor, selectedLineThickness, 1);
            MapCameraController.focusOnBoundsEvent.Invoke(new MapCameraController.FocusOnBoundsEvent.Context
            {
                bounds = selectedLine.Polyline.GetBounds()
            });
        }
    }

    private void InstantiateRouteLine(Route route, bool showNewRouteText)
    {
        InstantiateRouteLine(route.lineData, route.DisplayName, showNewRouteText);
    }

    private void InstantiateRouteLine(RouteLineData routeLineData, string routeName, bool showNewRouteText)
    {
        RouteLine rl = polylinePool.GetPooledObject<RouteLine>();

        List<MapPoint> routePoints = lineMap.GetMapPointsFromIDs(routeLineData.pointIDs);
        List<bool> pointsDiscovered = routePoints.Select(mp => mapSaveData.mapPointDictionary[mp.id].discovered).ToList();

        rl.Setup(routeName, routePoints, pointsDiscovered, showNewRouteText, unselectedLineColor, unselectedLineThickness);
        
        if (routeLineData.Length == 0)
        {
            routeLineData.SetLength(rl.Polyline.points);
        }
        activeRouteLines.Add(rl);
    }

    private void SetBubblePositionAlongLine(RouteLine routeLine, MapRunnerBubble bubble, float normalizedPosition)
    {
        Vector3 pos = routeLine.GetPositionAlongRoute(normalizedPosition, out int closestPointID);
        if (!mapSaveData.mapPointDictionary[closestPointID].discovered)
        {
            mapSaveData.mapPointDictionary[closestPointID].discovered = true;
            lineMap.SetPointDiscovered(closestPointID, true);
            mapPointDiscoveredEvent.Invoke(new MapPointDiscoveredEvent.Context
            {
                point = lineMap.GetMapPointFromID(closestPointID)
            });
        }
        pos.z -= 1;

        bubble.transform.position = pos;
    }
}
