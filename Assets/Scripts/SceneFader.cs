using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [씬 지역 전용] 이 컴포넌트가 붙은 씬에서만 페이드 인/아웃을 수행.
/// - 씬 시작 시 자동 페이드 인 (alpha 1 → 0)
/// - 퍼블릭 API로 페이드 아웃 후 씬 전환 제공
/// - 모바일 빌드 고려: unscaledDeltaTime 사용, CanvasGroup 기반
/// </summary>

[DefaultExecutionOrder(-1000)] // 가능한 한 이르게 초기화
public class SceneFader : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("검은색 전면 이미지를 가진 CanvasGroup. (필수)")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    [Tooltip("씬 시작 시 자동 페이드 인")]
    [SerializeField] private bool autoFadeInOnStart = true;

    [Header("Durations (sec)")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.4f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.4f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easeIn = AnimationCurve.EaseInOut(0, 1, 1, 0); // 1→0
    [SerializeField] private AnimationCurve easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1); // 0→1

    [Header("Raycast")]
    [Tooltip("페이드 중에는 입력 차단")]
    [SerializeField] private bool blockRaycastsWhileFading = true;

    // 내부 상태
    private bool _isFading;

    void Awake()
    {
        // [역할] 필수 레퍼런스 보정 및 초기 알파 보장
        if (!canvasGroup)
            canvasGroup = GetComponent<CanvasGroup>();

        if (!canvasGroup)
        {
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);
            if (!canvasGroup)
            {
                Debug.LogError("[SceneFader] CanvasGroup이 필요합니다. 페이드를 비활성화합니다.");
                enabled = false;
                return;
            }
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (autoFadeInOnStart && canvasGroup)
            canvasGroup.alpha = 1f;
    }

    async void Start()
    {
        // [역할] 씬 시작 페이드 인(검정 → 밝게)
        if (autoFadeInOnStart)
        {
            await FadeInAsync();
        }
    }

    /// <summary>
    /// [역할] alpha 1→0으로 페이드 인(화면 밝아짐). 씬 시작 시 호출.
    /// </summary>
    public async Task FadeInAsync()
    {
        if (!enabled || _isFading || !canvasGroup) return;
        _isFading = true;

        if (blockRaycastsWhileFading) canvasGroup.blocksRaycasts = true;

        float startA = canvasGroup.alpha; // 현재 알파에서 시작
        float t = 0f, dur = Mathf.Max(0.0001f, fadeInDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(t / dur);
            // [역할] easeIn(0→1) 값을 사용해 startA→0으로 보간
            canvasGroup.alpha = Mathf.Lerp(startA, 0f, 1f - Mathf.Clamp01(easeIn.Evaluate(x)));
            await Task.Yield();
        }
        canvasGroup.alpha = 0f;

        canvasGroup.blocksRaycasts = false;
        _isFading = false;
    }

    /// <summary>
    /// [역할] alpha 0→1로 페이드 아웃(화면 어두워짐). 씬 이동 전 호출.
    /// </summary>
    public async Task FadeOutAsync()
    {
        if (!enabled || _isFading || !canvasGroup) return;
        _isFading = true;

        if (blockRaycastsWhileFading) canvasGroup.blocksRaycasts = true;

        float t = 0f, dur = Mathf.Max(0.0001f, fadeOutDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(t / dur);
            canvasGroup.alpha = Mathf.Clamp01(easeOut.Evaluate(x)); // 0→1
            await Task.Yield();
        }
        canvasGroup.alpha = 1f;

        _isFading = false;
    }

    /// <summary>
    /// [역할] 페이드 아웃 후 지정 씬으로 전환.
    /// (이 API는 "이전 단계에서 아직 어두워지지 않았을 때"만 사용 권장)
    /// </summary>
    public async Task FadeOutAndLoadSceneAsync(string sceneName)
    {
        if (!gameObject.scene.IsValid()) return;
        if (canvasGroup && canvasGroup.alpha < 0.99f) // 이미 검정이면 생략
            await FadeOutAsync();

        SceneManager.LoadScene(sceneName);
    }

    /// <summary> [역할] 현재 페이드 중인지 여부 </summary>
    public bool IsFading() => _isFading;

    /// <summary> [역할] 현재 화면이 충분히 어두운(=검정) 상태인지 </summary>
    public bool IsScreenBlack => canvasGroup && canvasGroup.alpha >= 0.99f;
}
