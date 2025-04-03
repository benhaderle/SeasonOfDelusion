using System;

/// <summary>
/// Represents an event in a day like practice or a race
/// </summary>
[Serializable]
public class DayEvent
{
    /// <summary>
    /// An internal identifier like "FIRSTDAYCUTSCENE" or "NORMALPRACTICE"
    /// </summary>
    public string name;
    /// <summary>
    /// The type of event like "Cutscene", "Dialogue", or "Practice"
    /// </summary>
    public string type;
    /// <summary>
    /// The time in hours that this event takes place
    /// </summary>
    public int timeHours;
    /// <summary>
    /// The time in minutes that this event takes place
    /// </summary>
    public int timeMinutes;
    /// <summary>
    /// If applicable, the ID of the cutscene attached to this event
    /// </summary>
    public string cutsceneID;
    /// <summary>
    /// If applicable, the ID of the dialogue attached to this event
    /// </summary>
    public string dialogueID;
    /// <summary>
    /// If applicable, the ID of the race route attached to this event
    /// </summary>
    public string raceRouteID;
}