using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Globalization;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Контроллер веб-представления для отображения карты маршрута.
/// Управляет WebView компонентом для показа интерактивной карты,
/// обрабатывает сообщения от JavaScript и синхронизирует данные маршрута.
/// </summary>
public class RouteMapWebViewController : MonoBehaviour
{
    // === НАСТРОЙКИ ===

    /// <summary>Название сцены камеры для перехода.</summary>
    [Header("UI")]
    public string cameraScene = "CameraScene";

    // === КОМПОНЕНТЫ ===

    /// <summary>Ссылка на компонент WebView для отображения карты.</summary>
    private WebViewObject webView;

    /// <summary>
    /// Инициализирует WebView и настраивает карту маршрута.
    /// Проверяет на дубликаты экземпляров, создает WebView компонент,
    /// загружает HTML карту и подписывается на события сцен.
    /// </summary>
    void Start()
    {
        Debug.Log($"[ROUTE MAP] Start() called in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // Проверяем, есть ли уже экземпляр
        if (FindObjectOfType<RouteMapWebViewController>() != this)
        {
            Debug.Log("[ROUTE MAP] Another instance found, destroying this one");
            Destroy(gameObject);
            return;
        }

        // НЕ сохраняем объект между сценами - каждая сцена должна быть независимой
        // DontDestroyOnLoad(gameObject); // УБРАНО

        Debug.Log("[ROUTE MAP] Creating WebView for map display");
        webView = gameObject.AddComponent<WebViewObject>();

        webView.Init(
            cb: (msg) =>
            {
                var decoded = System.Uri.UnescapeDataString(msg);
                Debug.Log("[ROUTE MAP] msg: " + decoded);
                HandleMessage(decoded);
            },
            err: (e) => Debug.LogError("[ROUTE MAP] error: " + e),
            ld: (url) =>
            {
                Debug.Log("[ROUTE MAP] loaded: " + url);
                TryRestoreRoute();
                StartCoroutine(SendCompassHeadingLoop());
            },
            started: (url) => Debug.Log("[ROUTE MAP] started: " + url)
        );

        webView.SetVisibility(true);
        webView.SetMargins(0, 0, 0, 0);

        Input.compass.enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(LoadMapHtmlFromStreamingAssets());
#else
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "map.html");
        webView.LoadURL("file://" + path);
#endif

        // Подписываемся на события загрузки сцен
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Подписывается на события загрузки сцен при включении объекта.</summary>
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>Отписывается от событий загрузки сцен при отключении объекта.</summary>
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Обработчик события загрузки сцены.
    /// Логирует переход между сценами без дополнительных действий с WebView.
    /// </summary>
    /// <param name="scene">Загруженная сцена.</param>
    /// <param name="mode">Режим загрузки сцены.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Просто переходим между сценами, не трогаем WebView
        Debug.Log($"[ROUTE MAP] Scene loaded: {scene.name}");
    }

