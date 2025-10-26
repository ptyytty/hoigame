using System.Collections.Generic;
using UnityEngine;
using Game.Visual;
using System.Collections;

/// <summary>
///  Combatant가 들고 있는 Animator 조회
/// - 공격/피격 트리거, 속도 파라미터를 통일 인터페이스로 제공.
/// - 영웅/몬스터 프리팹 차이를 가림.
/// </summary>

[DisallowMultipleComponent]
public class CombatAnimator : MonoBehaviour
{
    [Header("Animator & Placeholders (Base Controller에 존재하는 기본 클립들)")]
    [SerializeField] private Animator anim;                  //  실제 Animator 핸들(수동/자동 할당)
    [SerializeField] private AnimationClip baseAttackClip;   //  Attack 상태 자리표시자(키 클립)
    [SerializeField] private AnimationClip baseIdleClip;     //  Idle 자리표시자
    [SerializeField] private AnimationClip baseRunClip;      //  Run 자리표시자
    [SerializeField] private AnimationClip baseHitClip;      //  Hit 자리표시자

    [Header("Attack Length Unifier")]
    [SerializeField] private bool restoreSpeedOnExit = true;        // 공격 종료 시 속도 복구
    private float desiredAttackLengthSec = 2f; //  공격 길이 타깃값(초)

    private AnimationClip appliedAttackClip;

    [Header("Hit Length Unifier")]
    [SerializeField] private float desiredHitLengthSec = 1.2f;
    private AnimationClip appliedHitClip;

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash    = Animator.StringToHash("Hit");

    void Reset()
    {
        //  에디터에서 컴포넌트 붙일 때 1회 자동 탐색
        TryResolveAnimator();
    }

    void Awake()
    {
        //  런타임에서도 혹시 비었으면 재탐색(프리팹 변형/중첩 대비)
        TryResolveAnimator();

        if (anim)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
    }

    /// <summary>
    ///  Animator를 안전하게 찾는 유틸 (자식/자기 자신/부모까지 탐색)
    /// </summary>
    private void TryResolveAnimator()
    {
        if (anim && anim.runtimeAnimatorController != null) return;

        anim = GetComponent<Animator>()
            ?? GetComponentInChildren<Animator>(true)
            ?? GetComponentInParent<Animator>(true);

        if (!anim)
        {
            Debug.LogError("[CombatAnimator] Animator를 찾지 못했습니다. 인스펙터 'Anim'에 직접 할당해주세요.", this);
            return;
        }
        if (!anim.runtimeAnimatorController)
        {
            Debug.LogWarning("[CombatAnimator] Animator는 찾았지만 Controller가 없습니다. 스폰 직후 ApplyHeroDefinition를 호출해야 합니다.", this);
        }
    }

    /// <summary>
    ///  영웅 정의를 받아 베이스 컨트롤러/아바타를 장착하고,
    ///        자리표시자 → 영웅 전용 클립으로 오버라이드(AOC) 적용.
    /// </summary>
    public void ApplyHeroDefinition(HeroDefinition def)
    {
        TryResolveAnimator();
        if (!anim || def == null) return;

        if (def.baseController) anim.runtimeAnimatorController = def.baseController;
        if (def.avatar)         anim.avatar = def.avatar;

        if (!anim.runtimeAnimatorController)
        {
            Debug.LogError("[CombatAnimator] Base Controller가 없습니다. HeroDefinition.baseController를 지정하세요.", this);
            return;
        }

        if (!baseAttackClip)
            Debug.LogError("[CombatAnimator] baseAttackClip(자리표시자)이 비어 있습니다. Attack_Placeholder를 연결하세요.", this);

        var aoc = new AnimatorOverrideController(anim.runtimeAnimatorController);
        var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        if (baseAttackClip && def.overrideAttack)
        {
            list.Add(new(baseAttackClip, def.overrideAttack));
            appliedAttackClip = def.overrideAttack; // 공격 클립 캐시
        }

        if (baseIdleClip && def.idleOverride) list.Add(new(baseIdleClip, def.idleOverride));
        if (baseRunClip  && def.runOverride)  list.Add(new(baseRunClip,  def.runOverride));
        if (baseHitClip && def.hitOverride)
        {
            list.Add(new(baseHitClip, def.hitOverride));
            appliedHitClip = def.hitOverride; // 피격 클립 캐시
        }

        aoc.ApplyOverrides(list);
        anim.runtimeAnimatorController = aoc;
    }

    /// <summary>  이동 블렌드(0=Idle, 1=Run) </summary>
    public void SetMove01(float v)
    {
        if (!anim) { TryResolveAnimator(); return; }
        anim.SetFloat(SpeedHash, Mathf.Clamp01(v));
    }

