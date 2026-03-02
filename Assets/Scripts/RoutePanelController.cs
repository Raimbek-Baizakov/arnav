using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RoutePanelController : MonoBehaviour
{
    public TMP_InputField startInput;
    public TMP_InputField endInput;

    public string cameraScene = "CameraScene";
    public string menuScene = "MainMenuScene";

    void Start()
    {
        if (startInput) startInput.text = RouteSession.StartText;
        if (endInput) endInput.text = RouteSession.EndText;
    }

    // Вызываем из WebView-контроллера, когда пришли адреса с карты
    public void SetAddresses(string startAddress, string endAddress)
    {
        RouteSession.StartText = (startAddress ?? "").Trim();
        RouteSession.EndText   = (endAddress ?? "").Trim();

        if (startInput) startInput.text = RouteSession.StartText;
        if (endInput)   endInput.text   = RouteSession.EndText;

        Debug.Log($"[RoutePanel] Addresses set: '{RouteSession.StartText}' -> '{RouteSession.EndText}'");
    }

    public void BuildRoute()
    {
        RouteSession.StartText = (startInput ? startInput.text : "").Trim();
        RouteSession.EndText   = (endInput ? endInput.text : "").Trim();

        if (string.IsNullOrEmpty(RouteSession.StartText) || string.IsNullOrEmpty(RouteSession.EndText))
        {
            Debug.LogWarning("Заполни старт и финиш (или выбери на карте).");
            return;
        }

        SceneManager.LoadScene(cameraScene);
    }

    public void ResetFields()
    {
        RouteSession.Clear();
        if (startInput) startInput.text = "";
        if (endInput) endInput.text = "";
    }

    public void Back()
    {
        SceneManager.LoadScene(menuScene);
    }
}