    /// <summary>
    /// Загружает HTML файл карты из StreamingAssets на Android.
    /// Использует UnityWebRequest для чтения файла и загрузки в WebView.
    /// </summary>
    private IEnumerator LoadMapHtmlFromStreamingAssets()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "map.html");
        var req = UnityWebRequest.Get(path);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[ROUTE MAP] Failed to load map.html: " + req.error);
            yield break;
        }

        string html = req.downloadHandler.text;
        webView.LoadHTML(html, "https://localhost/");
    }

    /// <summary>
    /// Пытается восстановить маршрут на карте из сохраненных данных RouteSession.
    /// Отправляет JavaScript команду для восстановления маршрута если есть координаты.
    /// </summary>
    private void TryRestoreRoute()
    {
        if (!RouteSession.HasStartCoords || !RouteSession.HasEndCoords)
            return;

        var payload = new RestorePayload
        {
            start = new RestorePoint
            {
                lat = RouteSession.StartLat,
                lon = RouteSession.StartLon,
                address = RouteSession.StartText
            },
            end = new RestorePoint
            {
                lat = RouteSession.EndLat,
                lon = RouteSession.EndLon,
                address = RouteSession.EndText
            }
        };

        string json = JsonUtility.ToJson(payload);
        string js = $"if (typeof restoreRouteFromUnity === 'function') restoreRouteFromUnity({json});";
        webView.EvaluateJS(js);
    }

    /// <summary>
    /// Цикл отправки данных компаса и GPS в WebView.
    /// Постоянно обновляет карту текущим положением и направлением пользователя.
    /// </summary>
    private IEnumerator SendCompassHeadingLoop()
    {
        while (webView != null)
        {
            // Отправляем GPS позицию для обновления маршрута
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var data = Input.location.lastData;
                string gpsJs = $"if (typeof updateGpsFromUnity === 'function') updateGpsFromUnity({data.latitude.ToString("F6", CultureInfo.InvariantCulture)}, {data.longitude.ToString("F6", CultureInfo.InvariantCulture)});";
                webView.EvaluateJS(gpsJs);
            }

            // Отправляем направление, если компас доступен
            if (Input.compass.enabled)
            {
                float heading = Input.compass.trueHeading;
                if (!float.IsNaN(heading))
                {
                    string headingJs = $"if (typeof updateHeadingFromUnity === 'function') updateHeadingFromUnity({heading.ToString("F1", CultureInfo.InvariantCulture)});";
                    webView.EvaluateJS(headingJs);
                }
            }

            yield return new WaitForSeconds(0.2f); // Обновление каждые 0.2 секунды
        }
    }

    /// <summary>
    /// Обрабатывает сообщения от JavaScript карты.
    /// Парсит JSON сообщения и выполняет соответствующие действия:
    /// - "reset": очищает маршрут
    /// - "done": сохраняет выбранный маршрут и переходит в сцену камеры
    /// </summary>
    /// <param name="json">JSON строка сообщения от карты.</param>
    private void HandleMessage(string json)
    {
        MapMsg data;
        try
        {
            data = JsonUtility.FromJson<MapMsg>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[ROUTE MAP] JSON parse error: " + ex);
            return;
        }

        if (data == null || string.IsNullOrEmpty(data.type))
            return;

        // Логируем все входящие сообщения
        Scene activeScene = SceneManager.GetActiveScene();
        Debug.Log($"[ROUTE MAP] Received '{data.type}' on scene '{activeScene.name}'");

        if (data.type == "reset")
        {
            RouteSession.Clear();
            return;
        }

        if (data.type == "done" && data.start != null && data.end != null)
        {
            Debug.Log($"[ROUTE MAP] Processing 'done' message: start={data.start.lat},{data.start.lon}, end={data.end.lat},{data.end.lon}");

            // Берём адреса (если не пришли — тогда fallback на координаты)
            string startText = !string.IsNullOrEmpty(data.start.address)
                ? data.start.address
                : $"{data.start.lat:F6},{data.start.lon:F6}";

            string endText = !string.IsNullOrEmpty(data.end.address)
                ? data.end.address
                : $"{data.end.lat:F6},{data.end.lon:F6}";

            // Сохраняем в сессию
            RouteSession.StartText = startText;
            RouteSession.EndText = endText;
            RouteSession.StartLat = data.start.lat;
            RouteSession.StartLon = data.start.lon;
            RouteSession.EndLat = data.end.lat;
            RouteSession.EndLon = data.end.lon;

            // Сохраняем маршрут
            RouteSession.RouteCoords.Clear();
            if (data.route != null)
            {
                var originalRoute = data.route.Select(p => new Vector2((float)p.lat, (float)p.lon)).ToList();
                RouteSession.RouteCoords = InterpolateRoute(originalRoute, 10f); // интерполируем каждые 10 метров
                Debug.Log($"[ROUTE MAP] Route saved: {RouteSession.RouteCoords.Count} points (from {originalRoute.Count} original)");
            }

            // Сохраняем маршрут в PlayerPrefs для независимости от WebView
            RouteSession.SaveRoute();

            Debug.Log($"[ROUTE MAP] Loading scene: {cameraScene} (current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name})");
            // Переходим в сцену камеры сразу после выбора маршрута
            SceneManager.LoadScene(cameraScene);
            return;
        }

        if (data.type == "update_route" && data.route != null)
        {
            // Обновляем маршрут в сессии
            var originalRoute = data.route.Select(p => new Vector2((float)p.lat, (float)p.lon)).ToList();
            RouteSession.RouteCoords = InterpolateRoute(originalRoute, 10f); // интерполируем каждые 10 метров
            Debug.Log($"[ROUTE MAP] Route updated with {RouteSession.RouteCoords.Count} points (interpolated from {originalRoute.Count})");

            // Сохраняем обновлённый маршрут
            RouteSession.SaveRoute();
            return;
        }

        // Обработчик set_heading убран - перешли на GPS bearing only
        // Обработчик test_alive убран - compass отключен
    }

    /// <summary>
    /// Очищает ресурсы при уничтожении объекта.
    /// Отписывается от событий сцен и уничтожает WebView компонент.
    /// </summary>
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[ROUTE MAP] OnDestroy - cleaning up WebView and preventing cross-scene persistence");
        if (webView != null)
        {
            webView.SetVisibility(false);
            Destroy(webView);
            webView = null;
        }
    }

    /// <summary>Точка маршрута для восстановления (сериализуемая).</summary>
    [System.Serializable]
    private class RestorePoint
    {
        public double lat;
        public double lon;
        public string address;
    }

    /// <summary>Полезная нагрузка для восстановления маршрута.</summary>
    [System.Serializable]
    private class RestorePayload
    {
        public RestorePoint start;
        public RestorePoint end;
    }

    /// <summary>Координаты с адресом для сообщений карты.</summary>
    [System.Serializable]
    private class LatLon
    {
        public double lat;
        public double lon;
        public string address;
    }

    /// <summary>Сообщение от JavaScript карты.</summary>
    [System.Serializable]
    private class MapMsg
    {
        public string type;
        public LatLon start;
        public LatLon end;
        public List<LatLon> route;
        public double heading; // для set_heading (убран)
    }

    /// <summary>
    /// Устанавливает правый отступ WebView (не используется, всегда полноэкранный).
    /// </summary>
    /// <param name="px">Отступ в пикселях (игнорируется).</param>
    public void SetRightMargin(int px)
    {
        // Right Panel removed, margins always 0 for full screen
        if (webView == null) return;
        webView.SetMargins(0, 0, 0, 0);
    }

    /// <summary>
    /// Интерполирует маршрут, добавляя точки каждые stepKm километров.
    /// Увеличивает плотность точек маршрута для более точной навигации.
    /// </summary>
    /// <param name="originalRoute">Оригинальный список точек маршрута.</param>
    /// <param name="stepKm">Расстояние между точками в километрах.</param>
    /// <returns>Интерполированный список точек маршрута.</returns>
    private List<Vector2> InterpolateRoute(List<Vector2> originalRoute, float stepKm)
    {
        if (originalRoute.Count < 2) return originalRoute;

        List<Vector2> interpolated = new List<Vector2>();
        interpolated.Add(originalRoute[0]);

        for (int i = 0; i < originalRoute.Count - 1; i++)
        {
            Vector2 start = originalRoute[i];
            Vector2 end = originalRoute[i + 1];
            float segmentDistance = GeoUtils.DistanceKm(start, end);

            if (segmentDistance <= stepKm)
            {
                // Если сегмент короткий, добавляем конец
                interpolated.Add(end);
            }
            else
            {
                // Добавляем промежуточные точки
                int numSteps = Mathf.CeilToInt(segmentDistance / stepKm);
                for (int j = 1; j <= numSteps; j++)
                {
                    float t = (float)j / numSteps;
                    Vector2 point = Vector2.Lerp(start, end, t);
                    interpolated.Add(point);
                }
            }
        }

        return interpolated;
    }
}