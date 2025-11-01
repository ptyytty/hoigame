using UnityEngine;
using Cinemachine;

/// <summary>
/// [직교 카메라 화면비 보정]
/// ▷ 기준 화면비/사이즈를 정의하고, 레터박스 적용 '후' 실제 가시영역 비율을 기준으로 OrthoSize를 보정합니다.
/// ▷ 던전처럼 월드 오브젝트가 많은 씬에서 '가로 폭 고정(또는 세로 고정)'을 원하는 경우에만 사용하세요.
/// ▷ 로비가 퍼스펙티브 카메라 위주라면 이 스크립트는 필요 없습니다.
/// </summary>
[ExecuteAlways]
public class CM_OrthoFit : MonoBehaviour
{
    public enum FitMode { ConstantWidth, ConstantHeight }

    [Header("Reference")]
    [Tooltip("기준 화면비 (예: 16:9 -> 16f/9f)")]
    public float referenceAspect = 16f / 9f;

    [Tooltip("기준 화면비에서의 Orthographic Size (예: 16:9일 때 10)")]
    public float referenceOrthoSize = 10f;

    [Header("Mode")]
    [Tooltip("가로폭을 일정하게 유지(ConstantWidth)할지, 세로폭을 유지(ConstantHeight)할지")]
    public FitMode fitMode = FitMode.ConstantWidth;

    [Header("Target")]
    [Tooltip("대상: Cinemachine Virtual Camera (우선)")]
    public CinemachineVirtualCamera vcam;

    [Tooltip("Cinemachine을 사용하지 않을 경우 직교 카메라 직접 지정")]
    public Camera orthoCamera;

    private const float EPS = 0.0005f;
    private Rect lastRect;
    private float lastSizeApplied = -1f;
    private int lastW, lastH;

    // ─────────────────────────────────────────────────────────────────────────────
    // [생명주기] 1회 캐시 및 강제 적용
    private void OnEnable()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();
        EnsureOrtho();
        Apply(force: true);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
    }

#if UNITY_EDITOR
    // ─────────────────────────────────────────────────────────────────────────────
    // [에디터 전용] 게임 뷰 변화 감지
    private void EditorUpdate()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }
#endif

    // ─────────────────────────────────────────────────────────────────────────────
    // [프레임 루프] 런타임 변화 반영
    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// [보조 메서드] 대상이 직교 모드인지 보장합니다.
    /// </summary>
    private void EnsureOrtho()
    {
        if (vcam)
        {
            var lens = vcam.m_Lens;
            lens.Orthographic = true;
            vcam.m_Lens = lens;
        }
        else if (orthoCamera)
        {
            orthoCamera.orthographic = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// [보조 메서드] 레터박스 적용 이후 '실제 가시영역' 화면비를 계산합니다.
    /// </summary>
    private float GetVisibleAspect(Camera outputCam)
    {
        if (!outputCam) return (float)Screen.width / Mathf.Max(1, Screen.height);

        Rect r = outputCam.rect; // 레터/필러박스 적용 결과
        float vw = Screen.width  * Mathf.Max(r.width,  EPS);
        float vh = Screen.height * Mathf.Max(r.height, EPS);
        return vw / vh;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// [핵심 메서드] 기준값과 현재 가시 화면비를 비교해 OrthoSize를 보정합니다.
    /// </summary>
    public void Apply(bool force = false)
    {
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0) return;
        if (!force && (w == lastW && h == lastH)) return;
        lastW = w; lastH = h;

        // 출력 카메라 선택(우선순위: vcam가 렌더링하는 메인 카메라 -> 직접 지정 카메라 -> Camera.main)
        Camera outCam = null;
        if (vcam && vcam.VirtualCameraGameObject)
            outCam = Camera.main; // 일반적으로 vcam은 메인 카메라를 구동
        if (!outCam) outCam = orthoCamera;
        if (!outCam) outCam = Camera.main;
        if (!outCam) return;

        Rect r = outCam.rect;
        float curAspect = GetVisibleAspect(outCam);
        float nextSize = referenceOrthoSize;

        switch (fitMode)
        {
            case FitMode.ConstantWidth:
                // 가로폭을 기준 화면비에서의 폭으로 고정
                nextSize = referenceOrthoSize * (referenceAspect / curAspect);
                break;
            case FitMode.ConstantHeight:
                // 세로폭 고정
                nextSize = referenceOrthoSize;
                break;
        }

        bool rectChanged = force ||
            Mathf.Abs(lastRect.x - r.x) > EPS ||
            Mathf.Abs(lastRect.y - r.y) > EPS ||
            Mathf.Abs(lastRect.width - r.width) > EPS ||
            Mathf.Abs(lastRect.height - r.height) > EPS;

        bool sizeChanged = Mathf.Abs(lastSizeApplied - nextSize) > EPS;

        if (rectChanged || sizeChanged)
        {
            // 대상에 적용
            if (vcam)
            {
                vcam.m_Lens.OrthographicSize = nextSize;
            }
            else if (outCam)
            {
                outCam.orthographicSize = nextSize;
            }

            lastRect = r;
            lastSizeApplied = nextSize;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // [에디터 값 수정 시] 즉시 재적용
    private void OnValidate()
    {
        EnsureOrtho();
        Apply(force: true);
    }
}
