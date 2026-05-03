using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сериализуемая версия Vector2 для сохранения в JSON.
/// Unity Vector2 не сериализуется напрямую, поэтому используем этот класс.
/// </summary>
[System.Serializable]
public class SerializableVector2
{
    public float x;
    public float y;

    public SerializableVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public Vector2 ToVector2() => new Vector2(x, y);
    public static SerializableVector2 FromVector2(Vector2 v) => new SerializableVector2(v.x, v.y);
}

/// <summary>
/// Структура данных для сериализации маршрута.
/// Содержит текстовые описания точек, координаты и список точек маршрута.
/// </summary>
[System.Serializable]
public class RouteData
{
    public string startText;
    public string endText;
    public double startLat;
    public double startLon;
    public double endLat;
    public double endLon;
    public List<SerializableVector2> routeCoords;
}

/// <summary>
/// Статический класс для управления сессией маршрута.
/// Хранит данные о маршруте в памяти и сохраняет/загружает из PlayerPrefs.
/// Используется для передачи маршрута между сценами и компонентами.
/// </summary>
public static class RouteSession
{
    // === КОНСТАНТЫ ===

    /// <summary>Ключ для сохранения маршрута в PlayerPrefs.</summary>
    private const string ROUTE_KEY = "SavedRoute";

    // === ДАННЫЕ МАРШРУТА ===

    /// <summary>Текстовое описание начальной точки.</summary>
    public static string StartText = "";

    /// <summary>Текстовое описание конечной точки.</summary>
    public static string EndText = "";

    /// <summary>Широта начальной точки.</summary>
    public static double StartLat = double.NaN;

    /// <summary>Долгота начальной точки.</summary>
    public static double StartLon = double.NaN;

    /// <summary>Широта конечной точки.</summary>
    public static double EndLat = double.NaN;

    /// <summary>Долгота конечной точки.</summary>
    public static double EndLon = double.NaN;

    /// <summary>Список координат точек маршрута.</summary>
    public static List<Vector2> RouteCoords = new List<Vector2>();

    // === СВОЙСТВА СОСТОЯНИЯ ===

    /// <summary>Есть ли координаты начальной точки.</summary>
    public static bool HasStartCoords => !double.IsNaN(StartLat) && !double.IsNaN(StartLon);

    /// <summary>Есть ли координаты конечной точки.</summary>
    public static bool HasEndCoords => !double.IsNaN(EndLat) && !double.IsNaN(EndLon);

    /// <summary>Есть ли загруженный маршрут.</summary>
    public static bool HasRoute => RouteCoords.Count > 0;

    /// <summary>
    /// Очищает все данные маршрута.
    /// Сбрасывает координаты, тексты и список точек.
    /// Также удаляет сохраненные данные из PlayerPrefs.
    /// </summary>
    public static void Clear()
    {
        StartText = "";
        EndText = "";
        StartLat = double.NaN;
        StartLon = double.NaN;
        EndLat = double.NaN;
        EndLon = double.NaN;
        RouteCoords.Clear();
        PlayerPrefs.DeleteKey(ROUTE_KEY);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Сохраняет текущий маршрут в PlayerPrefs.
    /// Конвертирует данные в сериализуемый формат и сохраняет как JSON.
    /// </summary>
    public static void SaveRoute()
    {
        if (!HasRoute) return;

        RouteData data = new RouteData
        {
            startText = StartText,
            endText = EndText,
            startLat = StartLat,
            startLon = StartLon,
            endLat = EndLat,
            endLon = EndLon,
            routeCoords = RouteCoords.ConvertAll(v => SerializableVector2.FromVector2(v))
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(ROUTE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("[RouteSession] Route saved to PlayerPrefs");
    }

    /// <summary>
    /// Загружает маршрут из PlayerPrefs.
    /// Десериализует JSON и восстанавливает данные маршрута.
    /// </summary>
    /// <returns>true если маршрут успешно загружен, false если данных нет или ошибка десериализации.</returns>
    public static bool LoadRoute()
    {
        if (!PlayerPrefs.HasKey(ROUTE_KEY)) return false;

        string json = PlayerPrefs.GetString(ROUTE_KEY);
        RouteData data = JsonUtility.FromJson<RouteData>(json);

        if (data == null) return false;

        StartText = data.startText;
        EndText = data.endText;
        StartLat = data.startLat;
        StartLon = data.startLon;
        EndLat = data.endLat;
        EndLon = data.endLon;
        RouteCoords = data.routeCoords.ConvertAll(sv => sv.ToVector2());

        Debug.Log("[RouteSession] Route loaded from PlayerPrefs, points: " + RouteCoords.Count);
        return true;
    }
}