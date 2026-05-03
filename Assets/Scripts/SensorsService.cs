using System.Collections;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Сервис для работы с датчиками устройства.
/// Предоставляет данные GPS (позиция, скорость), компаса и управляет разрешениями.
/// Используется другими компонентами для навигации и ориентации.
/// </summary>
public class SensorsService : MonoBehaviour
{
    // === СОСТОЯНИЕ СЕРВИСА ===

    /// <summary>Готов ли сервис к работе (разрешения получены, датчики инициализированы).</summary>
    public bool isReady { get; private set; }

    // === ДАННЫЕ GPS ===

    /// <summary>Текущая позиция пользователя (широта, долгота).</summary>
    public Vector2 gpsLatLon { get; private set; }

    /// <summary>Текущее направление компаса (градусы, 0 = север).</summary>
    public float compassHeading { get; private set; }

    /// <summary>Текущая скорость движения (м/с), рассчитанная по GPS.</summary>
    public float gpsSpeed { get; private set; } // м/с

    // === ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ===

    /// <summary>Предыдущая позиция GPS для расчета скорости.</summary>
    private Vector2 lastGpsPos;

    /// <summary>Время последнего расчета скорости.</summary>
    private float lastSpeedCalcTime;

    /// <summary>
    /// Инициализация сервиса датчиков.
    /// Запрашивает разрешения, включает компас и GPS, ждет готовности.
    /// </summary>
    IEnumerator Start()
    {
        Debug.Log("[Sensors] Starting initialization...");

#if UNITY_ANDROID
        // Запрашиваем разрешения на геолокацию
        string[] permissions = {
            Permission.FineLocation,
            Permission.CoarseLocation
        };

        Permission.RequestUserPermissions(permissions);

        // Ждём пока пользователь ответит на запрос разрешений
        yield return new WaitUntil(() =>
            Permission.HasUserAuthorizedPermission(Permission.FineLocation));

        yield return new WaitForSeconds(0.3f);
        Debug.Log("[Sensors] Permissions granted");
#endif

        // Проверяем, включена ли геолокация в настройках устройства
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("[Sensors] Location service disabled by user.");
            yield break;
        }

        // Включаем компас и гироскоп
        Input.compass.enabled = true;
        Input.gyro.enabled = true;
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[Sensors] Compass enabled: {Input.compass.enabled}");

        // Запускаем GPS с желаемой точностью
        Debug.Log("[Sensors] Starting GPS...");
        Input.location.Start(5f, 1f);

        // Ждём инициализации GPS с таймаутом
        float timeout = 20f;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Проверяем успешность запуска GPS
        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError("[Sensors] GPS failed: " + Input.location.status);
            yield break;
        }

        Debug.Log("[Sensors] GPS initialized successfully");

        // Ждём первых данных компаса
        float compassTimeout = 5f;
        yield return new WaitUntil(() =>
        {
            compassTimeout -= Time.deltaTime;
            return Input.compass.timestamp > 0 || compassTimeout <= 0;
        });

        Debug.Log($"[Sensors] Compass - mag: {Input.compass.magneticHeading:F1}°, " +
                  $"true: {Input.compass.trueHeading:F1}°, " +
                  $"timestamp: {Input.compass.timestamp}");

        // Сервис готов к работе
        isReady = true;
        Debug.Log("[Sensors] All sensors ready!");
    }

    /// <summary>
    /// Обновление данных датчиков каждый кадр.
    /// Читает GPS позицию, компас и рассчитывает скорость.
    /// </summary>
    void Update()
    {
        if (!isReady) return;

        // Получаем последние данные GPS
        var data = Input.location.lastData;
        Vector2 currentPos = new Vector2((float)data.latitude, (float)data.longitude);
        gpsLatLon = currentPos;

        // Рассчитываем скорость на основе перемещения (обновляем каждые 2 секунды)
        if (Time.time - lastSpeedCalcTime > 2f)
        {
            if (lastGpsPos != Vector2.zero)
            {
                float distanceKm = GeoUtils.DistanceKm(lastGpsPos, currentPos);
                float timeSeconds = Time.time - lastSpeedCalcTime;
                gpsSpeed = (distanceKm * 1000f) / timeSeconds; // м/с
            }
            lastGpsPos = currentPos;
            lastSpeedCalcTime = Time.time;
        }

        // Логируем данные компаса каждые 2 секунды
        if (Time.time % 2 < Time.deltaTime)
        {
            Debug.Log($"[Sensors] COMPASS: mag={Input.compass.magneticHeading:F1}°, " +
                      $"true={Input.compass.trueHeading:F1}°, " +
                      $"enabled={Input.compass.enabled}, " +
                      $"timestamp={Input.compass.timestamp}");
        }

        // Обновляем heading компаса
        compassHeading = Input.compass.trueHeading;

        // Логируем GPS данные каждые 5 секунд
        if (Time.time % 5 < Time.deltaTime)
        {
            Debug.Log($"[Sensors] GPS: {gpsLatLon.x:F6}, {gpsLatLon.y:F6}");
        }
    }

    void OnDisable()
    {
        Input.compass.enabled = false;
        Input.gyro.enabled = false;
        if (Input.location.status != LocationServiceStatus.Stopped)
            Input.location.Stop();
    }
}