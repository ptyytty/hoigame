using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [빌드 포함용] Outline 리소스를 강제로 로드시키는 클래스.
/// </summary>

public class OutlineResourceBinder : MonoBehaviour
{
    public Material outlineMat;
    public Shader outlineShader;

    void Awake()
    {
        // 강제 포함용 - 빌드 누락 방지
        if (!outlineMat)
            outlineMat = Resources.Load<Material>("Outline");

        if (outlineMat)
            Debug.Log($"[OutlineBinder] Outline mat found: {outlineMat.name}");
        else
            Debug.LogError("[OutlineBinder] Outline.mat not found in Resources!");

        if (!outlineShader)
            outlineShader = Shader.Find("Custom/Outline_Mobile_URP");

        if (outlineShader)
            Debug.Log($"[OutlineBinder] Shader found: {outlineShader.name}");
        else
            Debug.LogError("[OutlineBinder] Shader not found!");
    }
}
