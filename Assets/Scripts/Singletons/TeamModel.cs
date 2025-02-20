using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;

/// <summary>
/// Model for the list of teams
/// </summary>
public class TeamModel : Singleton<TeamModel>
{
    [SerializeField] private Team playerTeam;
    [SerializeField] private List<Team> otherTeams;
    [SerializeField] private RunnerCalculationVariables variables;
    [SerializeField] private List<Runner> runners;

    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    protected override void OnSuccessfulAwake()
    {
        base.OnSuccessfulAwake();

        playerTeam.Initialize(variables);
        otherTeams.ForEach(team => team.Initialize(variables));
    }

    private void OnEnable()
    {
        SimulationModel.endDayEvent.AddListener(OnEndDay);
    }

    private void OnDisable()
    {
        SimulationModel.endDayEvent.AddListener(OnEndDay);
    }

    private void OnEndDay(SimulationModel.EndDayEvent.Context context)
    {
        playerTeam.OnEndDay();
    }

    /// <returns>A List of all teams including the player team appended to the beginning</returns>
    public List<Team> GetAllTeams()
    {
        List<Team> allTeams = new()
        {
            playerTeam
        };
        allTeams.AddRange(otherTeams);

        return allTeams;
    }
}

[Serializable]
public class Team
{
    [SerializeField] private string name;
    public string Name => name;
    [SerializeField] private List<Runner> runners;
    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    public void Initialize(RunnerCalculationVariables variables)
    {
        runners.ForEach(r => r.Initialize(variables));
    }

    public void OnEndDay()
    {
        runners.ForEach(r =>
        {
            r.OnEndDay();
        });
    }

}