using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Контроллер HUD (Heads-Up Display) для отображения навигационной информации в AR-режиме.
/// Отвечает за обновление текстовых элементов с данными о скорости, расстоянии до поворота и до финиша.
/// Работает в паре с SensorsService для получения GPS и компасных данных.
/// </summary>
public class HudArrowController : MonoBehaviour
{
    // === ССЫЛКИ НА КОМПОНЕНТЫ ===

    /// <summary>Ссылка на сервис датчиков для получения GPS, скорости и ориентации устройства.</summary>
    [Header("Refs")]
    public SensorsService sensors;

    /// <summary>Ссылка на WebView для отправки сообщений о завершении маршрута.</summary>
    private WebViewObject webView;

    // === ЭЛЕМЕНТЫ HUD ===

    /// <summary>Текст для отображения текущей скорости движения (км/ч).</summary>
    [Header("HUD Display")]
    public TextMeshProUGUI speedText;

    /// <summary>Текст для отображения расстояния до следующего поворота маршрута.</summary>
    public TextMeshProUGUI distanceToTurnText;

    /// <summary>Текст для отображения оставшегося расстояния до конечной точки маршрута.</summary>
    public TextMeshProUGUI remainingDistanceText;

    // === НАСТРОЙКИ ЦЕЛИ ===

    /// <summary>Координаты целевой точки (широта, долгота) для навигации.</summary>
    [Header("Target")]
    public Vector2 targetLatLon;

    /// <summary>Использовать ли координаты из RouteSession вместо ручных настроек.</summary>
    public bool useRouteSessionTarget = true;

    // === НАСТРОЙКИ НАВИГАЦИИ ===

    /// <summary>Порог расстояния (км) для определения, находится ли пользователь на маршруте.</summary>
    [Header("Route navigation")]
    public float onRouteThresholdKm = 0.02f; // 20 метров для пешехода

    /// <summary>Порог расстояния (км) до финиша для автоматического завершения маршрута.</summary>
    public float finishThresholdKm = 0.05f; // 50 метров до финиша

    /// <summary>Коэффициент сглаживания для интерполяции целевой точки (0.01-1.0).</summary>
    [Range(0.01f, 1f)] public float targetLerp = 0.2f; // сглаживание цели

    // === ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ===

    /// <summary>Текущая сглаженная цель для навигации.</summary>
    Vector2 currentTarget;

    /// <summary>Флаг инициализации для первого кадра.</summary>
    bool initialized;

    /// <summary>
    /// Инициализация контроллера HUD.
    /// Загружает маршрут из PlayerPrefs, настраивает цель и находит WebView для коммуникации.
    /// </summary>
    void Start()
    {
        // Загружаем маршрут из PlayerPrefs, если он сохранён и не загружен
        if (!RouteSession.HasRoute)
        {
            RouteSession.LoadRoute();
        }

        Debug.Log($"[HudArrow] Start: useRouteSessionTarget={useRouteSessionTarget}, HasEndCoords={RouteSession.HasEndCoords}, HasRoute={RouteSession.HasRoute}");

        // Устанавливаем цель из RouteSession или используем ручные настройки
        if (useRouteSessionTarget && RouteSession.HasEndCoords)
        {
            targetLatLon = new Vector2((float)RouteSession.EndLat, (float)RouteSession.EndLon);
            Debug.Log($"[HudArrow] Target from RouteSession: {targetLatLon.x:F6},{targetLatLon.y:F6}");
        }
        else
        {
            Debug.Log($"[HudArrow] Using manual target: {targetLatLon.x:F6},{targetLatLon.y:F6}");
        }

        // Находим WebView для отправки сообщений о завершении
        var webViewController = FindObjectOfType<RouteMapWebViewController>();
        if (webViewController != null)
        {
            webView = webViewController.GetComponent<WebViewObject>();
        }

        // Сохраняем объект между сценами, чтобы HUD был в CameraScene
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Основной цикл обновления HUD.
    /// Выполняется каждый кадр для обновления навигационных данных и проверки завершения маршрута.
    /// </summary>
    void Update()
    {
        if (sensors == null || !sensors.isReady)
        {
            Debug.Log($"[HudArrow] Sensors not ready: sensors={sensors}, isReady={sensors?.isReady}");
            return;
        }

        Vector2 pos = sensors.gpsLatLon;

        Vector2 desiredTarget = targetLatLon;

        if (RouteSession.HasRoute)
        {
            desiredTarget = GetNextWaypoint(pos);
        }

        if (!initialized)
        {
            currentTarget = desiredTarget;
            initialized = true;
        }
        else
        {
            currentTarget = Vector2.Lerp(currentTarget, desiredTarget, targetLerp);
        }

        float distToFinish = GeoUtils.DistanceKm(pos, targetLatLon);
        if (distToFinish < finishThresholdKm)
        {
            Debug.Log($"[HudArrow] FINISH REACHED! Distance to end: {distToFinish * 1000:F0}m. Clearing route.");
            RouteSession.Clear();
            if (webView != null)
            {
                string js = "if (typeof showFinishMessage === 'function') showFinishMessage();";
                webView.EvaluateJS(js);
            }
            Debug.Log("✓ Конец маршрута!");
        }

        UpdateHUD(pos, distToFinish);
    }

    /// <summary>
    /// Обновляет текстовые элементы HUD с актуальными данными.
    /// </summary>
    /// <param name="pos">Текущая позиция пользователя (широта, долгота).</param>
    /// <param name="distToFinish">Расстояние до финиша в км.</param>
    private void UpdateHUD(Vector2 pos, float distToFinish)
    {
        float speed = sensors.gpsSpeed;
        if (speedText != null)
        {
            speedText.text = $"{speed * 3.6f:F1} км/ч";
        }

        if (remainingDistanceText != null)
        {
            if (distToFinish < 1)
                remainingDistanceText.text = $"{distToFinish * 1000:F0} м";
            else
                remainingDistanceText.text = $"{distToFinish:F2} км";
        }

        if (distanceToTurnText != null)
        {
            if (RouteSession.HasRoute && RouteSession.RouteCoords.Count > 0)
            {
                Vector2 nextWaypoint = GetNextWaypoint(pos);
                float distToNext = GeoUtils.DistanceKm(pos, nextWaypoint);
                if (distToNext < 1)
                    distanceToTurnText.text = $"{distToNext * 1000:F0} м";
                else
                    distanceToTurnText.text = $"{distToNext:F2} км";
            }
            else
            {
                distanceToTurnText.text = "—";
            }
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

        if (bestDist > onRouteThresholdKm)
        {
            return RouteSession.RouteCoords[bestIndex];
        }

        if (bestIndex >= RouteSession.RouteCoords.Count - 1)
        {
            return RouteSession.RouteCoords[RouteSession.RouteCoords.Count - 1];
        }

        return RouteSession.RouteCoords[bestIndex + 1];
    }
}