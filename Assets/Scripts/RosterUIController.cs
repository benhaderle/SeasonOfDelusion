using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CreateNeptune;

public class RosterUIController : MonoBehaviour
{
    [SerializeField] private PoolContext runnerCardPool;

    private void Awake()
    {
        runnerCardPool.Initialize();
    }

    // Start is called before the first frame update
    private void Start()
    {
        for(int i = 0; i < TeamModel.Instance.Runners.Count; i++)
        {
            runnerCardPool.GetPooledObject<RunnerCard>();
        }
    }
}
