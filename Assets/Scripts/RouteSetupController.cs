using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class RouteSetupController : MonoBehaviour
{
    public TMP_InputField startInput;
    public TMP_InputField endInput;

    public string mapScene = "MapSelectScene";
    public string cameraScene = "CameraScene";
    public string menuScene = "MainMenuScene";

    void Start()
    {
        if (!string.IsNullOrEmpty(RouteSession.StartText)) startInput.text = RouteSession.StartText;
        if (!string.IsNullOrEmpty(RouteSession.EndText)) endInput.text = RouteSession.EndText;
    }

    public void OpenMap()
    {
        RouteSession.StartText = startInput.text.Trim();
        RouteSession.EndText = endInput.text.Trim();

#if UNITY_ANDROID && !UNITY_EDITOR
        // Запрашиваем доступ к геолокации (нужен для кнопки "Я здесь" в map.html)
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            // Можно сразу открыть карту — но иногда удобнее открыть после выдачи разрешения.
            // Я оставляю открытие сразу, чтобы не ломать UX:
        }
#endif

        SceneManager.LoadScene(mapScene);
    }

    public void BuildRoute()
    {
        RouteSession.StartText = startInput.text.Trim();
        RouteSession.EndText = endInput.text.Trim();

        if (string.IsNullOrEmpty(RouteSession.StartText) || string.IsNullOrEmpty(RouteSession.EndText))
        {
            Debug.LogWarning("Заполни старт и финиш (или выбери на карте).");
            return;
        }

        SceneManager.LoadScene(cameraScene);
    }

    public void Back()
    {
        SceneManager.LoadScene(menuScene);
    }
}