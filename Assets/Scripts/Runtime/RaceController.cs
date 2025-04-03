using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CreateNeptune;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsible for the guts of the race simulation
/// </summary>
public class RaceController : MonoBehaviour
{
    /// <summary>
    /// How fast the simulation should run at normally (not in an opportunity zone)
    /// </summary>
    [SerializeField] private float simulationSecondsPerRealSecondsNormal = 30;
    /// <summary>
    /// How fast the simulation should run at while in a opportunity zone
    /// </summary>
    [SerializeField] private float simulationSecondsPerRealSecondsInOpportunityZone = 10;
    /// <summary>
    /// How fast the simulation is running currently
    /// </summary>
    private float simulationSecondsPerRealSeconds;

    /// <summary>
    /// How many simulation-seconds should pass before we update people's speeds and such
    /// </summary>
    [SerializeField] private float simulationStep = 60f;

    [Header("Run VO2 Calculation Variables")]
    /// <summary>
    /// The max standard deviation a runner's VO2 can be off from the coach's guidance at the beginning of a run in percentage of that runner's V02.
    /// IE if coach's guidance is .8 and maxDeviation is .1, then at most, the range from -1sigma to 1sigma will be .7-.9
    /// </summary>
    [SerializeField] private float maxDeviation = .1f;
    /// <summary>
    /// The max amount of experience before we consider a runner "fully experienced"
    /// </summary>
    [SerializeField] private float experienceCap = 1000000f;
    /// <summary>
    /// The amount of soreness at which point additional soreness no longer impacts performance
    /// </summary>
    [SerializeField] private float maxSoreness = 500f;
    /// <summary>
    /// The max amount in percentage of a runner's VO2 that soreness will effect
    /// IE if VO2 percent is .8, sorenessEffect is .1, and the runner is half sore, then VO2 will change to .75
    /// </summary>
    [SerializeField] private float sorenessEffect = .1f;
    private int currentOpportunityZoneIndex;
    private bool inOpportunityZone;
    private bool lastRunnerInOpportunityZone;
    private Runner currentRunnerInOpportunityZone;
    private RunnerState currentRunnerStateInOpportunityZone;
    private List<Runner> runnersThroughOpportunityZone = new();

    private Dictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary = new();

    #region Events
    public class StartRaceEvent : UnityEvent<StartRaceEvent.Context>
    {
        public class Context
        {
            public List<Team> teams;
            public RaceRoute raceRoute;
            public RunConditions runConditions;
        }
    };
    public static StartRaceEvent startRaceEvent = new();

