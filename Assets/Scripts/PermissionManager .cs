using UnityEngine;
using UnityEngine.Android;
using System.Collections;

/// <summary>
/// Менеджер разрешений для Android.
/// Запрашивает необходимые разрешения для камеры, GPS и компаса,
/// затем инициализирует соответствующие сервисы.
/// </summary>
public class PermissionManager : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(RequestAllPermissions());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestAllPermissions()
    {
        // Запрашиваем все разрешения сразу одним списком
        string[] permissions = new string[]
        {
            Permission.Camera,
            Permission.FineLocation,
            Permission.CoarseLocation,
            "android.permission.ACTIVITY_RECOGNITION"
        };

        Permission.RequestUserPermissions(permissions);

        // Ждём пока пользователь ответит на все диалоги
        yield return new WaitUntil(() =>
            Permission.HasUserAuthorizedPermission(Permission.Camera) ||
            Permission.HasUserAuthorizedPermission(Permission.FineLocation)
        );

        yield return new WaitForSeconds(0.5f);

        // Запускаем GPS и компас после получения разрешений
        if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            if (Input.location.isEnabledByUser)
            {
                Input.location.Start(5f, 1f);
                Input.compass.enabled = true;
                Debug.Log("[Permission] GPS and compass started");
            }
            else
            {
                Debug.LogWarning("[Permission] Location disabled in phone settings!");
            }
        }
        else
        {
            Debug.LogWarning("[Permission] Location permission denied!");
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.LogWarning("[Permission] Camera permission denied!");
        }
    }
#endif

    /// <summary>
    /// Останавливает сервисы при отключении компонента.
    /// Освобождает ресурсы GPS и компаса.
    /// </summary>
    void OnDisable()
    {
        if (Input.location.status != LocationServiceStatus.Stopped)
            Input.location.Stop();

        Input.compass.enabled = false;
    }
}