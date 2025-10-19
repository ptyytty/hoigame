using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rect;
    Rect lastSafe; Vector2Int lastSize;

    void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafe || lastSize.x != Screen.width || lastSize.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        lastSafe = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);

        Rect s = Screen.safeArea;
        Vector2 min = s.position, max = s.position + s.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;

        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }
}
