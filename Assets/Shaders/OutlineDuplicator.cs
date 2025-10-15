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
    [Tooltip("자식의 정적 MeshFilter들을 하나의 메쉬로 결합하여 내부선 제거")]
    public bool combineStaticChildren = false;

    [Header("Stencil Mask")]
    [SerializeField] private Material maskMaterial; // Custom/OutlineMask_URP (비워두면 자동 생성)

    // 내부 리스트 보관
    readonly List<Renderer> _maskRenderers = new();

    // ─────────────────────────────────────────────────────────────────────
    // 내부 상태
    static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");

    // 자식으로 만든 아웃라인 전용 렌더러 보관
    readonly List<Renderer> _outlineRenderers = new();
    bool _built;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // [역할] 머티리얼 확보(Provider → Shader.Find)
        EnsureMaterial();
    }

    void OnEnable()
    {
        // [역할] 원본 메쉬들 스캔 후, 동일 구조의 자식 렌더러 생성
        RebuildIfNeeded();
        // [역할] 시작 상태 적용
        ApplyProperties(enableNow: startEnabled);
    }

    void OnDisable()
    {
        // [역할] 비활성 시에는 렌더만 끄고(오브젝트는 유지) – 재활성화 대비
        EnableOutline(false);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // [역할] 인스펙터 값 바뀌면 즉시 반영
        if (!gameObject.activeInHierarchy) return;
        EnsureMaterial();
        ApplyProperties(enableNow: false);
    }
