using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// [모바일/빌드 안전] 아웃라인 셰이더/머티리얼을 런타임에서 일관되게 제공.
/// - Shader stripping에 대비해 Shader.Find 경로를 강제 참조.
/// - Resources(Materials/M_Outline) 백업 경로도 지원.
/// - 프로젝트 Settings에 의존성을 최소화(에디터 OK, 빌드 OK).
/// </summary>
public static class OutlineMaterialProvider
{
    // ✅ 셰이더 이름: 반드시 셰이더 파일의 상단 경로와 동일해야 함
    const string kShaderPath = "Custom/Outline_Mobile_URP";
    // ✅ Resources 백업 머티리얼 경로(있으면 빌드 포함을 보장)
    const string kResMatPath = "Materials/New_Outline"; // Resources/Materials/M_Outline.mat

    static Material _shared; // 역할: 공유 원본(서브메시별 인스턴스는 CreateInstance에서 생성)

    /// <summary>
    /// 역할: 런타임 시작 전에 셰이더/머티리얼 확보 시도(+검증 로그).
    /// </summary>
    [Preserve, RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Warmup()
    {
        if (_shared) return;

        // 1) Shader.Find 시도 — 빌드에서 스트립되면 null일 수 있음
        var sh = Shader.Find(kShaderPath);
        if (sh != null)
        {
            _shared = new Material(sh);
            Debug.Log($"[OutlinePROV] Shader.Find OK: {kShaderPath}");
            return;
        }
        Debug.LogWarning($"[OutlinePROV] Shader.Find FAIL: {kShaderPath} (빌드에서 스트립 가능성). Resources 백업을 시도합니다.");

        // 2) Resources 백업 — 프로젝트에 Resources/Materials/M_Outline.mat 생성 필요
        var resMat = Resources.Load<Material>(kResMatPath);
        if (resMat != null)
        {
            _shared = new Material(resMat); // 인스턴스화
            Debug.Log($"[OutlinePROV] Resources.Load OK: {kResMatPath}");
        }
        else
        {
            Debug.LogError($"[OutlinePROV] Resources.Load FAIL: {kResMatPath}. " +
                           $"둘 중 하나를 반드시 해주세요: (A) Graphics > Always Included Shaders에 '{kShaderPath}' 추가, " +
                           $"(B) Resources/Materials/M_Outline.mat 생성 후 셰이더 지정.");
        }
    }

    /// <summary>
    /// 역할: 사용 가능한 공유 머티리얼 반환(없으면 Warmup 시도).
    /// </summary>
    public static Material GetShared()
    {
        if (_shared == null) Warmup();
        return _shared;
    }

    /// <summary>
    /// 역할: 렌더러에 꽂아 쓸 인스턴스(서브메시별로 개별) 생성.
    /// </summary>
    public static Material CreateInstance()
    {
        var baseMat = GetShared();
        if (baseMat == null)
        {
            Debug.LogError("[OutlinePROV] CreateInstance 실패 — 공유 머티리얼이 없습니다. 위 Warmup 로그를 확인하세요.");
            return null;
        }
        return new Material(baseMat);
    }
}
