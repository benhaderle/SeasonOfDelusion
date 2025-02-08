using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Each dialogue should have an entry in this enum to be used as an ID
/// </summary>
public enum DialogueID { Intro = 0 };

[CreateAssetMenu(fileName = "newDialogue", menuName = "ScriptableObjects/Dialogue")]
public class Dialogue : ScriptableObject
{
    /// <summary>
    /// The ID for this dialogue
    /// </summary>
    public DialogueID dialogueID;
    /// <summary>
    /// A list of nodes for this dialogue
    /// </summary>
    public DialogueNode[] dialogueNodes;
}

/// <summary>
/// A node in a dialogue tree
/// </summary>
[Serializable]
public class DialogueNode
{
    /// <summary>
    /// Who is saying this line
    /// </summary>
    public string speaker;
    /// <summary>
    /// The text to be spoken
    /// </summary>
    public string text;
}
