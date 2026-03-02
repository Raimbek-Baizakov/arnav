using UnityEngine;
using UnityEngine.UI;

public class HudArrowController : MonoBehaviour
{
    [Header("Refs")]
    public SensorsService sensors;
    public RectTransform arrowRect; // UI стрелка

    [Header("Target")]
    public Vector2 targetLatLon; // временно зададим вручную в инспекторе

    [Header("Rotation smoothing")]
    [Range(0.01f, 1f)] public float arrowLerp = 0.2f;

    float currentZ;

    void Update()
    {
        if (sensors == null || !sensors.isReady) return;

        Vector2 pos = sensors.gpsLatLon;
        float heading = sensors.headingDeg;

        float bearing = GeoUtils.BearingDeg(pos, targetLatLon);
        float delta = GeoUtils.DeltaAngleSigned(heading, bearing); // куда повернуться относительно взгляда

        // UI: поворот по Z (в Canvas)
        currentZ = Mathf.LerpAngle(currentZ, -delta, arrowLerp); // минус часто нужен из-за оси UI
        arrowRect.localEulerAngles = new Vector3(0, 0, currentZ);
    }
}