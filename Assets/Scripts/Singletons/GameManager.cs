using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CreateNeptune;
using UnityEngine;

public enum Scene 
{ 
    StartScene = 0,
    MainScene = 1, 
    IntroCutscene = 2,
    PredayCutscene = 3
}

/// <summary>
/// Kinda random class. Don't put things here unless they don't make sense anywhere else.
/// Most things will get moved out to a more specific class at a point at which that makes sense. 
/// </summary>
public class GameManager : Singleton<GameManager>
{
    /// <summary>
    /// A default UI animation time to use across scenes
    /// </summary>
    [SerializeField] private float defaultUIAnimationTime;
    public float DefaultUIAnimationTime => defaultUIAnimationTime;
}
