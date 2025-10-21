using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("현재 체력")]
    [SerializeField] private Image fillCurrent;          // (Filled) 현재 체력 게이지
    [SerializeField] private TMP_Text hpText;            // 현재 체력 텍스트

    [Header("이후 체력(프리뷰)")]
    [SerializeField] private Image fillFuture;           // 미래 상태(프리뷰) 레이어
    [SerializeField] private Color healColor   = new Color(0.2f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color damageColor = new Color(1f,   0.5f, 0.1f, 1f);
    [SerializeField] private Color dotColor    = new Color(1f,   0f,   0.3f, 1f);
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.7f, 1f, 1f);

    [Header("옵션")]
    [SerializeField] private bool clampOverheal = true;  // 과치유면 Max로 클램프
    [SerializeField] private bool showText      = true;  // HP 텍스트 표시 여부

    // 내부 상태
    private Combatant bound;             // ★ 선택적 바인딩 대상(전투 UI 등)
    private int curHp, maxHp;            // 현재/최대 체력(내부 유지)
    private int previewTargetHp = -1;    // 프리뷰 타겟 HP(없으면 -1)
    private Coroutine animCo;            // 회복 커밋 (빨간 바)
    private Coroutine previewCo;         // 의무실 고정 (초록)

    private void Awake()
    {
        if (fillFuture) fillFuture.enabled = false;
    }

    //================= 공개 API (공용 사용) =================

    /// <summary>
    /// (옵션) 전투 유닛과 실시간 바인딩. 성공 시 true 반환.
    /// 다른 UI(의무실 등)에서는 바인딩 없이 Set/ShowPreview만 써도 됨.
    /// </summary>
    public bool TryBind(Combatant c)
    {
        // 동일 대상이면 무시
        if (bound == c && bound != null) return true;

        // 기존 바인딩 해제
        Unbind();

        if (c == null) return false;

        bound = c;
        bound.OnHpChanged += OnHpChanged;     // 체력 변화 이벤트 수신
        OnHpChanged(bound.currentHp, bound.maxHp); // 즉시 1회 동기화
        return true;
    }

    /// <summary>
    /// 현재 바인딩을 해제(이벤트 누수 방지). 바인딩이 없어도 안전 호출 가능.
    /// </summary>
    public void Unbind()
    {
        if (bound != null)
        {
            bound.OnHpChanged -= OnHpChanged;
            bound = null;
        }
    }

    /// <summary>
    /// 외부에서 수동으로 HP/Max를 즉시 반영(전투·의무실·기타 공용).
    /// </summary>
    public void Set(int hp, int max)
    {
        maxHp = Mathf.Max(1, max);
        curHp = Mathf.Clamp(hp, 0, maxHp);
        SetFill(fillCurrent, Ratio(curHp, maxHp));
        if (showText && hpText) hpText.text = $"{curHp}/{maxHp}";

        // Set을 호출하면 프리뷰가 유효 범위를 벗어날 수 있으니 필요 시 숨김
        if (previewTargetHp >= 0 && previewTargetHp == curHp) HideFuture();
    }

    //================= 프리뷰(미래 상태) =================

    public enum PreviewType { Heal, Damage, Dot, Shield }

    /// <summary>
    /// 현재 값 기준 delta 만큼 변화했을 때의 프리뷰를 표시(+힐/-피해 등).
    /// </summary>
    public void ShowPreviewDelta(int delta, PreviewType type)
    {
        int target = curHp + delta;
        ShowPreviewTo(target, type);
    }

    // 역할: delta만큼 회복/피해 등의 프리뷰를 'dur' 동안 부드럽게 표시
    public void ShowPreviewDeltaAnimated(int delta, PreviewType type, float dur = 0.25f)
    {
        ShowPreviewToAnimated(curHp + delta, type, dur);
    }

    /// <summary>
    /// 절대값 targetHp로 변화했을 때의 프리뷰를 표시(스킬 계산으로 타겟이 이미 있을 때).
    /// </summary>
    public void ShowPreviewTo(int targetHp, PreviewType type)
    {
        if (!fillFuture) return;

        int t = Mathf.Clamp(targetHp, 0, maxHp);
        if (type == PreviewType.Heal && clampOverheal)
            t = Mathf.Clamp(t, 0, maxHp);

        previewTargetHp = t;

        // 색상 지정
        fillFuture.color = type switch
        {
            PreviewType.Damage => damageColor,
            PreviewType.Dot => dotColor,
            PreviewType.Shield => shieldColor,
            _ => healColor
        };

        fillFuture.enabled = true;                         // 프리뷰 레이어 켜기
        SetFill(fillFuture, Ratio(previewTargetHp, maxHp));// 미래 길이 적용
    }
    
    // 역할: 타겟 HP까지 프리뷰 바(fillFuture)를 'dur' 동안 부드럽게 증가
    public void ShowPreviewToAnimated(int targetHp, PreviewType type, float dur = 0.25f)
    {
        if (!fillFuture) return;

        int t = Mathf.Clamp(targetHp, 0, maxHp);
        if (type == PreviewType.Heal && clampOverheal)
            t = Mathf.Clamp(t, 0, maxHp);

        previewTargetHp = t;

        fillFuture.color = type switch
        {
            PreviewType.Damage => damageColor,
            PreviewType.Dot    => dotColor,
            PreviewType.Shield => shieldColor,
            _                  => healColor
        };

        float start = fillFuture.enabled ? fillFuture.fillAmount : Ratio(curHp, maxHp);
        float end   = Ratio(previewTargetHp, maxHp);

        fillFuture.enabled = true;

        // 이전 프리뷰 애니메이션이 돌고 있으면 정지
        if (previewCo != null) StopCoroutine(previewCo);
        previewCo = StartCoroutine(CoAnimatePreview(start, end, Mathf.Max(0f, dur)));
    }

    /// <summary>
    /// 현재 표시 중인 프리뷰를 제거(미래 레이어만 숨김).
    /// </summary>
    public void ClearPreview()
    {
        previewTargetHp = -1;
        // 🔹 프리뷰 애니메이션 중이면 정지
        if (previewCo != null) { StopCoroutine(previewCo); previewCo = null; }
        HideFuture();
    }

    /// <summary>
    /// 프리뷰를 실제 값으로 커밋. dur>0이면 부드럽게 이동.
    /// (주의) 바인딩된 상태에서는 Combatant 쪽 로직이 '진짜' HP를 관리하니,
    /// 여기서는 UI 보정용으로만 쓰거나, 바인딩 없이 사용하세요.
    /// </summary>
    public void CommitPreview(float dur = 0.2f)
    {
        if (previewTargetHp < 0 || previewTargetHp == curHp)
        {
            HideFuture();
            return;
        }

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(CoAnimateCommit(curHp, previewTargetHp, Mathf.Max(0f, dur)));
    }

    //================= 바운드 콜백(선택적) =================

    /// <summary>
    /// Combatant 체력 변경 시 호출되는 콜백. 현재 게이지 즉시 동기화.
    /// </summary>
    private void OnHpChanged(int hp, int max)
    {
        Set(hp, max);
        // 바운드 상태에서도 프리뷰가 cur과 같아지면 감춤(시각적 일관성)
        if (previewTargetHp == curHp) HideFuture();
    }

    //================= 내부 유틸 =================

    // 역할: 프리뷰(초록) 이미지의 fillAmount만 시간에 따라 보간
    private IEnumerator CoAnimatePreview(float from, float to, float dur)
    {
        if (dur <= 0f)
        {
            SetFill(fillFuture, to);
            previewCo = null;
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float v = Mathf.Lerp(from, to, k);
            SetFill(fillFuture, v);
            yield return null;
        }
        SetFill(fillFuture, to);
        previewCo = null;
    }

    /// <summary>현재→프리뷰 타겟으로 부드럽게 이동시키는 애니메이션.</summary>
    private IEnumerator CoAnimateCommit(int fromHp, int toHp, float dur)
    {
        if (dur <= 0f)
        {
            Set(toHp, maxHp);
            ClearPreview();
            animCo = null;
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            int hp = Mathf.RoundToInt(Mathf.Lerp(fromHp, toHp, k));
            Set(hp, maxHp);
            yield return null;
        }

        Set(toHp, maxHp);
        ClearPreview();
        animCo = null;
    }

    /// <summary>Image.fillAmount 세이프 반영.</summary>
    private static void SetFill(Image img, float ratio)
    {
        if (!img) return;
        img.fillAmount = Mathf.Clamp01(ratio);
    }

    /// <summary>0 분모 보호 포함 안전한 비율 계산.</summary>
    private static float Ratio(int a, int b) => (b <= 0) ? 0f : (float)a / b;

    /// <summary>프리뷰 레이어 비활성.</summary>
    private void HideFuture()
    {
        if (fillFuture) fillFuture.enabled = false;
    }

    private void OnDisable()
    {
        if (animCo != null) { StopCoroutine(animCo); animCo = null; }
        if (previewCo != null) { StopCoroutine(previewCo); previewCo = null; } // 🔹 누수 방지
    }

    private void OnDestroy()
    {
        Unbind(); // 안전 해제
    }
}
