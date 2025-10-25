using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[Serializable]
public class Team
{
    [SerializeField] private TeamSaveDataSO saveData;
    [SerializeField] private string name;
    public string Name => name;
    private List<Runner> runners = new();
    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    public void Initialize(List<string> runnerNames)
    {
        if (saveData != null && !saveData.data.initialized)
        {
            saveData.Initialize(name, runnerNames);
        }
    }

    public Runner InitializeRunner(string runnerName, RunnerInitializationSO initializationSO, RunnerCalculationVariables variables)
    {
        if (saveData != null && saveData.data != null && !saveData.data.roster.Contains(runnerName))
        {
            saveData.data.roster.Add(runnerName);
        }

        Runner r = new();
        r.Initialize(initializationSO, variables, name);
        runners.Add(r);

        return r;    
    }

    public List<string> GetSavedRosterNames()
    {
        return saveData.data.roster;
    }

    public void OnEndDay()
    {
        runners.ForEach(r =>
        {
            r.OnEndDay();
        });
    }

}
