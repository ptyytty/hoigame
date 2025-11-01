using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// [모바일 레터/필러박스 생성기 - 안정화 버전]
/// ▷ 기준 화면비(referenceAspect)를 유지하고 남는 영역을 카메라 배경색으로 채움
/// ▷ Screen.width/height 변화가 있을 때만 Rect를 1회 갱신(디바운싱)
/// ▷ Editor Device Simulator의 미세 진동을 완화하기 위해 소수점 반올림 적용
/// </summary>
[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraLetterbox : MonoBehaviour
{
    [Tooltip("유지할 기준 화면비 (예: 16:9 = 16f/9f)")]
    public float referenceAspect = 16f / 9f;

    [Tooltip("에디터에서 전체 화면으로 고정(레터박스 미표시)")]
    public bool lockFullRectInEditor = true;

    [Tooltip("소수점 반올림 자릿수(시뮬레이터 떨림 방지)")]
    [Range(3, 6)] public int roundDigits = 4;

    private Camera cam;
    private int lastW = -1, lastH = -1;
    private float lastRefAspect = -1f;
    private Rect lastRect;                 // 마지막으로 적용한 Rect
    private bool isApplying = false;       // 재진입 보호 플래그

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        ApplyRect(true); // ▶ 첫 1회 즉시 적용
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
        EditorApplication.update += EditorTick;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorTick;
        if (lockFullRectInEditor && !Application.isPlaying && cam)
            cam.rect = new Rect(0, 0, 1, 1);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// [에디터] GameView 크기 변화 감지용(시뮬레이터 포함)
    /// </summary>
    private void EditorTick()
    {
        ApplyRect(); // 내부에서 변화가 있을 때만 실제 변경
    }
#endif

    void Update()
    {
        // ▶ 런타임에서도 해상도/회전이 바뀌면 1회만 적용
        if (Application.isPlaying)
            ApplyRect();
    }

    /// <summary>
    /// 화면 크기/기준비 변화가 있을 때만 Rect를 갱신
    /// </summary>
    private void ApplyRect(bool force = false)
    {
        if (!cam || isApplying) return;
#if UNITY_EDITOR
        if (!Application.isPlaying && lockFullRectInEditor)
        {
            cam.rect = new Rect(0, 0, 1, 1);
            return;
        }
#endif
        int w = Mathf.Max(Screen.width, 1);
        int h = Mathf.Max(Screen.height, 1);

        // ▶ 변화 감지(해상도/기준비)
        if (!force && w == lastW && h == lastH && Mathf.Approximately(referenceAspect, lastRefAspect))
            return;

        lastW = w; lastH = h; lastRefAspect = referenceAspect;

        float screenAspect = (float)w / h;
        Rect r;
        if (screenAspect > referenceAspect)
        {
            // 화면이 더 넓다 → 좌/우 필러박스
            float targetW = referenceAspect / screenAspect; // 0~1
            float x = (1f - targetW) * 0.5f;
            r = new Rect(x, 0f, targetW, 1f);
        }
        else
        {
            // 화면이 더 높다 → 상/하 레터박스
            float targetH = screenAspect / referenceAspect; // 0~1
            float y = (1f - targetH) * 0.5f;
            r = new Rect(0f, y, 1f, targetH);
        }

        // ▶ 소수점 반올림으로 미세 진동 제거
        r.x = (float)System.Math.Round(r.x, roundDigits);
        r.y = (float)System.Math.Round(r.y, roundDigits);
        r.width = (float)System.Math.Round(r.width, roundDigits);
        r.height = (float)System.Math.Round(r.height, roundDigits);

        // ▶ 동일값 재적용 금지(깜빡임 방지)
        if (!force && NearlyEqual(r, lastRect)) return;

        isApplying = true;  // 재진입 방지
        cam.rect = r;
        lastRect = r;
        isApplying = false;
    }

    /// <summary>
    /// Rect 근사치 비교(플로팅 에러 흡수)
    /// </summary>
    private static bool NearlyEqual(Rect a, Rect b, float eps = 0.0001f)
    {
        return Mathf.Abs(a.x - b.x) < eps &&
               Mathf.Abs(a.y - b.y) < eps &&
               Mathf.Abs(a.width - b.width) < eps &&
               Mathf.Abs(a.height - b.height) < eps;
    }

    /// <summary>
    /// 인스펙터 값이 바뀌면 즉시 재적용
    /// </summary>
    private void OnValidate()
    {
        if (!cam) cam = GetComponent<Camera>();
        ApplyRect(true);
    }
}
