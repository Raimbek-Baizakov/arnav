using System.Collections;
using UnityEngine;

public class RightPanelController : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform panel;                  // RightPanel RectTransform
    public RectTransform content;                // Content RectTransform
    public CanvasGroup contentGroup;             // CanvasGroup на Content
    public RouteMapWebViewController mapWebView; // WebViewRoot -> RouteMapWebViewController

    [Header("Widths (UI units)")]
    public float expandedWidth = 520f;
    public float collapsedWidth = 90f;

    [Header("Animation")]
    public float animTime = 0.18f;

    private bool isExpanded = true;
    private Coroutine animCo;
    private Canvas _canvas;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (contentGroup == null && content != null)
            contentGroup = content.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        ApplyInstant(isExpanded);
    }

    public void Toggle()
    {
        SetExpanded(!isExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimateWidth(expanded ? expandedWidth : collapsedWidth));
    }

    private void ApplyInstant(bool expanded)
    {
        float w = expanded ? expandedWidth : collapsedWidth;
        SetPanelWidth(w);
        ApplyMarginPx(w);
        SetContentVisible(expanded);
    }

    private IEnumerator AnimateWidth(float targetW)
    {
        float startW = panel.rect.width;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / animTime;
            float w = Mathf.Lerp(startW, targetW, Smooth(t));

            SetPanelWidth(w);
            ApplyMarginPx(w);

            yield return null;
        }

        SetPanelWidth(targetW);
        ApplyMarginPx(targetW);
        SetContentVisible(isExpanded);
    }

    private void SetPanelWidth(float w)
    {
        var size = panel.sizeDelta;
        size.x = w;
        panel.sizeDelta = size;
    }

    private void ApplyMarginPx(float panelWidthUiUnits)
    {
        float scale = (_canvas != null) ? _canvas.scaleFactor : 1f;
        int px = Mathf.RoundToInt(panelWidthUiUnits * scale);
        mapWebView?.SetRightMargin(px);
    }

    private void SetContentVisible(bool visible)
    {
        if (contentGroup == null) return;

        contentGroup.alpha = visible ? 1f : 0f;
        contentGroup.interactable = visible;
        contentGroup.blocksRaycasts = visible;
    }

    private float Smooth(float x) => x * x * (3f - 2f * x);
}