#endif

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>역할: 외부에서 아웃라인 ON/OFF</summary>
    // Enable/Disable 시 마스크도 함께 토글
    public void EnableOutline(bool on)
    {
        for (int i = _maskRenderers.Count - 1; i >= 0; i--)
        {
            var r = _maskRenderers[i];
            if (!r) { _maskRenderers.RemoveAt(i); continue; }
            r.enabled = on;
        }
        for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
        {
            var r = _outlineRenderers[i];
            if (!r) { _outlineRenderers.RemoveAt(i); continue; }
            r.enabled = on;
        }
    }

    /// <summary>역할: 색/두께를 전부 반영(필요 시 즉시 Enable)</summary>
    public void SetProperties(Color color, float width, bool enableNow = true)
    {
        outlineColor = new Color(color.r, color.g, color.b, 1f);
        outlineWidth = Mathf.Max(0.001f, width);
        ApplyProperties(enableNow);
    }

    /// <summary>역할: 외부에서 강제 재빌드가 필요할 때 호출</summary>
    public void RebuildIfNeeded()
    {
        if (_built) return;
        BuildOutlineChildren();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 내부 구현
    // ---------------------------------------------------------------------

    // [역할] Provider → Shader.Find 순으로 머티리얼 확보
    void EnsureMaterial()
    {
        if (outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported) return;

        var prov = OutlineMaterialProvider.Instance;
        if (prov) outlineMaterial = prov.GetSharedMaterial();

        if (!outlineMaterial || !outlineMaterial.shader)
            outlineMaterial = new Material(Shader.Find("Custom/Outline_Mobile_URP"));
    }

    void BuildCombinedStaticChildren()
    {
        var mfs = GetComponentsInChildren<MeshFilter>(includeInactive: true);
        var list = new List<CombineInstance>();

        foreach (var mf in mfs)
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr || !mf.sharedMesh) continue;               // 렌더러 없는 건 제외(콜라이더용 등)
            if (mr is SkinnedMeshRenderer) continue;           // 스키닝은 제외

            var ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = mf.transform.localToWorldMatrix * transform.worldToLocalMatrix;
            list.Add(ci);
        }

        if (list.Count == 0) return;

        var combined = new Mesh();
        combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 큰 메쉬 안전
        combined.CombineMeshes(list.ToArray(), /*mergeSubMeshes*/ true, /*useMatrices*/ true, /*hasLightmapData*/ false);

        var child = new GameObject(name + ".__OutlineCombined");
        child.transform.SetParent(transform, false);
        child.hideFlags = HideFlags.DontSave;

        var mfCombined = child.AddComponent<MeshFilter>();
        mfCombined.sharedMesh = combined;

        var mrCombined = child.AddComponent<MeshRenderer>();
        mrCombined.sharedMaterial = outlineMaterial;
        mrCombined.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mrCombined.receiveShadows = false;
        mrCombined.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mrCombined.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        mrCombined.allowOcclusionWhenDynamic = false;

        _outlineRenderers.Add(mrCombined);
    }

    // [역할] 원본(Mesh/SkinnedMesh)들을 스캔하고, 동일한 자식 GO에 아웃라인 전용 Renderer 생성
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

        // 2) 정적 MeshRenderer 들: (a) 마스크, (b) 아웃라인
        var mrs = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
        foreach (var mr in mrs)
        {
            if (mr is SkinnedMeshRenderer) continue;
            var mf = mr.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;

            // (a) 마스크
            var mGO = new GameObject(mr.gameObject.name + ".__OutlineMask");
            mGO.transform.SetParent(mr.transform, false);
            mGO.hideFlags = HideFlags.DontSave;

            var mMF = mGO.AddComponent<MeshFilter>(); mMF.sharedMesh = mf.sharedMesh;
            var mMR = mGO.AddComponent<MeshRenderer>(); mMR.sharedMaterial = maskMaterial;
            mMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mMR.receiveShadows = false;
            _maskRenderers.Add(mMR);

            // (b) 아웃라인
            var oGO = new GameObject(mr.gameObject.name + ".__Outline");
            oGO.transform.SetParent(mr.transform, false);
            oGO.hideFlags = HideFlags.DontSave;

            var oMF = oGO.AddComponent<MeshFilter>(); oMF.sharedMesh = mf.sharedMesh;
            var oMR = oGO.AddComponent<MeshRenderer>(); oMR.sharedMaterial = outlineMaterial;
            oMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            oMR.receiveShadows = false;
            _outlineRenderers.Add(oMR);
        }

        // 3) 스키닝도 동일 (마스크 + 아웃라인)
        var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
        foreach (var src in smrs)
        {
            if (!src.sharedMesh) continue;

            // (a) 마스크
            var mGO = new GameObject(src.gameObject.name + ".__OutlineMask");
            mGO.transform.SetParent(src.transform, false);
            mGO.hideFlags = HideFlags.DontSave;

            var mSR = mGO.AddComponent<SkinnedMeshRenderer>();
            mSR.sharedMesh = src.sharedMesh; mSR.rootBone = src.rootBone; mSR.bones = src.bones;
            mSR.sharedMaterial = maskMaterial; mSR.updateWhenOffscreen = true;
            mSR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mSR.receiveShadows = false;
            _maskRenderers.Add(mSR);

            // (b) 아웃라인
            var oGO = new GameObject(src.gameObject.name + ".__Outline");
            oGO.transform.SetParent(src.transform, false);
            oGO.hideFlags = HideFlags.DontSave;

            var oSR = oGO.AddComponent<SkinnedMeshRenderer>();
            oSR.sharedMesh = src.sharedMesh; oSR.rootBone = src.rootBone; oSR.bones = src.bones;
            oSR.sharedMaterial = outlineMaterial; oSR.updateWhenOffscreen = true;
            oSR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            oSR.receiveShadows = false;
            _outlineRenderers.Add(oSR);
        }

        _built = _outlineRenderers.Count > 0;
    }
    // [역할] 색/두께 머티리얼 프로퍼티를 일괄 반영(Instancing 없이 PropertyBlock 사용)
    void ApplyProperties(bool enableNow)
    {
        if (!_built) return;

        for (int i = _outlineRenderers.Count - 1; i >= 0; i--)
        {
            var r = _outlineRenderers[i];
            if (!r) { _outlineRenderers.RemoveAt(i); continue; }

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorID, outlineColor);
            mpb.SetFloat(WidthID, outlineWidth);
            r.SetPropertyBlock(mpb);

            if (enableNow) r.enabled = true;
        }
    }



    // [역할] 이전에 만들었던 아웃라인 자식 정리
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
        CleanupChildren();
    }
}