using System.Collections.Generic;
using UnityEngine;
using Game.Visual;

/// <summary>
/// [역할] Combatant가 들고 있는 Animator를 찾아서
/// - 공격/피격 트리거, 속도 파라미터를 통일 인터페이스로 제공.
/// - 영웅/몬스터 프리팹 차이를 가림.
/// </summary>

[DisallowMultipleComponent]
public class CombatAnimator : MonoBehaviour
{
    [Header("Animator & Placeholders (Base Controller에 존재하는 기본 클립들)")]
    [SerializeField] private Animator anim;                  // [역할] 실제 Animator 핸들(수동/자동 할당)
    [SerializeField] private AnimationClip baseAttackClip;   // [역할] Attack 상태 자리표시자(키 클립)
    [SerializeField] private AnimationClip baseIdleClip;     // [선택] Idle 자리표시자
    [SerializeField] private AnimationClip baseRunClip;      // [선택] Run 자리표시자
    [SerializeField] private AnimationClip baseHitClip;      // [선택] Hit 자리표시자

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash    = Animator.StringToHash("Hit");

    void Reset()
    {
        // [역할] 에디터에서 컴포넌트 붙일 때 1회 자동 탐색
        TryResolveAnimator();
    }

    void Awake()
    {
        // [역할] 런타임에서도 혹시 비었으면 재탐색(프리팹 변형/중첩 대비)
        TryResolveAnimator();

        if (anim)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
    }

    /// <summary>
    /// [역할] Animator를 안전하게 찾는 유틸 (자식/자기 자신/부모까지 탐색)
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
    /// [역할] 영웅 정의를 받아 베이스 컨트롤러/아바타를 장착하고,
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

        // 자리표시자 유효성 검사 (특히 Attack은 필수)
        if (!baseAttackClip)
            Debug.LogError("[CombatAnimator] baseAttackClip(자리표시자)이 비어 있습니다. Attack_Placeholder를 연결하세요.", this);

        var aoc = new AnimatorOverrideController(anim.runtimeAnimatorController);
        var list = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        // [역할] 공격 자리표시자 → 영웅 전용 공격 클립
        if (baseAttackClip && def.overrideAttack)
            list.Add(new(baseAttackClip, def.overrideAttack));

        // [역할] 선택: Idle/Run/Hit 자리표시자 교체
        if (baseIdleClip && def.idleOverride) list.Add(new(baseIdleClip, def.idleOverride));
        if (baseRunClip  && def.runOverride)  list.Add(new(baseRunClip,  def.runOverride));
        if (baseHitClip  && def.hitOverride)  list.Add(new(baseHitClip,  def.hitOverride));

        if (list.Count == 0)
            Debug.LogWarning("[CombatAnimator] 적용할 오버라이드가 없습니다. (Attack/Idle/Run/Hit 중 최소 하나 지정 권장)", this);

        aoc.ApplyOverrides(list);
        anim.runtimeAnimatorController = aoc;

        // [역할] Attack 상태에 모션이 실제 장착되어 있는지 최종 점검(자리표시자 미장착 보호)
        if (baseAttackClip == null)
            Debug.LogWarning("[CombatAnimator] 경고: Attack 자리표시자 자체가 null입니다. 컨트롤러 Attack 상태의 Motion을 확인하세요.", this);
    }

    /// <summary> [역할] 이동 블렌드(0=Idle, 1=Run) </summary>
    public void SetMove01(float v)
    {
        if (!anim) { TryResolveAnimator(); return; }
        anim.SetFloat(SpeedHash, Mathf.Clamp01(v));
    }

    /// <summary> [역할] 공격 트리거 (다른 트리거 잔여 제거 후 발화) </summary>
    public void PlayAttack()
    {
        if (!anim) { TryResolveAnimator(); return; }
        anim.ResetTrigger(HitHash);
        anim.ResetTrigger(AttackHash);
        anim.SetTrigger(AttackHash);
    }

    /// <summary> [역할] 피격 트리거 </summary>
    public void PlayHit()
    {
        if (!anim) { TryResolveAnimator(); return; }
        anim.ResetTrigger(AttackHash);
        anim.ResetTrigger(HitHash);
        anim.SetTrigger(HitHash);
    }

    /// <summary> [역할] (트리거 직후 1프레임) Attack 상태 총 길이 추정 </summary>
    public float GuessAttackTotalSec(float fallback = 0.9f)
    {
        if (!anim || anim.runtimeAnimatorController == null) return fallback;
        var next = anim.GetNextAnimatorStateInfo(0);
        if (next.length > 0.01f) return next.length;
        var cur = anim.GetCurrentAnimatorStateInfo(0);
        return cur.length > 0.01f ? cur.length : fallback;
    }

    /// <summary> [역할] 총 길이에 대한 윈드업(타격 타이밍) 비율 계산 </summary>
    public float GuessAttackWindupSec(float total, float min = 0.25f, float max = 0.6f)
        => Mathf.Clamp(total * 0.35f, min, max);
}
