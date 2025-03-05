using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerializedSaveData
{

    // The student id extracted from a student's email.
    public string userID;
    // The timestamp at which this save data was last saved (UTC).
    public string timestamp;

    public SimulationSaveData simulationSaveData;
    public RunnerSaveData[] playerRunnerSaveDatas;
}