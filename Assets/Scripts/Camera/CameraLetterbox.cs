using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraLetterbox : MonoBehaviour
{
    [Tooltip("고정 유지할 기준 화면비 (예: 16:9 -> 16f/9f)")]
    public float referenceAspect = 16f / 9f;

    private Camera cam;
    private int lastW, lastH;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        ApplyRect(true);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
    }

#if UNITY_EDITOR
    void EditorUpdate()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            ApplyRect();
    }
#endif

    public void ApplyRect(bool force = false)
    {
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0) return;
        if (!force && (w == lastW && h == lastH)) return;
        lastW = w; lastH = h;

        float currentAspect = (float)w / h;
        cam.rect = new Rect(0, 0, 1, 1);

        float scale = currentAspect / referenceAspect;
        if (scale < 1f)
        {
            // 더 좁음(세로 김) → 상/하 여백
            cam.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
        }
        else
        {
            // 더 넓음 → 좌/우 여백
            float inv = 1f / scale;
            cam.rect = new Rect((1f - inv) * 0.5f, 0f, inv, 1f);
        }
    }

    void OnValidate() => ApplyRect(true);
}