    /// <summary>  공격 트리거 (다른 트리거 잔여 제거 후 발화) </summary>
    public void PlayAttack()
    {
        if (!anim) { TryResolveAnimator(); return; }

        // 1) 기본 트리거 처리
        anim.ResetTrigger(HitHash);
        anim.ResetTrigger(AttackHash);

        // 2) 길이 보정 배속 계산
        float targetLen = desiredAttackLengthSec > 0f ? desiredAttackLengthSec : 0f;
        float baseLen = 0f;

        // 우선순위: 적용된 공격 클립 길이 → 다음 상태 길이 추정 → 현재 상태 길이 → 폴백
        if (appliedAttackClip) baseLen = Mathf.Max(baseLen, appliedAttackClip.length);
        float guessed = GuessAttackTotalSec(0f);
        if (guessed > 0.01f) baseLen = Mathf.Max(baseLen, guessed);
        if (baseLen <= 0.01f) baseLen = 1.0f; // 폴백

        float speedMul = 1f;
        if (targetLen > 0.01f)
        {
            // '원래길이 / 목표길이 = 재생배속'
            speedMul = Mathf.Clamp(baseLen / targetLen, 0.2f, 4f); // 극단값 클램프(모바일 안정성)
        }

        // 3) 트리거 직전 배속 반영
        float prevSpeed = anim.speed;
        anim.speed = speedMul;

        anim.SetTrigger(AttackHash);

        // 4) 공격 끝나면 속도 복구
        if (restoreSpeedOnExit && targetLen > 0.01f)
            StartCoroutine(RestoreSpeedAfterAttack(prevSpeed, targetLen));
    }

    /// <summary>
    ///  공격 길이를 '목표길이'로 맞춘 뒤, 그 시간이 지나면 Animator.speed를 원복.
    ///        (공격 동안만 전역 속도 변경 → 다른 상태에는 영향 없음)
    /// </summary>
    private IEnumerator RestoreSpeedAfterAttack(float prev, float targetLen)
    {
        // 트리거가 먹은 다음 프레임부터 대기 시작(상태 전이 안정화)
        yield return null;

        // 목표 길이 만큼 대기(이미 공격 배속으로 재생 중이므로 그냥 목표길이를 기다리면 됨)
        yield return new WaitForSeconds(targetLen);

        if (anim) anim.speed = prev;
    }

    /// <summary>  피격 트리거 </summary>
    public void PlayHit()
    {
        if (!anim) { TryResolveAnimator(); return; }

        // 1) 트리거 간섭 제거
        anim.ResetTrigger(AttackHash);
        anim.ResetTrigger(HitHash);

        // 2) 길이 보정 배속 계산
        float targetLen = desiredHitLengthSec > 0f ? desiredHitLengthSec : 0f;
        float baseLen = 0f;

        // 우선순위: 적용된 피격 클립 길이 → 다음 상태 길이 추정 → 현재 상태 길이 → 폴백
        if (appliedHitClip) baseLen = Mathf.Max(baseLen, appliedHitClip.length);
        float guessed = GuessHitTotalSec(0f);
        if (guessed > 0.01f) baseLen = Mathf.Max(baseLen, guessed);
        if (baseLen <= 0.01f) baseLen = 0.6f; // 폴백(피격은 짧게 가정)

        float speedMul = 1f;
        if (targetLen > 0.01f)
        {
            // '원래길이 / 목표길이 = 재생배속'
            speedMul = Mathf.Clamp(baseLen / targetLen, 0.2f, 4f); // 모바일 안정성 클램프
        }

        // 3) 트리거 직전 배속 반영
        float prevSpeed = anim.speed;
        anim.speed = speedMul;

        anim.SetTrigger(HitHash);

        // 4) 피격 끝나면 속도 복구 (공격과 동일 정책 사용)
        if (restoreSpeedOnExit && targetLen > 0.01f)
            StartCoroutine(RestoreSpeedAfterHit(prevSpeed, targetLen));
    }

    private IEnumerator RestoreSpeedAfterHit(float prev, float targetLen)
    {
        // 트리거가 반영된 다음 프레임부터 카운트(전이 안정화)
        yield return null;

        // 목표 길이만큼 대기(이미 배속 적용된 상태)
        yield return new WaitForSeconds(targetLen);

        if (anim) anim.speed = prev;
    }

    /// <summary>  (트리거 직후 1프레임) Attack 상태 총 길이 추정 </summary>
    public float GuessAttackTotalSec(float fallback = 0.9f)
    {
        if (!anim || anim.runtimeAnimatorController == null) return fallback;
        var next = anim.GetNextAnimatorStateInfo(0);
        if (next.length > 0.01f) return next.length;
        var cur = anim.GetCurrentAnimatorStateInfo(0);
        return cur.length > 0.01f ? cur.length : fallback;
    }

    /// <summary>  총 길이에 대한 윈드업(타격 타이밍) 비율 계산 </summary>
    public float GuessAttackWindupSec(float total, float min = 0.25f, float max = 0.6f)
        => Mathf.Clamp(total * 0.35f, min, max);

    public float GuessHitTotalSec(float fallback = 0.6f)
    {
        if (!anim || anim.runtimeAnimatorController == null) return fallback;
        var next = anim.GetNextAnimatorStateInfo(0);
        if (next.length > 0.01f) return next.length;
        var cur = anim.GetCurrentAnimatorStateInfo(0);
        return cur.length > 0.01f ? cur.length : fallback;
    }
}
