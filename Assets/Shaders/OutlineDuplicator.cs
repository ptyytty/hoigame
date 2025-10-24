using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

[DisallowMultipleComponent]
public class OutlineDuplicator : MonoBehaviour
{
    [Header("Outline material (Custom/Outline_Mobile_URP)")]
    [Tooltip("비워두면 OutlineMaterialProvider에서 자동 확보")]
    public Material outlineMaterial;

    [Header("Outline params")]
    public Color outlineColor = Color.green;
    [Range(0.001f, 0.5f)] public float outlineWidth = 0.06f;

    [Header("Startup")]
    [Tooltip("시작 시 아웃라인을 켤지 여부")]
    public bool startEnabled = false;

    [Header("Combine")]
    [Tooltip("자식의 정적 MeshFilter들을 하나로 결합하여 내부선 제거(모바일 최적화)")]
    public bool combineStaticChildren = false;

    [Header("Stencil Mask")]
    [SerializeField] private Material maskMaterial; // Custom/OutlineMask_URP (비워두면 자동 생성)

    [Header("Skinned options")]
    [Tooltip("SkinnedMeshRenderer의 마스크 렌더러를 생성하되 항상 비활성화하여 깜빡임 방지")]
    public bool disableSkinnedMaskRenderer = true;

    [Header("Excludes")]
    [Tooltip("이름에 포함되면 아웃라인/마스크 생성에서 제외(대소문자 무시)")]
    public string[] excludeNameContains = new[] { "eye_left", "eye_right", "body.006" };

    // ─────────────────────────────────────────────────────────────────────
    // 내부 상태/캐시
    static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");

    // 생성된 마스크/아웃라인 렌더러(스캐너 토글용). ※ Skinned 마스크는 필요 시 넣지 않음
    readonly List<Renderer> _maskRenderers = new();
    readonly List<Renderer> _outlineRenderers = new();

