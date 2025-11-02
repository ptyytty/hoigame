

/// <summary>
/// [모바일 레터/필러박스 생성기 - 안정화 버전]
/// ▷ 기준 화면비(referenceAspect)를 유지하고 남는 영역을 카메라 배경색으로 채움
/// ▷ Screen.width/height 변화가 있을 때만 Rect를 1회 갱신(디바운싱)
/// ▷ Editor Device Simulator의 미세 진동을 완화하기 위해 소수점 반올림 적용
/// </summary>
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraLetterbox : MonoBehaviour
{
    [Header("Aspect Settings")]
    [Tooltip("유지할 기준 화면비 (예: 16:9 = 16f/9f)")]
    public float referenceAspect = 16f / 9f;

    [Tooltip("실수 비교시 반올림 자릿수 (미세 떨림 방지)")]
    [Range(0, 7)] public int roundDigits = 5;

    [Header("Editor Behavior")]
    [Tooltip("에디터에서 '재생 중이 아닐 때'는 항상 풀 화면 Rect(0,0,1,1)로 고정하여 깜빡임 방지")]
    public bool lockFullRectInEditor = true;

    private Camera cam;
    private Vector2Int lastScreenSize = Vector2Int.zero;
    private Rect lastAppliedRect = new Rect(0, 0, 1, 1);

    // ─────────────────────────────────────────────────────────────────────────────
    // Unity Messages
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: 참조 캐시 및 초기 적용
    private void Awake()
    {
        if (!cam) cam = GetComponent<Camera>();
    }

    // 역할: 활성화 시 1회 적용
    private void OnEnable()
    {
        if (!cam) cam = GetComponent<Camera>();
        ApplyRect(force: true);
    }

    // 역할: 에디터에서 값이 바뀌었을 때 / 인스펙터 변경 시 호출
    //       편집 중에는 풀 화면 고정(선택사항)으로 ViewportRect 흔들림 방지
    private void OnValidate()
    {
        if (!cam) cam = GetComponent<Camera>();

#if UNITY_EDITOR
        if (!Application.isPlaying && lockFullRectInEditor && cam != null)
        {
            cam.rect = new Rect(0, 0, 1, 1);
            lastAppliedRect = cam.rect;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            return;
        }
#endif
        ApplyRect(force: true);
    }

    // 역할: 프레임별 해상도/화면비 변화 감지 후 필요 시만 적용
    private void Update()
    {
        if (!cam) return;

#if UNITY_EDITOR
        if (!Application.isPlaying && lockFullRectInEditor)
        {
            // 편집 중에는 풀 화면 유지
            if (cam.rect != new Rect(0, 0, 1, 1))
            {
                cam.rect = new Rect(0, 0, 1, 1);
                lastAppliedRect = cam.rect;
            }
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            return;
        }
#endif
        // 런타임 또는 (에디터/재생중 + lock 해제)에서만 반응
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        if (current != lastScreenSize)
        {
            ApplyRect(force: false);
            lastScreenSize = current;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: 현재 Screen 비율과 referenceAspect를 비교해 타깃 ViewportRect 계산 후 적용
    public void ApplyRect(bool force)
    {
        if (!cam) return;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        float windowAspect = (float)Screen.width / Screen.height;
        float targetW = 1f;
        float targetH = 1f;
        float targetX = 0f;
        float targetY = 0f;

        // 화면이 기준보다 '가로로 넓음' → 좌우 필러박스
        if (windowAspect > referenceAspect)
        {
            // 기준 높이 유지, 폭을 줄임
            float expectedWidth = referenceAspect / windowAspect; // (H=1 기준)
            targetW = expectedWidth;
            targetH = 1f;
            targetX = (1f - targetW) * 0.5f;
            targetY = 0f;
        }
        else // 화면이 기준보다 '세로로 길음' → 상하 레터박스
        {
            // 기준 폭 유지, 높이를 줄임
            float expectedHeight = windowAspect / referenceAspect; // (W=1 기준)
            targetW = 1f;
            targetH = expectedHeight;
            targetX = 0f;
            targetY = (1f - targetH) * 0.5f;
        }

        Rect target = new Rect(
            Round(targetX, roundDigits),
            Round(targetY, roundDigits),
            Round(targetW, roundDigits),
            Round(targetH, roundDigits)
        );

        if (force || !ApproximatelyRect(lastAppliedRect, target, roundDigits))
        {
            cam.rect = target;
            lastAppliedRect = target;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    // 역할: 반올림
    private static float Round(float v, int digits)
    {
        if (digits <= 0) return Mathf.Round(v);
        float m = Mathf.Pow(10f, digits);
        return Mathf.Round(v * m) / m;
    }

    // 역할: Rect 동일성 비교(반올림 자릿수 기준)
    private static bool ApproximatelyRect(Rect a, Rect b, int digits)
    {
        return Mathf.Approximately(Round(a.x, digits), Round(b.x, digits)) &&
               Mathf.Approximately(Round(a.y, digits), Round(b.y, digits)) &&
               Mathf.Approximately(Round(a.width, digits), Round(b.width, digits)) &&
               Mathf.Approximately(Round(a.height, digits), Round(b.height, digits));
    }
}