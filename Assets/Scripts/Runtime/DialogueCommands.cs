using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class DialogueCommands : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;

    private void Awake()
    {
        dialogueRunner.AddCommandHandler<string>("AddRunnerToTeam", AddRunnerToTeam);
        dialogueRunner.AddCommandHandler<string, string, float>("ChangeStat", ChangeStat);
    }

    private void AddRunnerToTeam(string runnerName)
    {
        Debug.Log($"Attempting to add runner \"{runnerName}\" to team.");

        TeamModel.Instance.AddRunnerToTeam(runnerName);
    }

    private void ChangeStat(string runnerName, string statName, float changeAmount)
    {
        Debug.Log($"Updating {statName} of {runnerName} runner(s) by {changeAmount}");

        TeamModel.Instance.ChangeRunnerStatFromDialogue(runnerName, statName, changeAmount);
    }
}
