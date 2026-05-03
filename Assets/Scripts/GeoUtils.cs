using UnityEngine;

/// <summary>
/// Утилиты для географических расчетов.
/// Содержит функции для расчета расстояний, азимутов и углов на сфере Земли.
/// Использует формулу гаверсинуса для точных расчетов на больших расстояниях.
/// </summary>
public static class GeoUtils
{
    /// <summary>
    /// Вычисляет расстояние в метрах между двумя точками на сфере Земли.
    /// Использует формулу гаверсинуса для высокой точности.
    /// </summary>
    /// <param name="aLatLon">Координаты первой точки (широта, долгота) в градусах.</param>
    /// <param name="bLatLon">Координаты второй точки (широта, долгота) в градусах.</param>
    /// <returns>Расстояние в метрах.</returns>
    public static float HaversineMeters(Vector2 aLatLon, Vector2 bLatLon)
    {
        const float R = 6371000f; // Радиус Земли в метрах
        float lat1 = aLatLon.x * Mathf.Deg2Rad;
        float lat2 = bLatLon.x * Mathf.Deg2Rad;
        float dLat = (bLatLon.x - aLatLon.x) * Mathf.Deg2Rad;
        float dLon = (bLatLon.y - aLatLon.y) * Mathf.Deg2Rad;

        float sinDLat = Mathf.Sin(dLat * 0.5f);
        float sinDLon = Mathf.Sin(dLon * 0.5f);

        float h = sinDLat * sinDLat + Mathf.Cos(lat1) * Mathf.Cos(lat2) * sinDLon * sinDLon;
        float c = 2f * Mathf.Atan2(Mathf.Sqrt(h), Mathf.Sqrt(1f - h));
        return R * c;
    }

    /// <summary>
    /// Вычисляет азимут (bearing) от точки A к точке B в градусах.
    /// 0° = Север, 90° = Восток, 180° = Юг, 270° = Запад.
    /// </summary>
    /// <param name="aLatLon">Координаты начальной точки (широта, долгота) в градусах.</param>
    /// <param name="bLatLon">Координаты конечной точки (широта, долгота) в градусах.</param>
    /// <returns>Азимут в градусах (0-360).</returns>
    public static float BearingDeg(Vector2 aLatLon, Vector2 bLatLon)
    {
        float lat1 = aLatLon.x * Mathf.Deg2Rad;
        float lat2 = bLatLon.x * Mathf.Deg2Rad;
        float dLon = (bLatLon.y - aLatLon.y) * Mathf.Deg2Rad;

        float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
        float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) - Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);
        float brng = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        return (brng + 360f) % 360f; // Нормализация в диапазон 0-360
    }

    /// <summary>
    /// Вычисляет расстояние в километрах между двумя точками.
    /// Использует HaversineMeters и конвертирует результат в километры.
    /// </summary>
    /// <param name="aLatLon">Координаты первой точки (широта, долгота) в градусах.</param>
    /// <param name="bLatLon">Координаты второй точки (широта, долгота) в градусах.</param>
    /// <returns>Расстояние в километрах.</returns>
    public static float DistanceKm(Vector2 aLatLon, Vector2 bLatLon)
    {
        return HaversineMeters(aLatLon, bLatLon) / 1000f;
    }

    /// <summary>
    /// Вычисляет минимальный угол поворота между двумя направлениями.
    /// Возвращает значение в диапазоне -180° до +180°.
    /// Положительное значение означает поворот по часовой стрелке.
    /// </summary>
    /// <param name="fromDeg">Начальный угол в градусах.</param>
    /// <param name="toDeg">Конечный угол в градусах.</param>
    /// <returns>Угол поворота в градусах (-180 до +180).</returns>
    public static float DeltaAngleSigned(float fromDeg, float toDeg)
    {
        return Mathf.DeltaAngle(fromDeg, toDeg);
    }
}