using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerializedSaveData
{
    // The timestamp at which this save data was last saved (UTC).
    public string timestamp;

    public SimulationSaveData simulationSaveData;
    public RunnerSaveData[] playerRunnerSaveDatas;
    public RouteSaveData[] routeSaveDatas;
    public WorkoutSaveData[] workoutSaveDatas;
    public MapSaveData mapSaveData;
}