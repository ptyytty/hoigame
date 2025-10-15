using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class OutlineDuplicator : MonoBehaviour
{
    // ── [필드 설명] ─────────────────────────────────────────────────────
    // outlineMaterial : 아웃라인에 사용할 머티리얼(프로바이더 기본값 권장)
    // outlineColor/width : 아웃라인 색/두께
    // startEnabled : 시작 시 곧바로 켤지 여부
    // fallbackDrawWhenInvisible : 엔진이 isVisible=false로 판단해도 강제 렌더(디버그)
    // forceOverlayQueue : 렌더큐를 3999(Transparent)로 강제 → URP Transparent 패스만 있어도 보이게
    // outlineRenderLayerName : 복제 렌더러 전용 레이어(비워두면 원본 레이어 그대로 사용)
    // ────────────────────────────────────────────────────────────────────

    [Header("Outline material (Custom/Outline_Mobile_URP via Provider)")]
    public Material outlineMaterial;

    [Header("Auto-tune")]
    public Color outlineColor = Color.green;        // 아웃라인 색
    public float outlineWidth = 0.06f;              // 월드 두께(모델 크기 비례 최소치 적용)

    [Header("Startup")]
    [SerializeField] private bool startEnabled = false;   // 시작 시 표시 여부
    [Tooltip("SetProperties 호출 시 자동 Enable할지")]
    public bool autoEnableOnSetProperties = true;

    [Header("Safety / Debug")]
    [Tooltip("엔진 가시성/레이어/패스 필터를 우회해 강제로 그리기")]
    public bool fallbackDrawWhenInvisible = true;   // 빌드 디버그 기본 ON
    [Tooltip("렌더 큐를 3999(Transparent)로 강제 (URP Transparent LayerMask만 켜져도 보이게)")]
    public bool forceOverlayQueue = true;

    [Header("Render Layer Override (optional)")]
    [Tooltip("복제 렌더러만 이 레이어로 렌더(비우면 원본 레이어 유지). 예: Default")]
    public string outlineRenderLayerName = "Default";

    static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");

    // 원본↔아웃라인 동기화를 위한 트랜스폼 목록
    readonly List<Transform> _outlineXforms = new();
    readonly List<Transform> _sourceXforms = new();

    // 실제 그리는 복제 렌더러들
    readonly List<Renderer> _outlineRenderers = new();

    // 폴백 드로우용 원본/스키닝 레퍼런스
    struct SourceMeshRef { public Mesh mesh; public Transform transform; public bool skinned; public SkinnedMeshRenderer smr; }
    readonly List<SourceMeshRef> _sources = new();

    bool _built = false;

    void Awake()
    {
        if (!outlineMaterial) outlineMaterial = OutlineMaterialProvider.GetShared();
        if (!outlineMaterial || !outlineMaterial.shader || !outlineMaterial.shader.isSupported)
            Debug.LogError("[Highlighter] Outline material/shader invalid.");
        TryBuildIfPossible();

        if (_built && startEnabled) EnableOutline(true);
    }

    void OnDestroy()
    {
        foreach (var r in _outlineRenderers) if (r) Destroy(r.gameObject);
        _outlineRenderers.Clear();
        _outlineXforms.Clear();
        _sourceXforms.Clear();
        _sources.Clear();
    }

    void LateUpdate()
    {
        // [역할] 매 프레임 원본/복제의 월드 포즈 동기화(스케일 1, 자식 고정)
        for (int i = 0; i < _outlineXforms.Count; i++)
        {
            var src = _sourceXforms[i]; var dst = _outlineXforms[i];
            if (!src || !dst) continue;
            dst.position = src.position;
            dst.rotation = src.rotation;
            dst.localScale = Vector3.one; // 자식 기준 1 고정
        }
    }

    /// <summary>역할: 색/두께를 갱신하고(필요 시) 즉시 표시</summary>
    public void SetProperties(Color color, float width, bool enableNow = true)
    {
        outlineColor = new Color(color.r, color.g, color.b, 1f);
        outlineWidth = Mathf.Max(0.0005f, width);

        TryBuildIfPossible();
        if (!_built) { Debug.LogWarning("[Highlighter] SetProperties 호출됨 - 아직 outline 렌더러가 없음."); return; }

        foreach (var r in _outlineRenderers)
        {
            if (!r) continue;
            // 모델 크기 비례 최소 두께 적용
            float radius = r.bounds.extents.magnitude;
            float widthToApply = Mathf.Max(outlineWidth, radius * 0.02f);

            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i]; if (!m) continue;
                if (m.HasColor(ColorID)) m.SetColor(ColorID, outlineColor);
                if (m.HasFloat(WidthID)) m.SetFloat(WidthID, widthToApply);
                if (forceOverlayQueue) m.renderQueue = 3999;
            }

            r.receiveShadows = false;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        if (autoEnableOnSetProperties && enableNow) EnableOutline(true);

        DebugDumpRenderers("AfterEnable");
        DebugCamerasAndFrustum("AfterEnable");
    }

    /// <summary>역할: 아웃라인 표시 on/off (Renderer.forceRenderingOff 활용)</summary>
    public void EnableOutline(bool on)
    {
        TryBuildIfPossible();
        if (!_built) { Debug.LogWarning("[Highlighter] EnableOutline 호출됨 - 아직 outline 렌더러가 없음."); return; }

        foreach (var r in _outlineRenderers)
        {
            if (!r) continue;
            r.enabled = true;
            r.forceRenderingOff = !on;
        }
        Debug.Log($"[Highlighter] Outline {(on ? "ENABLED" : "DISABLED")}");
    }

    // ───────────────── 내부 구현 ─────────────────

    void TryBuildIfPossible()
    {
        if (_built) return;
        if (!outlineMaterial || outlineMaterial.shader == null || !outlineMaterial.shader.isSupported) return;

        BuildOutlineRenderers();
        _built = true;

        // 자동으로 켜지진 않되, 속성은 미리 반영
        SetProperties(outlineColor, outlineWidth, enableNow: false);
    }

    /// <summary>
    /// 역할: 원본 Renderer들을 복제해 "아웃라인 전용 Renderer" 생성.
    /// - 반드시 원본의 "자식"으로 두고 local(0,0,1) 고정 → 좌표계/프러스텀 일치
    /// - MeshRenderer: 복제 Mesh로 bounds 약간 확장
    /// - SkinnedMeshRenderer: updateWhenOffscreen 및 localBounds 확장
    /// - 레이어 오버라이드가 지정되면 복제물만 안전 레이어로 렌더
    /// </summary>
    void BuildOutlineRenderers()
    {
        _outlineRenderers.Clear();
        _outlineXforms.Clear();
        _sourceXforms.Clear();
        _sources.Clear();

        int overrideLayer = -1;
        if (!string.IsNullOrEmpty(outlineRenderLayerName))
        {
            int idx = LayerMask.NameToLayer(outlineRenderLayerName);
            if (idx >= 0) overrideLayer = idx;
        }

        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var src in meshRenderers)
        {
            var mf = src.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;

            var go = new GameObject(src.gameObject.name + "__Outline");
            go.transform.SetParent(src.transform, false);       // ★ 자식으로 고정(좌표 일치)
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.layer = (overrideLayer >= 0) ? overrideLayer : src.gameObject.layer;

            var newMF = go.AddComponent<MeshFilter>();
            var cloned = Object.Instantiate(mf.sharedMesh);
            var b = cloned.bounds; b.Expand(new Vector3(0.25f, 0.25f, 0.25f));
            cloned.bounds = b;
            newMF.sharedMesh = cloned;

            var newMR = go.AddComponent<MeshRenderer>();
            int sub = newMF.sharedMesh.subMeshCount;
            var mats = new Material[sub];
            for (int i = 0; i < sub; i++) mats[i] = OutlineMaterialProvider.CreateInstance();
            newMR.sharedMaterials = mats;
            newMR.shadowCastingMode = ShadowCastingMode.Off;
            newMR.receiveShadows = false;
            newMR.allowOcclusionWhenDynamic = false;

            _outlineRenderers.Add(newMR);
            _outlineXforms.Add(go.transform);
            _sourceXforms.Add(src.transform);

            Debug.Log($"[OL-POSE] src={src.name} srcW={src.transform.position} olW={go.transform.position} local={go.transform.localPosition}");
        }

        var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var src in skinnedRenderers)
        {
            if (!src.sharedMesh) continue;

            var go = new GameObject(src.gameObject.name + "__Outline");
            go.transform.SetParent(src.transform, false);
            go.layer = (overrideLayer >= 0) ? overrideLayer : src.gameObject.layer;

            var newSMR = go.AddComponent<SkinnedMeshRenderer>();
            newSMR.sharedMesh  = src.sharedMesh;
            newSMR.rootBone    = src.rootBone;
            newSMR.bones       = src.bones;
            newSMR.quality     = src.quality;
            newSMR.updateWhenOffscreen = true;

            var b = src.localBounds;
            float pad = Mathf.Max(0.5f, outlineWidth * 10f);
            b.Expand(new Vector3(pad, pad, pad));
            newSMR.localBounds = b;

            int subCount = newSMR.sharedMesh.subMeshCount;
            var mats = new Material[subCount];
            for (int i = 0; i < subCount; i++) mats[i] = OutlineMaterialProvider.CreateInstance();
            newSMR.sharedMaterials = mats;

            newSMR.shadowCastingMode = ShadowCastingMode.Off;
            newSMR.receiveShadows = false;
            newSMR.allowOcclusionWhenDynamic = false;

            _outlineRenderers.Add(newSMR);

            // 스키닝은 부모 유지(본 추적), 월드 동기 목록은 MeshRenderer만
        }

        // 폴백용 소스 목록(강제 드로우 시 사용)
        foreach (var src in meshRenderers)
        {
            var mf = src.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;
            _sources.Add(new SourceMeshRef { mesh = mf.sharedMesh, transform = src.transform, skinned = false, smr = null });
        }
        foreach (var src in skinnedRenderers)
        {
            if (!src.sharedMesh) continue;
            _sources.Add(new SourceMeshRef { mesh = null, transform = src.transform, skinned = true, smr = src });
        }
    }

    // ── 디버그: 렌더러 상태 ────────────────────────────
    void DebugDumpRenderers(string tag = "Dump")
    {
        foreach (var r in _outlineRenderers)
        {
            if (!r) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                var q = m ? m.renderQueue : -1;
                var sh = m && m.shader ? m.shader.name : "null";
                Debug.Log($"[HighlighterDBG:{tag}] {r.name} layer={LayerMask.LayerToName(r.gameObject.layer)} enabled={r.enabled} forceOff={r.forceRenderingOff} isVisible={r.isVisible} mat{i}={sh} queue={q}");
            }
        }
    }

    // ── 디버그: 카메라/프러스텀 검사 ───────────────────
    void DebugCamerasAndFrustum(string tag = "Cam")
    {
        var cams = Camera.allCameras;
        foreach (var r in _outlineRenderers)
        {
            if (!r) continue;
            var b = r.bounds;
            Debug.Log($"[OL-CAM:{tag}] R={r.name} vis={r.isVisible} center={b.center} size={b.size}");
            for (int i = 0; i < cams.Length; i++)
            {
                var cam = cams[i];
                if (!cam || !cam.isActiveAndEnabled) continue;
                bool layerOn = (cam.cullingMask & (1 << r.gameObject.layer)) != 0;
                var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                bool inFrustum = GeometryUtility.TestPlanesAABB(planes, b);
                Debug.Log($"   - cam[{i}] {cam.name} layerOn={layerOn} inFrustum={inFrustum} ortho={cam.orthographic} pos={cam.transform.position}");
            }
        }
    }

    // ── 디버그: 엔진이 invisible 판단 시 강제 드로우 ──
    MaterialPropertyBlock _mpb;
    void EnsureMPB() { if (_mpb != null) return; _mpb = new MaterialPropertyBlock(); }

    void OnRenderObject()
    {
        if (!fallbackDrawWhenInvisible) return;
        if (!outlineMaterial || outlineMaterial.shader == null || !outlineMaterial.shader.isSupported) return;

        // 아웃라인이 꺼져 있으면 스킵
        bool anyEnabled = false;
        for (int i = 0; i < _outlineRenderers.Count; i++)
            if (_outlineRenderers[i] && !_outlineRenderers[i].forceRenderingOff) { anyEnabled = true; break; }
        if (!anyEnabled) return;

        var cam = Camera.current; if (!cam) return;

        bool allInvisible = true;
        foreach (var r in _outlineRenderers) { if (r && r.isVisible) { allInvisible = false; break; } }
        if (!allInvisible) return;

        EnsureMPB();
        _mpb.SetColor("_OutlineColor", outlineColor);
        _mpb.SetFloat("_OutlineWidth", Mathf.Max(0.0005f, outlineWidth));

        // 폴백은 항상 화면에 보이도록 깊이/스텐실 무시 키워드를 켠다
        outlineMaterial.EnableKeyword("_OUTLINE_DEBUG_BYPASS");

        foreach (var s in _sources)
        {
            if (!s.transform) continue;

            Mesh meshToDraw = s.mesh;
            if (s.skinned)
            {
                if (!s.smr) continue;
                if (meshToDraw == null) meshToDraw = new Mesh();
                s.smr.BakeMesh(meshToDraw);
            }
            if (!meshToDraw) continue;

            outlineMaterial.SetPass(0); // 첫 패스
            Graphics.DrawMeshNow(meshToDraw, s.transform.localToWorldMatrix);
        }
    }
}
