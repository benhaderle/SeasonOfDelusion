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
    }

    private void AddRunnerToTeam(string runnerName)
    {
        Debug.Log($"Attempting to add runner \"{runnerName}\" to team.");

        TeamModel.Instance.AddRunnerToTeam(runnerName);
    }
}
