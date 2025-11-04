using System.Collections;
using UnityEngine;

/// <summary>
/// [역할] 로비에서 Idle과 3개 춤 상태를 섞어 재생하는 컨트롤러
/// - 씬 진입/탭 전환 직후: Idle 잠깐 → 랜덤 춤 시작
/// - 각 춤이 끝나면: Idle 잠깐 → 다음 랜덤 춤
/// - 같은 춤 연속 방지 옵션 제공
/// - Time.timeScale과 무관하게 동작(실시간 대기)
/// </summary>
public class LobbyRandomAnimator : MonoBehaviour
{
    [Header("Animator & States")]
    [Tooltip("캐릭터의 Animator")]
    public Animator animator;

    [Tooltip("무작위로 재생할 '춤' 상태 이름들 (Animator 상태명)")]
    public string[] danceStates; // 예: "Dance_A", "Dance_B", "Dance_C"

    [Tooltip("danceStates와 1:1로 매칭되는 클립(길이 측정용)")]
    public AnimationClip[] danceClips;

    [Tooltip("기본 Idle 상태 이름")]
    public string idleState = "Idle";

    [Header("Timings (seconds)")]
    [Tooltip("상태 전환시 크로스페이드 시간")]
    public float crossFadeTime = 0.12f;

    [Tooltip("씬 처음/탭 전환 직후 Idle 유지 시간 범위")]
    public Vector2 firstIdleRange = new Vector2(0.4f, 1.0f);

    [Tooltip("각 춤 사이 Idle 유지 시간 범위")]
    public Vector2 idleBetweenRange = new Vector2(0.4f, 1.2f);

    [Tooltip("춤과 춤 사이의 추가 랜덤 대기(Idle 외, 연출용)")]
    public Vector2 extraWaitRange = new Vector2(0.2f, 0.6f);

    [Header("Options")]
    [Tooltip("활성화될 때 자동으로 시작")]
    public bool playOnEnable = true;

    [Tooltip("같은 춤이 연속으로 나오지 않게 함")]
    public bool preventImmediateRepeat = true;

    private Coroutine loop;
    private int lastIndex = -1;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (playOnEnable) StartRandomLoop();  // [역할] 활성화 시 자동 시작
    }

    void OnDisable()
    {
        StopRandomLoop();                     // [역할] 비활성화 시 정지
    }

    /// <summary>
    /// [역할] 무작위 춤 루프를 시작(이미 동작 중이면 무시)
    /// </summary>
    public void StartRandomLoop()
    {
        if (loop != null) return;
        loop = StartCoroutine(CoLoop());
    }

    /// <summary>
    /// [역할] 무작위 춤 루프를 정지
    /// </summary>
    public void StopRandomLoop()
    {
        if (loop == null) return;
        StopCoroutine(loop);
        loop = null;
    }

    /// <summary>
    /// [역할] 탭 전환/로비 복귀 시 외부에서 호출하면
    /// Idle을 잠깐 보여준 뒤 랜덤 루프를 재개
    /// </summary>
    public void RestartWithIdleIntro()
    {
        StopRandomLoop();
        loop = StartCoroutine(CoLoop(idleIntro: true));
    }

    /// <summary>
    /// [역할] 코루틴 메인 루프:
    /// - (선택) Idle 인트로
    /// - 랜덤 춤 재생 → Idle → (추가대기) → 다음 춤 …
    /// </summary>
    private IEnumerator CoLoop(bool idleIntro = true)
    {
        if (!IsConfigValid())
        {
            Debug.LogWarning("[LobbyRandomAnimator] 설정이 올바르지 않습니다.");
            yield break;
        }

        var waitEOF = new WaitForEndOfFrame();

        // --- 1) Idle 인트로 ---
        if (idleIntro)
        {
            CrossFade(idleState);
            yield return RealtimeWait(Random.Range(firstIdleRange.x, firstIdleRange.y), waitEOF);
        }

        // --- 2) 무한 루프: 춤 → Idle → 추가대기 ---
        while (true)
        {
            int idx = NextIndex();
            string state = danceStates[idx];
            AnimationClip clip = danceClips[idx];

            // 2-1) 춤 재생
            CrossFade(state);
            yield return RealtimeWait(Mathf.Max(0.01f, clip.length), waitEOF);

            // 2-2) Idle 사이 끼우기
            CrossFade(idleState);
            yield return RealtimeWait(Random.Range(idleBetweenRange.x, idleBetweenRange.y), waitEOF);

            // 2-3) (선택) 조금 더 쉼을 주고 다음 춤
            float extra = Random.Range(extraWaitRange.x, extraWaitRange.y);
            if (extra > 0f) yield return RealtimeWait(extra, waitEOF);
        }
    }

    /// <summary>
    /// [역할] 설정 유효성 확인 (Animator, 배열 길이 일치 등)
    /// </summary>
    private bool IsConfigValid()
    {
        return animator != null &&
               danceStates != null && danceStates.Length > 0 &&
               danceClips != null && danceClips.Length == danceStates.Length &&
               !string.IsNullOrEmpty(idleState);
    }

    /// <summary>
    /// [역할] 같은 춤 연속 방지 옵션을 고려해 다음 인덱스 선택
    /// </summary>
    private int NextIndex()
    {
        if (danceStates.Length == 1) return 0;

        int idx = Random.Range(0, danceStates.Length);
        if (preventImmediateRepeat)
        {
            while (idx == lastIndex)
                idx = Random.Range(0, danceStates.Length);
        }
        lastIndex = idx;
        return idx;
    }

    /// <summary>
    /// [역할] Animator.CrossFade 래퍼(레이어=0, 시작지점=0)
    /// </summary>
    private void CrossFade(string stateName)
    {
        animator.CrossFade(stateName, crossFadeTime, 0, 0f);
    }

    /// <summary>
    /// [역할] Time.timeScale 영향을 받지 않는 실시간 대기
    /// </summary>
    private static IEnumerator RealtimeWait(float seconds, WaitForEndOfFrame waitEOF)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return waitEOF;
        }
    }
}
