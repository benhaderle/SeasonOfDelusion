using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CreateNeptune;
using UnityEngine;

public class TeamModel : Singleton<TeamModel>
{
    [SerializeField] private RunnerCalculationVariables variables;
    [SerializeField] private List<Runner> runners;

    public ReadOnlyCollection<Runner> Runners => runners.AsReadOnly();

    protected override void OnSuccessfulAwake()
    {
        base.OnSuccessfulAwake();

        runners.ForEach(r => r.Initialize(variables));
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
        runners.ForEach(r =>
        {
            r.OnEndDay();
        });
    }
}
