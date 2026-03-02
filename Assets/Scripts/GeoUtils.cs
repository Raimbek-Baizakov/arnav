using UnityEngine;

public static class GeoUtils
{
    // Возвращает расстояние в метрах между двумя точками (lat/lon)
    public static float HaversineMeters(Vector2 aLatLon, Vector2 bLatLon)
    {
        const float R = 6371000f;
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

    // Bearing (азимут) от A к B в градусах: 0=North, 90=East
    public static float BearingDeg(Vector2 aLatLon, Vector2 bLatLon)
    {
        float lat1 = aLatLon.x * Mathf.Deg2Rad;
        float lat2 = bLatLon.x * Mathf.Deg2Rad;
        float dLon = (bLatLon.y - aLatLon.y) * Mathf.Deg2Rad;

        float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
        float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) - Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);
        float brng = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        return (brng + 360f) % 360f;
    }

    // Нормализуем угол в диапазон -180..180
    public static float DeltaAngleSigned(float fromDeg, float toDeg)
    {
        return Mathf.DeltaAngle(fromDeg, toDeg);
    }
}