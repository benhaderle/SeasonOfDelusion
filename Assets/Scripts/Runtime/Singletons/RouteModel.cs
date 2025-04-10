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

    [SerializeField] private List<RaceRoute> raceRoutes;

    protected override void OnSuccessfulAwake()
    {
        routes.Sort((a, b) => { return a.Length <= b.Length ? -1 : 1; });
    }


    public RaceRoute GetRaceRoute(string id)
    {
        return raceRoutes.Find(r => r.ID == id);
    }
}
