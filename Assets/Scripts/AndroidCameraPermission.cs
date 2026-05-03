using UnityEngine;

/// <summary>
/// Запросчик разрешений камеры и GPS для Android.
/// Проверяет и запрашивает необходимые разрешения при запуске приложения.
/// </summary>
public class AndroidCameraPermission : MonoBehaviour
{
    /// <summary>
    /// Запускает запрос разрешений при старте.
    /// Выполняется только на Android устройствах вне редактора Unity.
    /// </summary>
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        RequestPermission(UnityEngine.Android.Permission.Camera);
        RequestPermission(UnityEngine.Android.Permission.FineLocation);
        RequestPermission(UnityEngine.Android.Permission.CoarseLocation);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Запрашивает конкретное разрешение у пользователя.
    /// Проверяет наличие разрешения перед запросом.
    /// </summary>
    /// <param name="permission">Строка с названием разрешения Android.</param>
    private void RequestPermission(string permission)
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
        {
            UnityEngine.Android.Permission.RequestUserPermission(permission);
        }
    }
#endif
}