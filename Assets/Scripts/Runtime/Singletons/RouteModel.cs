using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using Shapes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Holds all info pertaining the routes avaialble 
/// </summary>
public class RouteModel : Singleton<RouteModel>
{
    [SerializeField] private List<Route> routes;

    public ReadOnlyCollection<Route> Routes => routes.AsReadOnly();

    [SerializeField] private List<RaceRoute> raceRoutes;

    #region Events
    public class RouteUnlockedEvent : UnityEvent<RouteUnlockedEvent.Context>
    {
        public class Context
        {
            public Route route;
            public MapPoint unlockedPoint;
        }
    };
    public static RouteUnlockedEvent routeUnlockedEvent = new();

    #endregion

    protected override void OnSuccessfulAwake()
    {
        for(int i = 0; i < routes.Count; i++)
        {
            routes[i].OnValidate();
        }

        routes.Sort((a, b) => { return a.Length <= b.Length ? -1 : 1; });
    }

    private void OnEnable()
    {
        MapController.mapPointDiscoveredEvent.AddListener(OnMapNodeDiscovered);
    }

    private void OnDisable()
    {
        MapController.mapPointDiscoveredEvent.RemoveListener(OnMapNodeDiscovered);
    }

    private void OnMapNodeDiscovered(MapController.MapPointDiscoveredEvent.Context context)
    {
        for (int i = 0; i < routes.Count; i++)
        {
            if (routes[i].CheckUnlock(context.point.id))
            {
                routeUnlockedEvent.Invoke(new RouteUnlockedEvent.Context
                {
                    route = routes[i],
                    unlockedPoint = context.point
                });
            }
        }
    }

    public RaceRoute GetRaceRoute(string id)
    {
        return raceRoutes.Find(r => r.ID == id);
    }
}
