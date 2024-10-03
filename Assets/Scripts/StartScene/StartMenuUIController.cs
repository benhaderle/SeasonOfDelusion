using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class StartMenuUIController : MonoBehaviour
{
    public void OnPlayButton()
    {
        SceneManager.LoadScene((int)Scene.MainScene);
    }
    
}