    MaterialPropertyBlock _mpb;         // 색/두께 반영용
    MaterialPropertyBlock _mpbZero;     // 끌 때 폭=0 강제용
    bool _built;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // [역할] 아웃라인에 사용할 머티리얼 확보
        EnsureMaterial();
    }

    void OnEnable()
    {
        // [역할] 원본 구조 스캔 후 자식 렌더러 생성
        RebuildIfNeeded();
        // [역할] 시작 상태 반영(필요 시 즉시 켜기)
        ApplyProperties(enableNow: startEnabled);
    }

    void OnDisable()
    {
        // [역할] 비활성화 시 렌더만 끄고 폭=0으로 잔상 방지
        EnableOutline(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // [역할] 인스펙터 값 변경 즉시 반영
        if (!gameObject.activeInHierarchy) return;
        EnsureMaterial();
        ApplyProperties(enableNow: false);
    }
#endif

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>역할: 외부에서 아웃라인 ON/OFF</summary>
    public void EnableOutline(bool on)
    {
        // 마스크 토글(※ Skinned 마스크는 옵션에 따라 리스트에 없음)
        for (int i = _maskRenderers.Count - 1; i >= 0; i--)
        {
            var r = _maskRenderers[i];
            if (!r) { _maskRenderers.RemoveAt(i); continue; }
            r.enabled = on;
        }

        // 아웃라인 토글 + MPB 재주입
        for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
        {
            var r = _outlineRenderers[i];
            if (!r) { _outlineRenderers.RemoveAt(i); continue; }

            if (on)
            {
                _mpb ??= new MaterialPropertyBlock();
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorID, outlineColor);
                _mpb.SetFloat(WidthID, outlineWidth);
                r.SetPropertyBlock(_mpb);
                r.enabled = true;
            }
            else
            {
                _mpbZero ??= new MaterialPropertyBlock();
                r.GetPropertyBlock(_mpbZero);
                _mpbZero.SetFloat(WidthID, 0f);
                r.SetPropertyBlock(_mpbZero);
                r.enabled = false;
            }
        }
    }

    /// <summary>역할: 외부에서 색/두께를 일괄 변경(필요 시 즉시 Enable)</summary>
    public void SetProperties(Color color, float width, bool enableNow = true)
    {
        outlineColor = new Color(color.r, color.g, color.b, 1f);
        outlineWidth = Mathf.Max(0.001f, width);
        ApplyProperties(enableNow);
    }

    /// <summary>역할: 외부에서 강제 재빌드 필요 시 호출</summary>
    public void RebuildIfNeeded()
    {
        if (_built) return;
        BuildOutlineChildren();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ---------------------------------------------------------------------

    /// <summary>역할: Outline 머티리얼 확보(Provider 우선 → Shader.Find)</summary>
    void EnsureMaterial()
    {
        if (outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported) return;

        var prov = OutlineMaterialProvider.Instance;
        if (prov) outlineMaterial = prov.GetSharedMaterial();

        if (!outlineMaterial || !outlineMaterial.shader)
            outlineMaterial = new Material(Shader.Find("Custom/Outline_Mobile_URP"));
    }

    /// <summary>역할: 이름 기반 제외 판단(대소문자 무시)</summary>
    bool ShouldExcludeByName(string objName)
    {
        if (excludeNameContains == null || excludeNameContains.Length == 0) return false;
        var lower = objName.ToLowerInvariant();
        for (int i = 0; i < excludeNameContains.Length; i++)
        {
            var key = excludeNameContains[i];
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (lower.Contains(key.ToLowerInvariant())) return true;
        }
        return false;
    }

    /// <summary>역할: 레이어/정적 플래그/SRP 렌더링 레이어 마스크를 원본에서 복사</summary>
    static void CopyLayerAndFlags(GameObject srcGO, Renderer srcR, GameObject childGO, Renderer childR = null)
    {
        if (!srcGO || !childGO) return;

        childGO.layer = srcGO.layer; // 카메라 레이어 필터링 일치
#if UNITY_EDITOR
        childGO.isStatic = srcGO.isStatic;
#endif
        if (srcR && childR)
            childR.renderingLayerMask = srcR.renderingLayerMask; // SRP Rendering Layers 일치
    }

    /// <summary>
    /// 역할: 정적 Mesh들을 하나의 메쉬로 결합하여 Outline/Mask 쌍을 생성(옵션)
    /// 내부선 노이즈 줄이고 드로우콜 절감.
    /// </summary>
    void BuildCombinedStaticChildren()
    {
        var mfs = GetComponentsInChildren<MeshFilter>(includeInactive: true);
        var list = new List<CombineInstance>();

        foreach (var mf in mfs)
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr || !mf.sharedMesh) continue;      // 렌더러 없음/메쉬 없음 제외
            if (mr is SkinnedMeshRenderer) continue;  // 스키닝 제외
            if (ShouldExcludeByName(mr.gameObject.name)) continue;

            var ci = new CombineInstance
            {
                mesh = mf.sharedMesh,
                // 루트 로컬좌표로 변환(정확한 결합을 위해 필수)
                transform = mf.transform.localToWorldMatrix * transform.worldToLocalMatrix
            };
            list.Add(ci);
        }

        if (list.Count == 0) return;

        var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
        combined.CombineMeshes(list.ToArray(), /*mergeSubMeshes*/ true, /*useMatrices*/ true, /*hasLightmapData*/ false);

        // (a) 결합 마스크
        var maskGO = new GameObject(name + ".__OutlineCombinedMask");
        maskGO.transform.SetParent(transform, false);
        maskGO.hideFlags = HideFlags.DontSave;

        var mfMask = maskGO.AddComponent<MeshFilter>(); mfMask.sharedMesh = combined;
        var mrMask = maskGO.AddComponent<MeshRenderer>(); mrMask.sharedMaterial = maskMaterial;
        mrMask.shadowCastingMode = ShadowCastingMode.Off;
        mrMask.receiveShadows = false;
        mrMask.lightProbeUsage = LightProbeUsage.Off;
        mrMask.reflectionProbeUsage = ReflectionProbeUsage.Off;
        mrMask.allowOcclusionWhenDynamic = false;
        CopyLayerAndFlags(gameObject, null, maskGO, mrMask);
        _maskRenderers.Add(mrMask);

        // (b) 결합 아웃라인
        var outGO = new GameObject(name + ".__OutlineCombined");
        outGO.transform.SetParent(transform, false);
        outGO.hideFlags = HideFlags.DontSave;

        var mfOut = outGO.AddComponent<MeshFilter>(); mfOut.sharedMesh = combined;
        var mrOut = outGO.AddComponent<MeshRenderer>(); mrOut.sharedMaterial = outlineMaterial;
        mrOut.shadowCastingMode = ShadowCastingMode.Off;
        mrOut.receiveShadows = false;
        mrOut.lightProbeUsage = LightProbeUsage.Off;
        mrOut.reflectionProbeUsage = ReflectionProbeUsage.Off;
        mrOut.allowOcclusionWhenDynamic = false;
        CopyLayerAndFlags(gameObject, null, outGO, mrOut);
        _outlineRenderers.Add(mrOut);
    }

    /// <summary>
    /// 역할: 원본(Mesh/SkinnedMesh)들을 스캔해서 자식으로 Mask/Outline Renderer를 생성
    /// - combineStaticChildren=true면 정적 메시들은 결합 경로만 생성
    /// - 스키닝은 항상 개별 복제 (단, 이름 제외 + Skinned 마스크 비활성 옵션 적용)
    /// </summary>
    void BuildOutlineChildren()
    {
        _built = false;
        CleanupChildren();
        EnsureMaterial(); // outlineMaterial 보장

        // 1) 마스크 머티리얼 확보
        if (!maskMaterial || !maskMaterial.shader)
        {
            var sh = Shader.Find("Custom/OutlineMask_URP");
            if (sh) maskMaterial = new Material(sh);
        }
        if (!maskMaterial || !maskMaterial.shader || !maskMaterial.shader.isSupported)
        {
            Debug.LogError("[OutlineDuplicator] OutlineMask_URP 셰이더 확보 실패");
            return;
        }

        // 2) 정적 MeshRenderer: 개별 생성 or 결합 생성
        if (combineStaticChildren)
        {
            BuildCombinedStaticChildren();
        }
        else
        {
            var mrs = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var mr in mrs)
            {
                if (mr is SkinnedMeshRenderer) continue;
                if (ShouldExcludeByName(mr.gameObject.name)) continue;

                var mf = mr.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;

                // (a) 마스크
                var mGO = new GameObject(mr.gameObject.name + ".__OutlineMask");
                mGO.transform.SetParent(mr.transform, false);
                mGO.hideFlags = HideFlags.DontSave;

                var mMF = mGO.AddComponent<MeshFilter>(); mMF.sharedMesh = mf.sharedMesh;
                var mMR = mGO.AddComponent<MeshRenderer>(); mMR.sharedMaterial = maskMaterial;
                mMR.shadowCastingMode = ShadowCastingMode.Off;
                mMR.receiveShadows = false;
                CopyLayerAndFlags(mr.gameObject, mr, mGO, mMR);
                _maskRenderers.Add(mMR);

                // (b) 아웃라인
                var oGO = new GameObject(mr.gameObject.name + ".__Outline");
                oGO.transform.SetParent(mr.transform, false);
                oGO.hideFlags = HideFlags.DontSave;

                var oMF = oGO.AddComponent<MeshFilter>(); oMF.sharedMesh = mf.sharedMesh;
                var oMR = oGO.AddComponent<MeshRenderer>(); oMR.sharedMaterial = outlineMaterial;
                oMR.shadowCastingMode = ShadowCastingMode.Off;
                oMR.receiveShadows = false;
                CopyLayerAndFlags(mr.gameObject, mr, oGO, oMR);
                _outlineRenderers.Add(oMR);
            }
        }

        // 3) SkinnedMeshRenderer: 개별 복제(이름 제외 + 마스크 비활성 옵션 적용)
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
        foreach (var src in smrs)
        {
            if (!src.sharedMesh) continue;
            if (ShouldExcludeByName(src.gameObject.name)) continue;

            // (a) 마스크
            var mGO = new GameObject(src.gameObject.name + ".__OutlineMask");
            mGO.transform.SetParent(src.transform, false);
            mGO.hideFlags = HideFlags.DontSave;

            var mSR = mGO.AddComponent<SkinnedMeshRenderer>();
            mSR.sharedMesh = src.sharedMesh; mSR.rootBone = src.rootBone; mSR.bones = src.bones;
            mSR.sharedMaterial = maskMaterial; mSR.updateWhenOffscreen = true;
            mSR.shadowCastingMode = ShadowCastingMode.Off;
            mSR.receiveShadows = false;
            CopyLayerAndFlags(src.gameObject, src, mGO, mSR);

            // ★ 깜빡임 방지: Skinned 마스크는 항상 비활성(리스트에도 추가하지 않음)
            if (disableSkinnedMaskRenderer)
            {
                mSR.enabled = false;
            }
            else
            {
                _maskRenderers.Add(mSR);
            }

            // (b) 아웃라인
            var oGO = new GameObject(src.gameObject.name + ".__Outline");
            oGO.transform.SetParent(src.transform, false);
            oGO.hideFlags = HideFlags.DontSave;

            var oSR = oGO.AddComponent<SkinnedMeshRenderer>();
            oSR.sharedMesh = src.sharedMesh; oSR.rootBone = src.rootBone; oSR.bones = src.bones;
            oSR.sharedMaterial = outlineMaterial; oSR.updateWhenOffscreen = true;
            oSR.shadowCastingMode = ShadowCastingMode.Off;
            oSR.receiveShadows = false;
            CopyLayerAndFlags(src.gameObject, src, oGO, oSR);
            _outlineRenderers.Add(oSR);
        }

        _built = _outlineRenderers.Count > 0;
    }

    /// <summary>역할: 색/두께 머티리얼 프로퍼티를 일괄 반영(필요 시 즉시 Enable)</summary>
    void ApplyProperties(bool enableNow)
    {
        if (!_built) return;
        _mpb ??= new MaterialPropertyBlock();

        for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
        {
            var r = _outlineRenderers[i];
            if (!r) { _outlineRenderers.RemoveAt(i); continue; }

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorID, outlineColor);
            _mpb.SetFloat(WidthID, outlineWidth);
            r.SetPropertyBlock(_mpb);

            if (enableNow) r.enabled = true;
        }

        if (enableNow)
        {
            // 마스크도 함께 켜기(정적/Skinned-옵션 미적용 대상만)
            for (int i = _maskRenderers.Count - 1; i >= 0; i--)
            {
                var r = _maskRenderers[i];
                if (!r) { _maskRenderers.RemoveAt(i); continue; }
                r.enabled = true;
            }
        }
    }

    /// <summary>역할: 이전에 만들었던 자식 Outline/Mask 오브젝트 정리</summary>
    void CleanupChildren()
    {
        for (int i = _maskRenderers.Count - 1; i >= 0; i--)
            if (_maskRenderers[i]) DestroyImmediate(_maskRenderers[i].gameObject);
        _maskRenderers.Clear();

        for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
            if (_outlineRenderers[i]) DestroyImmediate(_outlineRenderers[i].gameObject);
        _outlineRenderers.Clear();
    }

    void OnDestroy()
    {
        // [역할] 파괴 시 생성한 자식 정리
        CleanupChildren();
    }
}