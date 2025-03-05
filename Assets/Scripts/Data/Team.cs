using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class Team
{
    [SerializeField] private string name;
    public string Name => name;
    [SerializeField] private List<Runner> runners;
    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    public void Initialize(RunnerCalculationVariables variables)
    {
        runners.ForEach(r => r.Initialize(variables, name));
    }

    public void OnEndDay()
    {
        runners.ForEach(r =>
        {
            r.OnEndDay();
        });
    }

}
