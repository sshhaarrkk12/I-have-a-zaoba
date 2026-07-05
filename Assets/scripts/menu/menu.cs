using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private const string GameStartScene = "Wakeup";

    public void Gamestart()
    {
        SceneManager.LoadScene(GameStartScene);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}
