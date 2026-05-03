using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер 3D стрелки навигации для AR-режима.
/// Создает 3D модель стрелки, рендерит ее в текстуру и поворачивает в направлении цели.
/// Использует GPS и компас для точной ориентации в реальном мире.
/// </summary>
public class _3dHudArrow : MonoBehaviour
{
    // === ССЫЛКИ НА КОМПОНЕНТЫ ===

    /// <summary>Ссылка на сервис датчиков для получения GPS и компасных данных.</summary>
    [Header("Refs")]
    public SensorsService sensors;

    /// <summary>Ссылка на WebView для отправки сообщений о завершении маршрута.</summary>
    private WebViewObject webView;

    // === НАСТРОЙКИ 3D СТРЕЛКИ ===

    /// <summary>Имя GameObject'а 3D модели стрелки в сцене.</summary>
    [Header("3D Arrow Setup")]
    public string arrowModelName = "3darrow";

    /// <summary>Размер RenderTexture для рендеринга стрелки (ширина и высота).</summary>
    public int renderTextureSize = 256;

    /// <summary>Цвет фона RenderTexture (обычно прозрачный для AR).</summary>
    public Color arrowBackgroundColor = new Color(0, 0, 0, 0); // прозрачный фон

    /// <summary>Смещение поворота по оси Z для коррекции визуального отображения.</summary>
    public float zRotationOffset = 100f; // смещение поворота по оси Z для коррекции визуального смещения

    // === НАСТРОЙКИ ЦЕЛИ ===

    /// <summary>Координаты целевой точки (широта, долгота) для навигации.</summary>
    [Header("Target")]
    public Vector2 targetLatLon; // временно зададим вручную в инспекторе

    /// <summary>Использовать ли координаты из RouteSession вместо ручных настроек.</summary>
    public bool useRouteSessionTarget = true;

    // === НАСТРОЙКИ НАВИГАЦИИ ===

    /// <summary>Порог расстояния (км) для определения, находится ли пользователь на маршруте.</summary>
    [Header("Route navigation")]
    public float onRouteThresholdKm = 0.02f; // 20 метров для пешехода

    /// <summary>Порог расстояния (км) до финиша для автоматического завершения маршрута.</summary>
    public float finishThresholdKm = 0.05f; // 50 метров до финиша для сброса маршрута

    /// <summary>Коэффициент сглаживания для интерполяции целевой точки (0.01-1.0).</summary>
    [Range(0.01f, 1f)] public float targetLerp = 0.2f; // сглаживание цели

    // === НАСТРОЙКИ СГЛАЖИВАНИЯ ПОВОРОТА ===

    /// <summary>Коэффициент сглаживания поворота стрелки (0.01-1.0).</summary>
    [Header("Rotation smoothing")]
    [Range(0.01f, 1f)] public float arrowLerp = 0.6f;

    /// <summary>Порог скачка bearing (градусы), при превышении которого поворот происходит мгновенно.</summary>
    public float jumpThresholdDeg = 90f; // если bearing скачет больше чем на это значение, сразу прыгаем

    // === ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ===

    /// <summary>Текущий угол поворота стрелки по оси Z.</summary>
    float currentZ;

    /// <summary>Текущая сглаженная цель для навигации.</summary>
    Vector2 currentTarget;

    /// <summary>Флаг инициализации для первого кадра.</summary>
    bool initialized;

    /// <summary>Сглаженное значение heading компаса.</summary>
    private float smoothedCompassHeading;

    // === КОМПОНЕНТЫ 3D СТРЕЛКИ ===

    /// <summary>Камера для рендеринга 3D стрелки в текстуру.</summary>
    private Camera arrowCamera;

    /// <summary>RenderTexture для хранения изображения стрелки.</summary>
    private RenderTexture arrowRenderTexture;

    /// <summary>Transform 3D модели стрелки.</summary>
    private Transform arrow3DTransform;

    /// <summary>Начальная локальная ориентация стрелки.</summary>
    private Quaternion initialArrowLocalRotation;

