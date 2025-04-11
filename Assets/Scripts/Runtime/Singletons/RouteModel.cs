using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Holds all info pertaining the routes avaialble 
/// </summary>
public class RouteModel : Singleton<RouteModel>
{
    [SerializeField] private List<Route> routes;

    public ReadOnlyCollection<Route> Routes => routes.AsReadOnly();

    [SerializeField] private List<RaceRoute> raceRoutes;

    public void OnValidate()
    {
        if (routes != null)
        {
            for (int i = 0; i < routes.Count; i++)
            {
                routes[i].Validate();
            }
        }
    }

    protected override void OnSuccessfulAwake()
    {
        for(int i = 0; i < routes.Count; i++)
        {
            routes[i].Validate();
        }

        routes.Sort((a, b) => { return a.Length <= b.Length ? -1 : 1; });
    }

    public RaceRoute GetRaceRoute(string id)
    {
        return raceRoutes.Find(r => r.ID == id);
    }
}
