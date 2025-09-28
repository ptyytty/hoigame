using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class OutlineDuplicator : MonoBehaviour
{
    [Header("Outline material made from Custom/Outline_InvertedHull_URP")]
    public Material outlineMaterial;

    [Header("Auto-tune")]
    public Color outlineColor = new Color(0f, 0f, 0f, 1f);
    public float outlineWidth = 0.1f;

    [Header("Startup")]
    [Tooltip("게임 시작 시 아웃라인을 켤지 여부 (기본: 꺼짐)")]
    [SerializeField] private bool startEnabled = false;

    static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    static readonly int WidthID = Shader.PropertyToID("_OutlineWidth");

    // 원본 ↔ 아웃라인 렌더러 매핑
    readonly List<Renderer> _outlineRenderers = new List<Renderer>();
    bool _built = false;

    void Awake()
    {
        // ✅ 변경: 일단 대기, 머티리얼이 나중에 세팅되면 그때 빌드
        TryBuildIfPossible();
        // 기본은 꺼놓기 (스캐너가 ShowUI로 켜줄거니까)
        if (_built) EnableOutline(false);
    }

    void OnDestroy()
    {
        // 생성했던 아웃라인 GO들 정리
        foreach (var r in _outlineRenderers)
            if (r) Destroy(r.gameObject);
        _outlineRenderers.Clear();
    }

    public void SetProperties(Color color, float width)
    {
        outlineColor = color;
        outlineWidth = width;

        TryBuildIfPossible();

        foreach (var r in _outlineRenderers)
        {
            if (r == null) continue;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i]) { mats[i].SetColor(ColorID, color); mats[i].SetFloat(WidthID, width); }
            }
        }
    }

    public void EnableOutline(bool on)
    {
        TryBuildIfPossible();
        foreach (var r in _outlineRenderers) if (r) r.enabled = on;
    }

    void TryBuildIfPossible()
    {
        if (_built) return;
        if (!outlineMaterial) return;     // 아직 머티리얼이 안 들어왔으면 대기
        BuildOutlineRenderers();          // 기존 함수 그대로 사용
        _built = true;
        // 빌드 직후 한 번 속성 반영
        SetProperties(outlineColor, outlineWidth);
    }

    void BuildOutlineRenderers()
    {
        _outlineRenderers.Clear();

        // 원본들 찾기(자식까지)
        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // MeshRenderer 처리
        foreach (var src in meshRenderers)
        {
            // 메시가 없으면 스킵
            var mf = src.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) continue;

            // 아웃라인용 GO 생성
            var go = new GameObject(src.gameObject.name + "__Outline");
            go.transform.SetParent(src.transform, false); // 동일 위치/회전/스케일

            var newMF = go.AddComponent<MeshFilter>();
            newMF.sharedMesh = mf.sharedMesh;

            var newMR = go.AddComponent<MeshRenderer>();
            // 서브메시 개수만큼 아웃라인 머티리얼 인스턴스 채우기
            int subCount = newMF.sharedMesh.subMeshCount;
            var mats = new Material[subCount];
            for (int i = 0; i < subCount; i++)
                mats[i] = new Material(outlineMaterial); // SRP Batcher 호환 OK

            newMR.sharedMaterials = mats;

            // 그림자 등 본체와 동일 옵션을 원하면 복사
            newMR.shadowCastingMode = src.shadowCastingMode;
            newMR.receiveShadows = src.receiveShadows;
            newMR.lightProbeUsage = src.lightProbeUsage;
            newMR.reflectionProbeUsage = src.reflectionProbeUsage;
            newMR.probeAnchor = src.probeAnchor;
            newMR.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;

            _outlineRenderers.Add(newMR);
        }

        // SkinnedMeshRenderer 처리
        foreach (var src in skinnedRenderers)
        {
            if (!src.sharedMesh) continue;

            var go = new GameObject(src.gameObject.name + "__Outline");
            go.transform.SetParent(src.transform, false);

            var newSMR = go.AddComponent<SkinnedMeshRenderer>();
            newSMR.sharedMesh = src.sharedMesh;
            newSMR.rootBone = src.rootBone;
            newSMR.bones = src.bones;
            newSMR.quality = src.quality;
            newSMR.updateWhenOffscreen = src.updateWhenOffscreen;
            newSMR.localBounds = src.localBounds;

            int subCount = newSMR.sharedMesh.subMeshCount;
            var mats = new Material[subCount];
            for (int i = 0; i < subCount; i++)
                mats[i] = new Material(outlineMaterial);

            newSMR.sharedMaterials = mats;

            newSMR.shadowCastingMode = src.shadowCastingMode;
            newSMR.receiveShadows = src.receiveShadows;
            newSMR.lightProbeUsage = src.lightProbeUsage;
            newSMR.reflectionProbeUsage = src.reflectionProbeUsage;
            newSMR.probeAnchor = src.probeAnchor;
            newSMR.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;

            _outlineRenderers.Add(newSMR);
        }
    }

}
