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
// Damage / Heal
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class DamageEffect : SkillEffect
{
    public int damage;

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;
        int before = target.currentHp;                // 디버그용 나중에 삭제
        // 전투 런타임 경로(권장)
        target.ApplyDamage(Mathf.Max(0, damage));

        Debug.Log($"[Effect/Damage] {user?.DisplayName} → {target.DisplayName} : -{damage} HP ({before}→{target.currentHp})");
    }
}

public class SignDamageEffect : SkillEffect
{
    public int damage;
    public float bonusOnSign = 0.15f;     // 표식 추가 피해

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;

        bool marked = target.HasDebuff(BuffType.Sign) || target.Marked;
        int final = marked ? Mathf.RoundToInt(damage * (1f + bonusOnSign)) : damage;

        int before = target.currentHp;                // 디버그용 나중에 삭제

        target.ApplyDamage(final);

        Debug.Log($"[Effect/SignDamage] {user?.DisplayName} → {target.DisplayName} : base={damage}, marked={marked}, -{final} ({before}→{target.currentHp})");

    }
}

[Serializable]
public class HealEffect : SkillEffect
{
    public int amount;
    public bool percent = false;        // 퍼센트 회복 사용 여부
    public float rate = 0.0f;           // percent == true일 때 사용

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;

        int heal = amount;

        if (percent)
        {
            heal = Mathf.Max(1, Mathf.FloorToInt(target.maxHp * rate));
        }

        int before = target.currentHp;                // 디버그용 나중에 삭제

        target.HealHp(heal);
        Debug.Log($"[Effect/Heal] {user?.DisplayName} → {target.DisplayName} : +{heal} HP ({before}→{target.currentHp})");
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
        if (!target) return;

        // 최소 동작: 영웅이면 Job에 위임(몬스터는 추후 확장)
        target?.AddBuff(ability, duration); // 능력치 버프는 Buff
        
        Debug.Log($"[Effect/Buff] {user?.DisplayName} → {target.DisplayName} : +{ability} ({duration}T)");
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
        float r = UnityEngine.Random.value;
        if (r > probability)
        {
            Debug.Log($"[Effect/Debuff:MISS] {user?.DisplayName} → {target.DisplayName} : {DebuffType} (p={probability:F2}, roll={r:F2})");
            return;
        }
        target.AddDebuff(DebuffType, duration);
        Debug.Log($"[Effect/Debuff] {user?.DisplayName} → {target.DisplayName} : {DebuffType} ({duration}T, p={probability:F2}, roll={r:F2})");
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
// 개별 디버프 타입들 — 최소한 "타입만" 명시 (세부 효과는 추후 확장)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable] public class PoisonEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Poison; }
[Serializable] public class BleedingEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Bleeding; }
[Serializable] public class BurnEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Burn; }
//------------------------------------------------------------------------------------

[Serializable] public class SignEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Sign; }
[Serializable] public class FaintEffect : DebuffEffect { public override BuffType DebuffType => BuffType.Faint; }
// 도발: 현재는 "버프 타입만" 부여. 강제 타겟팅 등의 세부는 추후 Combatant/AI에서 처리.
[Serializable]
public class TauntEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Taunt;

    public override void Apply(Combatant user, Combatant target)
    {
        if (BattleManager.Instance == null || user == null || !user.IsAlive) return;

        // 도발 보호자는 '항상 시전자'
        BattleManager.Instance.BeginTaunt(user);
        Debug.Log($"[Effect/Taunt] {user.DisplayName} 가 도발 시작 (duration={duration})");
        // duration은 BM 쪽에서 '도발자 자신의 턴 시작'에 해제되므로 여기선 별도 타이머 불필요
    }
}