    /// <summary>
    /// Инициализация 3D стрелки.
    /// Загружает маршрут, настраивает цель и подготавливает компоненты для рендеринга стрелки.
    /// </summary>
    void Start()
    {
        // Загружаем маршрут из PlayerPrefs, если он сохранён и не загружен
        if (!RouteSession.HasRoute)
        {
            RouteSession.LoadRoute();
        }

        Debug.Log($"[3dHudArrow] Start: useRouteSessionTarget={useRouteSessionTarget}, HasEndCoords={RouteSession.HasEndCoords}, HasRoute={RouteSession.HasRoute}");

        // Устанавливаем цель из RouteSession или используем ручные настройки
        if (useRouteSessionTarget && RouteSession.HasEndCoords)
        {
            targetLatLon = new Vector2((float)RouteSession.EndLat, (float)RouteSession.EndLon);
            Debug.Log($"[3dHudArrow] Target from RouteSession: {targetLatLon.x:F6},{targetLatLon.y:F6}");
        }
        else
        {
            Debug.Log($"[3dHudArrow] Using manual target: {targetLatLon.x:F6},{targetLatLon.y:F6}");
        }

        // Находим WebView для отправки сообщений о завершении
        var webViewController = FindObjectOfType<RouteMapWebViewController>();
        if (webViewController != null)
        {
            webView = webViewController.GetComponent<WebViewObject>();
        }

        // Настраиваем 3D стрелку
        Setup3DArrow();
    }

    /// <summary>
    /// Настраивает компоненты для рендеринга 3D стрелки.
    /// Создает RenderTexture, камеру и находит 3D модель стрелки в сцене.
    /// </summary>
    private void Setup3DArrow()
    {
        // Найти 3D модель стрелки по имени
        GameObject arrowModel = GameObject.Find(arrowModelName);
        if (arrowModel == null)
        {
            Debug.LogError($"[3dHudArrow] 3D arrow model '{arrowModelName}' not found in scene!");
            return;
        }

        arrow3DTransform = arrowModel.transform;
        initialArrowLocalRotation = arrow3DTransform.localRotation;

        // Создать RenderTexture для рендеринга стрелки
        arrowRenderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
        arrowRenderTexture.Create();

        // Создать камеру для рендеринга стрелки
        GameObject cameraGO = new GameObject("ArrowRenderCamera");
        arrowCamera = cameraGO.AddComponent<Camera>();
        arrowCamera.targetTexture = arrowRenderTexture;
        arrowCamera.clearFlags = CameraClearFlags.SolidColor;
        arrowCamera.backgroundColor = arrowBackgroundColor;
        arrowCamera.orthographic = true;
        arrowCamera.orthographicSize = 1f;
        arrowCamera.nearClipPlane = 0.1f;
        arrowCamera.farClipPlane = 10f;

        // Позиционировать камеру для рендеринга стрелки
        arrowCamera.transform.position = new Vector3(0, 0, -1);
        arrowCamera.transform.LookAt(Vector3.zero);

        Debug.Log("[3dHudArrow] 3D arrow setup complete");
    }

