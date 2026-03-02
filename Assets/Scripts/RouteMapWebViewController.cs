using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

public class RouteMapWebViewController : MonoBehaviour
{
    [Header("UI")]
    public RoutePanelController routePanel; // перетащи RightPanel сюда в инспекторе

    private WebViewObject webView;

    void Start()
    {
        webView = gameObject.AddComponent<WebViewObject>();

        webView.Init(
            cb: (msg) =>
            {
                var decoded = System.Uri.UnescapeDataString(msg);
                Debug.Log("[ROUTE MAP] msg: " + decoded);
                HandleMessage(decoded);
            },
            err: (e) => Debug.LogError("[ROUTE MAP] error: " + e),
            started: (url) => Debug.Log("[ROUTE MAP] started: " + url)
        );

        webView.SetVisibility(true);
        webView.SetMargins(0, 0, 500, 0);

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(LoadMapHtmlFromStreamingAssets());
#else
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "map.html");
        webView.LoadURL("file://" + path);
#endif
    }

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

        if (routePanel == null)
            routePanel = FindObjectOfType<RoutePanelController>();

        if (data.type == "reset")
        {
            RouteSession.Clear();
            if (routePanel != null) routePanel.SetAddresses("", "");
            return;
        }

        if (data.type == "done" && data.start != null && data.end != null)
        {
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

            // ОБНОВЛЯЕМ UI СРАЗУ
            if (routePanel != null)
                routePanel.SetAddresses(RouteSession.StartText, RouteSession.EndText);
            else
                Debug.LogWarning("[ROUTE MAP] RoutePanelController not found.");

            return;
        }
    }

    void OnDestroy()
    {
        if (webView != null)
            webView.SetVisibility(false);
    }

    [System.Serializable]
    private class LatLon
    {
        public double lat;
        public double lon;
        public string address; // <-- ВАЖНО: добавили
    }

    [System.Serializable]
    private class MapMsg
    {
        public string type;
        public LatLon start;
        public LatLon end;
    }

    public void SetRightMargin(int px)
    {
        if (webView == null) return;
        webView.SetMargins(0, 0, px, 0);
    }
}