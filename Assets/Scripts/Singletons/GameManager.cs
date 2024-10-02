using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CreateNeptune;
using UnityEngine;

public enum Scene { StartScene, MainScene}

/// <summary>
/// Kinda random class. Don't put things here unless they don't make sense absolutely anywhere else.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    /// <summary>
    /// A default UI animation time to use across scenes
    /// </summary>
    [SerializeField] private float defaultUIAnimationTime;
    public float DefaultUIAnimationTime => defaultUIAnimationTime;
}
