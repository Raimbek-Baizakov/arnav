using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class MapWebViewController : MonoBehaviour
{
    public RoutePanelController routePanel;
    public string htmlFileName = "map.html";

    private WebViewObject webView;

    [System.Serializable]
    public class BridgePoint
    {
        public double lat;
        public double lon;
        public string address;
    }

    [System.Serializable]
    public class BridgeMsg
    {
        public string type;
        public BridgePoint start;
        public BridgePoint end;
    }

    void Start()
    {
        webView = gameObject.AddComponent<WebViewObject>();

        webView.Init(
            cb: (msg) =>
            {
                var decoded = System.Uri.UnescapeDataString(msg);
                Debug.Log("[WebView] msg: " + decoded);
                HandleMessage(decoded);
            },
            err: (e) => Debug.LogError("[WebView] error: " + e),
            started: (url) => Debug.Log("[WebView] started: " + url),
            hooked: (url) => { }
        );

        // Чтобы WebView не перекрывал правую панель:
        int rightPanelPx = Mathf.RoundToInt(Screen.width * 0.30f);
        webView.SetMargins(0, 0, rightPanelPx, 0);

        webView.SetVisibility(true);
        StartCoroutine(LoadHtmlFromStreamingAssets());
    }

    private IEnumerator LoadHtmlFromStreamingAssets()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, htmlFileName);

        // Android
        if (path.Contains("://") || path.Contains(":///"))
        {
            using (UnityWebRequest www = UnityWebRequest.Get(path))
            {
                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[WebView] Failed to load HTML: " + www.error);
                    yield break;
                }
                webView.LoadHTML(www.downloadHandler.text, "file://");
            }
        }
        else
        {
            // Editor/Windows
            webView.LoadURL("file:///" + path);
        }
    }

    private void HandleMessage(string decodedJson)
    {
        BridgeMsg m = null;

        try
        {
            m = JsonUtility.FromJson<BridgeMsg>(decodedJson);
        }
        catch
        {
            Debug.LogWarning("[WebView] JSON parse failed.");
            return;
        }

        if (m == null || string.IsNullOrEmpty(m.type))
            return;

        if (routePanel == null)
            routePanel = FindObjectOfType<RoutePanelController>();

        if (m.type == "done")
        {
            // берём адреса из HTML
            string startAddr = m.start != null ? m.start.address : "";
            string endAddr   = m.end != null ? m.end.address : "";

            if (routePanel != null)
                routePanel.SetAddresses(startAddr, endAddr);
            else
                Debug.LogWarning("RoutePanelController not found in scene.");

            return;
        }

        if (m.type == "reset")
        {
            if (routePanel != null) routePanel.ResetFields();
            return;
        }

        // Остальные типы (gps и т.п.) можно обработать позже
    }
}