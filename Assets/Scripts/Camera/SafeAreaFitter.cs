using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("Targets")]
    [Tooltip("SafeArea를 적용할 대상 RectTransform (비워두면 본인)")]
    public RectTransform target;

    [Header("Behavior")]
    [Tooltip("에디터(비재생)에서도 적용할지 여부")]
    public bool applyInEditor = true;

    [Tooltip("에디터(비재생)에서 SafeArea/해상도 변화가 threshold(px) 이상일 때만 갱신")]
    public int editorThresholdPx = 2;

    [Tooltip("런타임에서 SafeArea/해상도 변화가 threshold(px) 이상일 때만 갱신")]
    public int runtimeThresholdPx = 1;

    private RectTransform _rect;
    private Rect lastSafe = Rect.zero;
    private Vector2Int lastSize = Vector2Int.zero;

    // ─────────────────────────────────────────────────────────────────────────────
    // Unity Messages
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: 참조 캐시
    private void Awake()
    {
        _rect = target ? target : GetComponent<RectTransform>();
    }

    // 역할: 활성화 시 1회 적용
    private void OnEnable()
    {
        if (!_rect) _rect = target ? target : GetComponent<RectTransform>();
        Apply(force: true);
    }

    // 역할: 프레임별로 의미있는 변화가 있을 때만 Apply() 수행
    private void Update()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!applyInEditor) return;
            if (HasSignificantChange(editorThresholdPx))
                Apply(force: false);
            return;
        }
#endif
        // 런타임
        if (HasSignificantChange(runtimeThresholdPx))
            Apply(force: false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: SafeArea를 정규화(0~1)한 뒤 앵커에 반영하고, 오프셋은 0으로 고정
    public void Apply(bool force)
    {
        if (!_rect) _rect = target ? target : GetComponent<RectTransform>();
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect s = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);

        if (!force && s == lastSafe && size == lastSize) return;

        lastSafe = s;
        lastSize = size;

        Vector2 min = s.position;
        Vector2 max = s.position + s.size;

        // 픽셀 → 정규화
        min.x /= Screen.width;  min.y /= Screen.height;
        max.x /= Screen.width;  max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: SafeArea/해상도의 변화량이 임계값(px) 이상인지 검사
    private bool HasSignificantChange(int thresholdPx)
    {
        Rect s = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);

        bool sizeChanged = (size != lastSize);
        bool safeChanged =
            Mathf.Abs(s.x - lastSafe.x) > thresholdPx ||
            Mathf.Abs(s.y - lastSafe.y) > thresholdPx ||
            Mathf.Abs(s.width - lastSafe.width) > thresholdPx ||
            Mathf.Abs(s.height - lastSafe.height) > thresholdPx;

        return sizeChanged || safeChanged;
    }
}