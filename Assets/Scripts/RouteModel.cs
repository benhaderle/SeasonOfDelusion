using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using CreateNeptune;
using UnityEngine;

public class RouteModel : Singleton<RouteModel>
{
    [SerializeField] private List<Route> routes;

    public ReadOnlyCollection<Route> Routes => routes.AsReadOnly();

    protected override void OnSuccessfulAwake()
    {
        routes.Sort((a, b) => { return a.Length <= b.Length ? -1 : 1; });
    }
}
