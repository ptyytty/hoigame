using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[ExecuteAlways, RequireComponent(typeof(CinemachineVirtualCamera))]
public class CM_OrthoFit : MonoBehaviour
{
    public enum FitMode { ConstantWidth, ConstantHeight }

    [Header("Reference")]
    public float referenceAspect = 16f / 9f; // 기준 화면비
    public float referenceOrthoSize = 16f;   // 16:9에서의 사이즈(지금 값)

    [Header("Mode")]
    public FitMode fitMode = FitMode.ConstantWidth;

    CinemachineVirtualCamera vcam;
    int lastW, lastH;

    void OnEnable()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        EnsureOrtho();
        Apply(true);
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
            Apply();
    }
#endif

    void EnsureOrtho()
    {
        var lens = vcam.m_Lens;
        lens.Orthographic = true;
        vcam.m_Lens = lens;
    }

    public void Apply(bool force = false)
    {
        if (!vcam) return;
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0) return;
        if (!force && w == lastW && h == lastH) return;
        lastW = w; lastH = h;

        float curAspect = (float)w / h;

        switch (fitMode)
        {
            case FitMode.ConstantWidth:
                vcam.m_Lens.OrthographicSize = referenceOrthoSize * (referenceAspect / curAspect);
                break;
            case FitMode.ConstantHeight:
                vcam.m_Lens.OrthographicSize = referenceOrthoSize;
                break;
        }
    }

    void OnValidate()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();
        EnsureOrtho(); Apply(true);
    }
}