    /// <summary>
    /// Основной цикл обновления 3D стрелки.
    /// Вычисляет bearing к цели, поворачивает стрелку и проверяет завершение маршрута.
    /// </summary>
    void Update()
    {
        if (sensors == null || !sensors.isReady)
        {
            Debug.Log($"[3dHudArrow] Sensors not ready: sensors={sensors}, isReady={sensors?.isReady}");
            return;
        }

        Vector2 pos = sensors.gpsLatLon;

        Vector2 desiredTarget = targetLatLon;

        // Если есть маршрут, используем направление вдоль маршрута
        // if (RouteSession.HasRoute)
        // {
        //     desiredTarget = GetNextWaypoint(pos);
        // }

        // Сглаживаем цель
        if (!initialized)
        {
            currentTarget = desiredTarget;
            smoothedCompassHeading = sensors.compassHeading;
            initialized = true;
        }
        else
        {
            currentTarget = Vector2.Lerp(currentTarget, desiredTarget, targetLerp);
            smoothedCompassHeading = Mathf.LerpAngle(smoothedCompassHeading, sensors.compassHeading, 0.1f);
        }

        float bearing = GeoUtils.BearingDeg(pos, currentTarget);
        float phoneHeading = smoothedCompassHeading;
        float relativeBearing = bearing - phoneHeading;
        relativeBearing = Mathf.Repeat(relativeBearing + 180, 360) - 180; // normalize to -180..180
        float newTargetZ = relativeBearing; // Use relative bearing for AR navigation

        // Если bearing скачет больше чем на jumpThresholdDeg, сразу прыгаем и начинаем интерполировать
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentZ, newTargetZ));
        if (angleDiff > jumpThresholdDeg)
        {
            currentZ = newTargetZ;
            Debug.Log($"[3dHudArrow] JUMP: bearing skipped {angleDiff:F1}° - jumping directly");
        }
        else
        {
            // Иначе интерполируем плавно
            currentZ = Mathf.LerpAngle(currentZ, newTargetZ, arrowLerp);
        }

        // Поворачиваем 3D-модель стрелки только по оси Z, сохраняя стартовую локальную ориентацию
        if (arrow3DTransform != null)
        {
            Vector3 baseEuler = initialArrowLocalRotation.eulerAngles;
            arrow3DTransform.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y, baseEuler.z + (currentZ + 180f) + zRotationOffset);
        }

        // Проверяем достижение финиша
        float distToFinish = GeoUtils.DistanceKm(pos, targetLatLon);
        if (distToFinish < finishThresholdKm)
        {
            Debug.Log($"[3dHudArrow] FINISH REACHED! Distance to end: {distToFinish * 1000:F0}m. Clearing route.");
            RouteSession.Clear(); // Сбрасываем маршрут
            // Скрываем стрелку
            if (arrow3DTransform != null)
            {
                arrow3DTransform.gameObject.SetActive(false);
            }
            // Отправляем сообщение в WebView
            if (webView != null)
            {
                string js = "if (typeof showFinishMessage === 'function') showFinishMessage();";
                webView.EvaluateJS(js);
            }
            Debug.Log("✓ Конец маршрута!");
        }

        // Логируем каждый кадр для точной диагностики
        if (Time.time % 0.5f < Time.deltaTime) // каждые 0.5 сек
        {
            float actual3DAngle = arrow3DTransform != null ? arrow3DTransform.localEulerAngles.z : 0f;
            Debug.Log($"[3dHudArrow] GPS Bearing Mode | pos={pos.x:F4},{pos.y:F4} | bearing={bearing:F1}° | phoneHeading={phoneHeading:F1}° | relativeBearing={relativeBearing:F1}° | currentZ(calc)={currentZ:F1}° | 3dZ(actual)={actual3DAngle:F1}° | " +
                      $"target={currentTarget.x:F4},{currentTarget.y:F4} | distToFinish={distToFinish * 1000:F0}m");
        }
    }

    /// <summary>
    /// Определяет следующий waypoint маршрута для навигации.
    /// Находит ближайшую точку маршрута и возвращает следующую за ней.
    /// </summary>
    /// <param name="currentPos">Текущая позиция пользователя.</param>
    /// <returns>Координаты следующего waypoint или конечной точки.</returns>
    private Vector2 GetNextWaypoint(Vector2 currentPos)
    {
        if (RouteSession.RouteCoords.Count == 0) return targetLatLon;

        // Найти индекс точки на маршруте, ближайшей к currentPos
        int bestIndex = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < RouteSession.RouteCoords.Count; i++)
        {
            float dist = GeoUtils.DistanceKm(currentPos, RouteSession.RouteCoords[i]);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        // Если далеко от маршрута, указывать к ближайшей точке
        if (bestDist > onRouteThresholdKm)
        {
            return RouteSession.RouteCoords[bestIndex];
        }

        // Если близко, указывать к следующей точке или к концу
        if (bestIndex >= RouteSession.RouteCoords.Count - 1)
        {
            return RouteSession.RouteCoords[RouteSession.RouteCoords.Count - 1];
        }

        return RouteSession.RouteCoords[bestIndex + 1];
    }

    /// <summary>
    /// Освобождает ресурсы при уничтожении объекта.
    /// Удаляет RenderTexture и камеру для предотвращения утечек памяти.
    /// </summary>
    private void OnDestroy()
    {
        if (arrowRenderTexture != null)
        {
            arrowRenderTexture.Release();
            Destroy(arrowRenderTexture);
        }
        if (arrowCamera != null)
        {
            Destroy(arrowCamera.gameObject);
        }
    }
}