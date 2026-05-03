using UnityEngine;

/// <summary>
/// Инициализатор приложения.
/// Настраивает глобальные параметры приложения при запуске.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    /// <summary>
    /// Вызывается при инициализации объекта.
    /// Настраивает приложение для работы в фоне на мобильных устройствах.
    /// </summary>
    void Awake()
    {
        // Предотвращаем выключение приложения при выходе на домашний экран
        Application.runInBackground = true;
    }
}