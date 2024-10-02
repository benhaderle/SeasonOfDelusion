using System.Collections;
using System.Collections.Generic;
using CreateNeptune;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuUIController : MonoBehaviour
{
    public void OnPlayButton()
    {
        SceneManager.LoadScene((int)Scene.MainScene);
    }
}
