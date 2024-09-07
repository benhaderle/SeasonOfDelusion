using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CreateNeptune;
using UnityEngine;

public class TeamModel : Singleton<TeamModel>
{
    [SerializeField] private List<Runner> runners;

    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    protected override void OnSuccessfulAwake()
    {
        base.OnSuccessfulAwake();

        runners.ForEach(r => r.Initialize());
    }
}
