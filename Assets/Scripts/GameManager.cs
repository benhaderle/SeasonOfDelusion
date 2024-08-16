using System.Collections;
using System.Collections.Generic;
using CreateNeptune;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private float defaultUIAnimationTime;
    public float DefaultUIAnimationTime => defaultUIAnimationTime;
}