    public class RaceSimulationUpdatedEvent : UnityEvent<RaceSimulationUpdatedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerState> runnerStateDictionary;
        }
    }
    public static RaceSimulationUpdatedEvent raceSimulationUpdatedEvent = new();

    public class RaceSimulationEndedEvent : UnityEvent<RaceSimulationEndedEvent.Context>
    {
        public class Context
        {
            public ReadOnlyDictionary<Runner, RunnerUpdateRecord> runnerUpdateDictionary;
            public List<TeamRaceResultRecord> sortedTeamRaceResultRecords;
        }
    }
    public static RaceSimulationEndedEvent raceSimulationEndedEvent = new();
    public class RaceOpportunityStartedEvent : UnityEvent<RaceOpportunityStartedEvent.Context>
    {
        public class Context
        {
            public float distance;
        }
    }
    public static RaceOpportunityStartedEvent raceOpportunityStartedEvent = new();
    public class RunnerInRaceOpportunityZoneEvent : UnityEvent<RunnerInRaceOpportunityZoneEvent.Context>
    {
        public class Context
        {
            public Runner runner;
            public RunnerState runnerState;
        }
    }
    public static RunnerInRaceOpportunityZoneEvent runnerInRaceOpportunityZoneEvent = new();
    public class RaceOpportunityEndedEvent : UnityEvent<RaceOpportunityEndedEvent.Context>
    {
        public class Context
        {
        }
    }
    public static RaceOpportunityEndedEvent raceOpportunityEndedEvent = new();

    #endregion

    private void OnEnable()
    {
        startRaceEvent.AddListener(OnStartRace);
        RaceOpportunityUIController.raceOpportunityButtonPressedEvent.AddListener(OnRaceOpportunityButtonPressed);
    }

    private void OnDisable()
    {
        startRaceEvent.RemoveListener(OnStartRace);
        RaceOpportunityUIController.raceOpportunityButtonPressedEvent.RemoveListener(OnRaceOpportunityButtonPressed);
    }

    private void OnStartRace(StartRaceEvent.Context context)
    {
        simulationSecondsPerRealSeconds = simulationSecondsPerRealSecondsNormal;
        inOpportunityZone = false;
        currentOpportunityZoneIndex = 0;
        StartCoroutine(SimulateRaceRoutine(context.teams, context.raceRoute));
    }

    /// <summary>
    /// Simulates an entire race for the given teams
    /// </summary>
    private IEnumerator SimulateRaceRoutine(List<Team> teams, RaceRoute raceRoute)
    {
        float targetVO2Percent = 1.1f;

        // wait a frame for the other starts to get going
        yield return null;

        // go through each runner and initialize their state for this workout
        Dictionary<Runner, RunnerState> runnerStates = new();
        List<KeyValuePair<Runner, RunnerState>> sortedRunnerStates;

        teams.ForEach(t => t.Runners.ToList().ForEach(r =>  runnerStates.Add(r, new RunnerState())));

        // reset speed and initial V02 for every runner
        foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
        {
            Runner runner = kvp.Key;
            RunnerState state = kvp.Value;

            //TODO: this sets the V02 perfectly at the start of each interval bc the other way with rolls was too random
            // but this should probably account for experience and soreness in some way
            state.desiredVO2 = runner.currentVO2Max * targetVO2Percent;
            state.currentSpeed = 0;
            state.desiredSpeed = 0;
            state.intervalDistance = 0;
        }

        // while all runners have not finished, simulate the run
        while (runnerStates.Values.Any(state => state.totalDistance < raceRoute.Length))
        {
            // first figure out every runner's preferred speed
            foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
            {
                Runner runner = kvp.Key;
                RunnerState state = kvp.Value;

                state.desiredVO2 = RunUtility.StepRunnerVO2(runner, state, targetVO2Percent, maxSoreness);
                state.desiredSpeed = RunUtility.CaclulateSpeedFromOxygenCost(state.desiredVO2 * runner.CalculateRunEconomy(state));
            }

            // now that we have everyone's desired speed, we use a gravity model to group people
            int numGravityIterations = 2;
            for (int i = 0; i < numGravityIterations; i++)
            {
                foreach (KeyValuePair<Runner, RunnerState> kvp in runnerStates)
                {
                    Runner runner = kvp.Key;
                    RunnerState state = kvp.Value;

                    state.desiredSpeed = RunUtility.RunGravityModel(runner, state, runnerStates, targetVO2Percent, raceRoute.Length);

                    // if this is the last iteration, set the current speed
                    if (i == numGravityIterations - 1)
                    {
                        state.currentSpeed = state.desiredSpeed;
                    }
                }
            }

            //then spend a second simulating before moving on to the next iteration
            float simulationTime = simulationStep;
            while (simulationTime > 0)
            {
                float timePassed = simulationSecondsPerRealSeconds * Time.deltaTime;
                RunUtility.StepRunState(runnerStates, timePassed, raceRoute.Length, raceRoute.Length);

                float opportunityZoneThreshold = .05f;
                //if we've got a runner within the next opporunity threshold, trigger the opportunity flow
                if (!inOpportunityZone && currentOpportunityZoneIndex < raceRoute.OpportunityMarkers.Count
                    && runnerStates.Max(kvp => kvp.Value.totalDistance) > raceRoute.OpportunityMarkers[currentOpportunityZoneIndex] - opportunityZoneThreshold)
                {
                    raceOpportunityStartedEvent.Invoke(new RaceOpportunityStartedEvent.Context { distance = raceRoute.OpportunityMarkers[currentOpportunityZoneIndex] });
                    inOpportunityZone = true;
                    simulationSecondsPerRealSeconds = simulationSecondsPerRealSecondsInOpportunityZone;
                }
                else if (inOpportunityZone)
                {
                    float currentOpportunityDistance = raceRoute.OpportunityMarkers[currentOpportunityZoneIndex];
                    // if we're in the opportunity zone, but don't have a current runner, try to see if there is one
                    if (currentRunnerInOpportunityZone == null)
                    {
                        //get the runners on the player team that are not past the oppportunity zone sorted by total distance along the course
                        List<KeyValuePair<Runner, RunnerState>> playerTeamRunnersInZone = GetRunnersOnTeam(runnerStates, TeamModel.Instance.PlayerTeamName);
                        playerTeamRunnersInZone = playerTeamRunnersInZone.Where(kvp => !runnersThroughOpportunityZone.Contains(kvp.Key)).OrderByDescending(kvp => kvp.Value.totalDistance).ToList();

                        //check to see if the next runner on the player team is in the zone
                        //if they are store their info and broadcast the event that they're in the zone
                        RunnerState runnerState = playerTeamRunnersInZone[0].Value;
                        if (runnerState.totalDistance > currentOpportunityDistance - opportunityZoneThreshold && runnerState.totalDistance <= currentOpportunityDistance)
                        {
                            currentRunnerInOpportunityZone = playerTeamRunnersInZone[0].Key;
                            currentRunnerStateInOpportunityZone = runnerState;

                            runnerInRaceOpportunityZoneEvent.Invoke(new RunnerInRaceOpportunityZoneEvent.Context
                            {
                                runner = currentRunnerInOpportunityZone,
                                runnerState = runnerState
                            });

                            //if the list count is 1, then this is the last runner
                            if (playerTeamRunnersInZone.Count == 1)
                            {
                                lastRunnerInOpportunityZone = true;
                            }
                        }
                    }
                    else
                    {
                        // if we have a runner in the opportunity zone, slow down the simulation as they approach the marker
                        // we will listen for an event to set things back to normal speed
                        RunnerState runnerState = runnerStates[currentRunnerInOpportunityZone];
                        simulationSecondsPerRealSeconds = simulationSecondsPerRealSecondsInOpportunityZone * Mathf.InverseLerp(currentOpportunityDistance, currentOpportunityDistance - opportunityZoneThreshold, runnerState.totalDistance);
                    }
                }

                raceSimulationUpdatedEvent.Invoke(new RaceSimulationUpdatedEvent.Context
                {
                    runnerStateDictionary = new ReadOnlyDictionary<Runner, RunnerState>(runnerStates),
                });

                yield return null;
                simulationTime -= timePassed;
            }
        }

        //sort the runner states in finish order
        sortedRunnerStates = runnerStates.ToList();
        sortedRunnerStates.Sort((kvp1, kvp2) => RunUtility.SortRunnerState(kvp1.Value, kvp2.Value));

        //figure out the team results
        List<TeamRaceResultRecord> teamRaceResultRecords = new();
        for (int i = 0; i < teams.Count; i++)
        {
            List<KeyValuePair<Runner, RunnerState>> runnersOnTeam = GetRunnersOnTeam(runnerStates, teams[i].Name);
            int[] runnerScores = runnersOnTeam.Select(r => sortedRunnerStates.IndexOf(r) + 1).OrderBy(n => n).ToArray();

            teamRaceResultRecords.Add(new TeamRaceResultRecord
            {
                teamName = teams[i].Name,
                runnerScores = runnerScores,
                teamScore = runnerScores[0] + runnerScores[1] + runnerScores[2] + runnerScores[3]
            });
        }
        teamRaceResultRecords = teamRaceResultRecords.OrderBy(t => t.teamScore).ToList();

        // post run update for the player team
        foreach (Runner runner in teams[0].Runners)
        {
            RunnerState state = runnerStates[runner];

            RunnerUpdateRecord record = runner.PostRunUpdate(state);
            runnerUpdateDictionary.Add(runner, record);
        }

        raceSimulationEndedEvent.Invoke(new RaceSimulationEndedEvent.Context()
        {
            runnerUpdateDictionary = new ReadOnlyDictionary<Runner, RunnerUpdateRecord>(runnerUpdateDictionary),
            sortedTeamRaceResultRecords = teamRaceResultRecords
        });
    }

    private void OnRaceOpportunityButtonPressed(RaceOpportunityUIController.RaceOpportunityButtonPressedEvent.Context context)
    {
        if (context.ease < 0)
        {
            currentRunnerStateInOpportunityZone.desiredVO2 *= .7f;
        }
        else if (context.ease > 0)
        {
            currentRunnerStateInOpportunityZone.desiredVO2 *= 1.3f;
        }

        //reset the simulation seconds and set everything as null
        runnersThroughOpportunityZone.Add(currentRunnerInOpportunityZone);
        simulationSecondsPerRealSeconds = simulationSecondsPerRealSecondsInOpportunityZone;
        currentRunnerInOpportunityZone = null;
        currentRunnerStateInOpportunityZone = null;

        //do a check to see if that's all the runners thru the opportunity zone
        if (lastRunnerInOpportunityZone)
        {
            //reset the simulation speed
            simulationSecondsPerRealSeconds = simulationSecondsPerRealSecondsNormal;

            //increment to the next opportunity zone
            currentOpportunityZoneIndex++;

            //reset all the bools and clear the list of runners thru the zone
            inOpportunityZone = false;
            lastRunnerInOpportunityZone = false;
            runnersThroughOpportunityZone.Clear();

            raceOpportunityEndedEvent.Invoke(new RaceOpportunityEndedEvent.Context { });
        }
    }

    /// <param name="stateDictionary">The dictionary whose entries you want to select runners and states from</param>
    /// <param name="teamName">The team name whose runners you are looking for</param>
    /// <returns>A List of all runners and states that are part of the given team</returns>
    private List<KeyValuePair<Runner, RunnerState>> GetRunnersOnTeam(Dictionary<Runner, RunnerState> stateDictionary, string teamName)
    {
        return stateDictionary.Where(kvp => kvp.Key.TeamName == teamName).ToList();
    }
}

public class TeamRaceResultRecord
{
    public string teamName;
    public int teamScore;
    public int[] runnerScores;

}