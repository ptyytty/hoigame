using System;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// NOTE
// - MonoBehaviour 상속 제거: 데이터 코드에서 `new DamageEffect{ ... }` 등 사용 가능
// - 두 경로 모두 제공: Apply(Job, Job) / Apply(Combatant, Combatant)
// - 현재는 "동작 최소화"만 하고, 세부 로직은 추후에 채우기 쉽게 골격만 유지
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class SkillEffect
{
    public int duration;

    public virtual void Apply(Combatant user, Combatant target){}
}

// ─────────────────────────────────────────────────────────────────────────────
// Damage
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class DamageEffect : SkillEffect
{
    public int damage;

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;
        // 전투 런타임 경로(권장)
        target.ApplyDamage(Mathf.Max(0, damage));
    }
}

public class SignDamageEffect : SkillEffect
{
    public int damage;
    public int bonusOnSign = 6;     // 표식 추가 피해

    public override void Apply(Combatant user, Combatant target)
    {
        if (target == null) return;
        int dmg = damage;

        if (target.HasBuff(BuffType.Sign))
            dmg += bonusOnSign;

        target.ApplyDamage(dmg);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Ability Buff (공격/방어/명중 등 수치형 버프용 껍데기)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class AbilityBuff : SkillEffect
{
    public int value;
    public BuffType ability; // 프로젝트 기준 BuffType 사용

    public override void Apply(Combatant user, Combatant target)
    {
        // 최소 동작: 영웅이면 Job에 위임(몬스터는 추후 확장)
        target?.AddBuff(ability, duration);
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// 임의 콜백형 스킬 (기존 코드 호환용)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class SpecialSkillEffect : SkillEffect
{
    public Action<Job, Job> onApply;

    public override void Apply(Combatant user, Combatant target)
    {
        onApply?.Invoke(user != null ? user.hero : null, target != null ? target.hero : null);
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// 공통 Debuff 골격 (중독/출혈/화상/표식/기절/도발 등)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public abstract class DebuffEffect : SkillEffect
{
    public float probability = 1f;
    public abstract BuffType DebuffType { get; }

    public override void Apply(Combatant user, Combatant target)
    {
        if (target == null) return;
        if (UnityEngine.Random.value > probability) return;

        // 기본 적용: 영웅이면 Job 버프로 위임 (몬스터는 필요 시 확장)
        target.AddBuff(DebuffType, duration);
        // 몬스터 쪽 세부 로직은 추후 Combatant에 버프 시스템 도입 시 반영
    }

}

// ─────────────────────────────────────────────────────────────────────────────
// 개별 디버프 타입들 — 최소한 "타입만" 명시 (세부 효과는 추후 확장)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable] public class PoisonEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Poison; }
[Serializable] public class BleedingEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Bleeding; }
[Serializable] public class BurnEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Burn; }
[Serializable] public class SignEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Sign; }
[Serializable] public class FaintEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Faint; }
// 도발: 현재는 "버프 타입만" 부여. 강제 타겟팅 등의 세부는 추후 Combatant/AI에서 처리.
[Serializable] public class TauntEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Taunt; }
