using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OpenRouteSetup()
    {
        SceneManager.LoadScene("RouteMapScene");
    }

    public void Exit()
    {
        Application.Quit();
        Debug.Log("Exit pressed");
    }
}