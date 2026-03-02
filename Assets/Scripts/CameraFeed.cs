using UnityEngine;
using UnityEngine.UI;

public class CameraFeed : MonoBehaviour
{
    public RawImage rawImage;

    [Header("Editor only (PC): pick Iriun if available")]
    public string editorPreferredSubstring = "Iriun";

    [Header("Android: prefer back camera")]
    public bool androidPreferBackCamera = true;

    private WebCamTexture camTexture;

    void Start()
    {
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("No camera devices found.");
            return;
        }

        Debug.Log("=== Cameras detected ===");
        foreach (var d in devices) Debug.Log($"Cam: {d.name}, frontFacing={d.isFrontFacing}");

        string chosen = devices[0].name;

#if UNITY_ANDROID && !UNITY_EDITOR
        // На телефоне: выбираем заднюю камеру
        if (androidPreferBackCamera)
        {
            foreach (var d in devices)
            {
                if (!d.isFrontFacing)
                {
                    chosen = d.name;
                    break;
                }
            }
        }
#else
        // В Editor: выбираем Iriun если есть
        if (!string.IsNullOrEmpty(editorPreferredSubstring))
        {
            foreach (var d in devices)
            {
                if (d.name.ToLower().Contains(editorPreferredSubstring.ToLower()))
                {
                    chosen = d.name;
                    break;
                }
            }
        }
#endif

        Debug.Log("Using camera: " + chosen);

        camTexture = new WebCamTexture(chosen, 1280, 720, 30);
        rawImage.texture = camTexture;
        camTexture.Play();
    }

    void OnDisable()
    {
        if (camTexture != null && camTexture.isPlaying)
            camTexture.Stop();
    }
}