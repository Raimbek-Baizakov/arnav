using System.Collections;
using UnityEngine;

public class SensorsService : MonoBehaviour
{
    public bool isReady { get; private set; }
    public Vector2 gpsLatLon { get; private set; }   // (lat, lon)
    public float headingDeg { get; private set; }    // 0..360, North=0

    [Header("Smoothing")]
    [Range(0.01f, 1f)] public float headingLerp = 0.15f;

    float smoothedHeading;

    IEnumerator Start()
    {
        // Compass
        Input.compass.enabled = true;

        // GPS
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("Location service disabled by user.");
            yield break;
        }

        Input.location.Start(desiredAccuracyInMeters: 5f, updateDistanceInMeters: 1f);

        float timeout = 10f;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError("Location service failed: " + Input.location.status);
            yield break;
        }

        smoothedHeading = Input.compass.trueHeading;
        isReady = true;
        Debug.Log("Sensors ready.");
    }

    void Update()
    {
        if (!isReady) return;

        var data = Input.location.lastData;
        gpsLatLon = new Vector2((float)data.latitude, (float)data.longitude);

        float rawHeading = Input.compass.trueHeading; // можно magneticHeading, но trueHeading лучше
        smoothedHeading = Mathf.LerpAngle(smoothedHeading, rawHeading, headingLerp);
        headingDeg = smoothedHeading;
    }
}