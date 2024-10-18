using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueID { Intro = 0 };

[CreateAssetMenu(fileName = "newDialogue", menuName = "ScriptableObjects/Dialogue")]
public class Dialogue : ScriptableObject
{
    public DialogueID dialogueID;
    public DialogueNode[] dialogueNodes;
}

[Serializable]
public class DialogueNode
{
    public string speaker;
    public string text;
}
