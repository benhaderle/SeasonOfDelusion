using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CreateNeptune;
using UnityEngine.SceneManagement;

/// <summary>
/// Model for the day to day simulation. (Not the run simulation)
/// </summary>
public class SimulationModel : Singleton<SimulationModel>
{
    private int day;

    #region Events
    public class StartDayEvent : UnityEvent<StartDayEvent.Context> 
    { 
        public class Context
        {
        }
    };
    public static StartDayEvent startDayEvent = new ();
    public class EndDayEvent : UnityEvent<EndDayEvent.Context> 
    { 
        public class Context
        {
        }
    };
    public static EndDayEvent endDayEvent = new ();
    #endregion

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Start()
    {
        startDayEvent.Invoke(new StartDayEvent.Context());
    }

    public void AdvanceDay()
    {
        endDayEvent.Invoke(new EndDayEvent.Context());

        day++;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
