using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CreateNeptune;
using Shapes;
using UnityEngine;
using UnityEngine.Events;

public class MapController : MonoBehaviour
{
    [SerializeField] private LineMap lineMap;
    [SerializeField] private PoolContext polylinePool;
    [SerializeField] private Color selectedLineColor;
    [SerializeField] private Color unselectedLineColor;
    [SerializeField] private float selectedLineThickness;
    [SerializeField] private float unselectedLineThickness;
    private RouteLine selectedLine;
    private List<RouteLine> activeRouteLines = new();

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
    }

    private void OnEnable()
    {
        showRoutesEvent.AddListener(OnShowRoutes);
        RouteUIController.routeSelectedEvent.AddListener(OnRouteSelected);
    }

    private void OnDisable()
    {
        showRoutesEvent.RemoveListener(OnShowRoutes);
        RouteUIController.routeSelectedEvent.RemoveListener(OnRouteSelected);
    }

    private void OnShowRoutes(ShowRoutesEvent.Context context)
    {
        for (int i = 0; i < context.routes.Count; i++)
        {
            RouteLine rl = polylinePool.GetPooledObject<RouteLine>();
            rl.Setup(context.routes[i].Name, lineMap.GetPolylinePointsFromIndices(context.routes[i].lineData.pointIDs), unselectedLineColor, unselectedLineThickness);
            activeRouteLines.Add(rl);
        }
    }

    private void OnRouteSelected(RouteUIController.RouteSelectedEvent.Context context)
    {
        SelectLine(context.route == null ? null : activeRouteLines.First(rl => rl.RouteName == context.route.Name));
    }

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
        }
    }
}
