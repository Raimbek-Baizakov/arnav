using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Контроллер кнопки "Назад" в сцене камеры.
/// Обрабатывает возврат пользователя к карте маршрута.
/// </summary>
public class CameraBackButton : MonoBehaviour
{
    // === НАСТРОЙКИ ===

    /// <summary>Название сцены карты маршрута для возврата.</summary>
    [Header("Scene")]
    public string routeSceneName = "RouteMapScene";

    /// <summary>
    /// Возвращает пользователя к сцене карты маршрута.
    /// Используется как обработчик нажатия кнопки "Назад".
    /// </summary>
    public void GoBack()
    {
        if (string.IsNullOrEmpty(routeSceneName))
        {
            routeSceneName = "RouteMapScene";
        }

        SceneManager.LoadScene(routeSceneName);
    }
}
