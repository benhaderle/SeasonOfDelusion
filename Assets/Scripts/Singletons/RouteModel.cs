using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;

/// <summary>
/// Holds all info pertaining the routes avaialble 
/// </summary>
public class RouteModel : Singleton<RouteModel>
{
    [SerializeField] private List<Route> routes;

    public ReadOnlyCollection<Route> Routes => routes.AsReadOnly();

    private List<Route> todaysRoutes;
    public ReadOnlyCollection<Route> TodaysRoutes => todaysRoutes.AsReadOnly();

    [SerializeField] private List<RaceRoute> raceRoutes;

    protected override void OnSuccessfulAwake()
    {
        routes.Sort((a, b) => { return a.Length <= b.Length ? -1 : 1; });
        SetTodaysRoutes();
    }

    private void SetTodaysRoutes()
    {
        todaysRoutes = new List<Route>();
        int numRoutes = 3;
        for (int i = 0; i < numRoutes; i++)
        {
            todaysRoutes.Add(routes[Random.Range(i * routes.Count / numRoutes, (i + 1) * routes.Count / numRoutes)]);
        }
    }

    public RaceRoute GetRaceRoute(string id)
    {
        return raceRoutes.Find(r => r.ID == id);
    }
}
