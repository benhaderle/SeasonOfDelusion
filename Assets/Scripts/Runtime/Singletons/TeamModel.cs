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
    [SerializeField] private List<RunnerInitializationSO> playerTeamRunnerInitializationSOs;
    [SerializeField] private Team playerTeam;
    public string PlayerTeamName => playerTeam.Name;
    [SerializeField] private List<string> startingRosterNames;
    [SerializeField] private List<Team> otherTeams;
    [SerializeField] private RunnerCalculationVariables variables;
    public ReadOnlyCollection<Runner> PlayerRunners => playerTeam.Runners;
    private bool loaded;

    protected override void OnSuccessfulAwake()
    {
        base.OnSuccessfulAwake();
        loaded = false;
    }

    private void OnEnable()
    {
        SaveDataLoadedEvent.Instance.AddListener(OnSaveDataLoaded);
        SimulationModel.endDayEvent.AddListener(OnEndDay);
    }

    private void OnDisable()
    {
        SaveDataLoadedEvent.Instance.RemoveListener(OnSaveDataLoaded);
        SimulationModel.endDayEvent.RemoveListener(OnEndDay);
    }

    private void Start()
    {
        if (!loaded && SaveData.Instance.loaded)
        {
            OnSaveDataLoaded();
        }
    }

    private void OnSaveDataLoaded()
    {
        loaded = true;

        playerTeam.Initialize(startingRosterNames);

        foreach (string runnerName in playerTeam.GetSavedRosterNames())
        {
            AddRunnerToTeam(runnerName);
        }

        //TODO: when we loop back to races, I'll have to figure this whole initialization thing out
        //otherTeams.ForEach(team => team.Initialize(variables));

        SaveDataLoadedEvent.Instance.RemoveListener(OnSaveDataLoaded);
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

    public void AddRunnerToTeam(string runnerName)
    {
        RunnerInitializationSO initializationSO = playerTeamRunnerInitializationSOs.Find(so => $"{so.firstName}{so.lastName}" == runnerName);

        if (initializationSO != null)
        {
            playerTeam.InitializeRunner(runnerName, initializationSO, variables);
        }
    }
}