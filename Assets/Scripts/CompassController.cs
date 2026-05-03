using UnityEngine;

/// <summary>
/// Контроллер компаса для отображения направления.
/// Управляет UI элементом компаса, сглаживает показания и использует
/// гироскоп как резервный источник данных.
/// </summary>
public class CompassController : MonoBehaviour
{
    // === КОМПОНЕНТЫ ===

    /// <summary>RectTransform UI элемента компаса для вращения.</summary>
    [SerializeField] private RectTransform compassUI;

    // === НАСТРОЙКИ ===

    /// <summary>Скорость сглаживания поворота компаса.</summary>
    [SerializeField] private float smoothSpeed = 5f;

    // === ВНУТРЕННИЕ ПОЛЯ ===

    /// <summary>Текущее направление в градусах.</summary>
    private float currentHeading = 0f;

    /// <summary>Сглаженное направление для плавной анимации.</summary>
    private float smoothedHeading = 0f;

    /// <summary>Временная метка последнего обновления компаса.</summary>
    private double lastTimestamp = 0;

    void Start()
    {
        Input.compass.enabled = true;
        Input.gyro.enabled = true;
        Input.location.Start();

        Debug.Log("[Compass] Initialized.");
    }

    void Update()
    {
        bool compassUpdated = Input.compass.enabled && Input.compass.timestamp > lastTimestamp;

        if (compassUpdated)
        {
            lastTimestamp = Input.compass.timestamp;
            currentHeading = Input.compass.trueHeading;
            Debug.Log($"[Compass] True heading: {currentHeading:F1}°, Accuracy: {Input.compass.headingAccuracy:F1}°");
        }
        else if (Input.gyro.enabled)
        {
            currentHeading += Input.gyro.rotationRateUnbiased.y * Time.deltaTime * Mathf.Rad2Deg;
            currentHeading = (currentHeading + 360f) % 360f; // нормализация
            Debug.Log($"[Compass] Gyro fallback: {currentHeading:F1}°");
        }
        else
        {
            Debug.LogWarning("[Compass] No compass or gyro available!");
            return;
        }

        // LerpAngle корректно обрабатывает переход через 0°/360°
        smoothedHeading = Mathf.LerpAngle(smoothedHeading, currentHeading, Time.deltaTime * smoothSpeed);
        compassUI.rotation = Quaternion.Euler(0, 0, -smoothedHeading);
    }

    void OnDisable()
    {
        Input.compass.enabled = false;
        Input.gyro.enabled = false;

        if (Input.location.status != LocationServiceStatus.Stopped)
            Input.location.Stop();
    }
}