using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Runner
{
    public string name { get; private set; }

    #region Stats

    public float vo2Max { get; private set; }
    public int endurance { get; private set; }
    public int hills { get; private set; }
    public int discipline { get; private set; }
    public int grit { get; private set; }
    public int wit { get; private set; }
    public int spirit { get; private set; }
    public int smarts { get; private set; }

    public float musclesHealth;
    public float skeletalHealth;
    public float respiratoryHealth;
    public float cardioHealth;
    public float experience;
    public float emotion;
    public float intuition;
    public float sleep;
    public float nutrition;
    public float hydration;

    #endregion

    public Runner()
    {
        vo2Max = 0;
        endurance = 0;
        hills = 0;
        discipline = 0;
        grit = 0;
        wit = 0;
        spirit = 0;
        smarts = 0;
    }
}
