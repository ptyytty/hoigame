using UnityEngine;

public class OutlineMaterialProvider : MonoBehaviour
{
    public static OutlineMaterialProvider Instance { get; private set; }

    [Header("🔹직접 연결할 머테리얼 (선택)")]
    [SerializeField] private Material serializedMaterial;

    [Header("🔹Resources/Outline.mat 를 우선 로드")]
    [SerializeField] private string resourcesPath = "Outline";

    private Material _shared;

    void Awake()
    {
        // [역할] 싱글톤 보장 + 씬 전환 유지
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [역할] 공유 머티리얼 확보(순서: 직렬화 > Resources > Shader.Find)
        _shared = TryGetValid(serializedMaterial);
        if (!_shared)
        {
            var resMat = Resources.Load<Material>(resourcesPath);
            _shared = TryGetValid(resMat);
        }
        if (!_shared)
        {
            var sh = Shader.Find("Custom/Outline_Mobile_URP");
            if (sh) _shared = new Material(sh);
        }

        // [역할] 최종 확인 및 로그
        if (!_shared || _shared.shader == null || !_shared.shader.isSupported)
        {
            Debug.LogError("[OutlinePROV] 유효한 Outline 머티리얼/셰이더 확보 실패! (isSupported=false)");
        }
        else
        {
            Debug.Log($"[OutlinePROV] Ready: mat={_shared.name}, shader={_shared.shader.name}, supported={_shared.shader.isSupported}");
        }
    }

    /// <summary>역할: 외부에 공유 머티리얼 제공</summary>
    public Material GetSharedMaterial()
    {
        if (_shared && _shared.shader && _shared.shader.isSupported) return _shared;

        // [역할] 런타임 중 복구 시도
        var resMat = Resources.Load<Material>(resourcesPath);
        _shared = TryGetValid(resMat);
        if (!_shared)
        {
            var sh = Shader.Find("Custom/Outline_Mobile_URP");
            if (sh) _shared = new Material(sh);
        }

        if (!_shared || _shared.shader == null || !_shared.shader.isSupported)
            Debug.LogError("[OutlinePROV] 런타임 복구 실패 (isSupported=false)");

        return _shared;
    }

    // [역할] null 아닌 유효 머티리얼만 통과
    Material TryGetValid(Material m)
    {
        if (!m || !m.shader) return null;
        if (!m.shader.isSupported) return null;
        return m;
    }

    // ✅ 호환용 정적 접근자
    public static Material GetShared() =>
        Instance ? Instance.GetSharedMaterial() : null;
}