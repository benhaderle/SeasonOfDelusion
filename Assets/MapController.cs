using System.Collections;
using System.Collections.Generic;
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
    private Polyline selectedLine;
    private List<Polyline> activePolylines = new();

    #region Events
    public class ShowRoutesEvent : UnityEvent<ShowRoutesEvent.Context>
    {
        public class Context
        {
            public List<RouteLineData> routes;
        }
    };
    public static ShowRoutesEvent showRoutesEvent = new();
    public class RouteLineSelectedEvent : UnityEvent<RouteLineSelectedEvent.Context>
    {
        public class Context
        {
            public Polyline polyline;
        }
    };
    public static RouteLineSelectedEvent routeLineSelectedEvent = new();
    #endregion

    private void Awake()
    {
        polylinePool.Initialize();
    }

    private void OnEnable()
    {
        showRoutesEvent.AddListener(OnShowRoutes);
        routeLineSelectedEvent.AddListener(OnRouteLineSelected);
    }

    private void OnDisable()
    {
        showRoutesEvent.RemoveListener(OnShowRoutes);
        routeLineSelectedEvent.RemoveListener(OnRouteLineSelected);
    }

    private void OnShowRoutes(ShowRoutesEvent.Context context)
    {
        for (int i = 0; i < context.routes.Count; i++)
        {
            Polyline p = polylinePool.GetPooledObject<Polyline>();
            p.gameObject.layer = 8;
            p.Color = unselectedLineColor;
            p.Thickness = unselectedLineThickness;
            p.SetPoints(lineMap.GetPolylinePointsFromIndices(context.routes[i].pointIDs));
            if (i == 0)
            {
                SelectLine(p);
            }

            MeshCollider mc = p.GetComponent<MeshCollider>();
            Mesh mesh = new Mesh();
            ShapesMeshGen.GenPolylineMeshWithThickness(mesh, p.points, false, PolylineJoins.Simple, true, false, p.Thickness);
            mc.sharedMesh = mesh;

            activePolylines.Add(p);
        }
    }

    private void OnRouteLineSelected(RouteLineSelectedEvent.Context context)
    {
        SelectLine(context.polyline);
    }

    private void SelectLine(Polyline p)
    {
        if (selectedLine != null)
        {
            selectedLine.Color = unselectedLineColor;
            selectedLine.Thickness = unselectedLineThickness;
        }

        selectedLine = p;

        if (selectedLine != null)
        {
            selectedLine.Color = selectedLineColor;
            selectedLine.Thickness = selectedLineThickness;
        }
    }
}
