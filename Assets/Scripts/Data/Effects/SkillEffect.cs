using System;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// NOTE
// - 두 경로 모두 제공: Apply(Job, Job) / Apply(Combatant, Combatant)
// - 현재는 "동작 최소화"만 하고, 세부 로직은 추후에 채우기 쉽게 골격만 유지
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class SkillEffect
{
    public int duration;

    public virtual void Apply(Combatant user, Combatant target) { }
}

// ─────────────────────────────────────────────────────────────────────────────
// Damage / Heal
// ─────────────────────────────────────────────────────────────────────────────

// 기본 데미지
[Serializable]
public class DamageEffect : SkillEffect
{
    public int damage;                  // 데미지
    public int correctionHitOverride;   // 명중 보정

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;

        // 명중 판정
        int atkHit = HitFormula.ResolveHit(user);
        int tgtSpd = HitFormula.ResolveSpeed(target);
        int corr   = (correctionHitOverride != 0) ? correctionHitOverride : SkillCastContext.CorrectionHit;

        bool isHit = HitFormula.RollToHit(atkHit, tgtSpd, corr, out int chance, out float roll);
        if (!isHit)
        {
            Debug.Log($"[HitCheck/MISS] {user?.DisplayName} → {target.DisplayName} | chance={chance}%, roll={roll:F1} | hit={atkHit}, spd={tgtSpd}, corr={corr}");
            return;
        }
        Debug.Log($"[HitCheck/HIT]  {user?.DisplayName} → {target.DisplayName} | chance={chance}%, roll={roll:F1} | hit={atkHit}, spd={tgtSpd}, corr={corr}");

        // 2) 피해 계산/적용
        int def = DamageFormula.ResolveDefense(target);
        int final = DamageFormula.ComputeFinalDamage(damage, def);

        target.ApplyDamage(final);
        Debug.Log($"[Effect/Damage] raw={damage}, def={def}, final={final}");
    }
}

//-------------- 마법 데미지 ---------------
[Serializable]
public class MagicDamageEffect : SkillEffect
{
    public int damage;
    public int correctionHitOverride;

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;

        // 명중 판정
        int atkHit = HitFormula.ResolveHit(user);
        int tgtSpd = HitFormula.ResolveSpeed(target);
        int corr   = (correctionHitOverride != 0) ? correctionHitOverride : SkillCastContext.CorrectionHit;

        bool isHit = HitFormula.RollToHit(atkHit, tgtSpd, corr, out int chance, out float roll);
        if (!isHit) { Debug.Log($"[Hit/MISS Magic] chance={chance} roll={roll:F1}"); return; }

        // 저항 반영
        int res   = target.GetCurrentResistance();              // 저항 조회
        int final = Mathf.Max(0, damage - Mathf.Max(0, res));   // 정수 차감

        target.ApplyDamage(final);
        Debug.Log($"[MagicDamage] raw={damage}, res={res}, final={final}");
    }
}

/* ------------- 표식 데미지 ------------- */
[Serializable]
public class SignDamageEffect : SkillEffect
{
    public int damage;
    public float bonusOnSign = 0.15f;   // 표식 추가 피해 배율
    public int correctionHitOverride;

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target || !target.IsAlive) return;

        // 명중 판정
        int atkHit = HitFormula.ResolveHit(user);
        int tgtSpd = HitFormula.ResolveSpeed(target);
        int corr   = (correctionHitOverride != 0) ? correctionHitOverride : SkillCastContext.CorrectionHit;

        bool isHit = HitFormula.RollToHit(atkHit, tgtSpd, corr, out int chance, out float roll);
        if (!isHit)
        {
            Debug.Log($"[HitCheck/MISS] {user?.DisplayName} → {target.DisplayName} | chance={chance}%, roll={roll:F1} | hit={atkHit}, spd={tgtSpd}, corr={corr} (SignDamage)");
            return;
        }
        Debug.Log($"[HitCheck/HIT]  {user?.DisplayName} → {target.DisplayName} | chance={chance}%, roll={roll:F1} | hit={atkHit}, spd={tgtSpd}, corr={corr} (SignDamage)");

        // 표식 보너스 반영
        bool marked = target.HasDebuff(BuffType.Sign) || target.Marked;
        int raw = marked ? Mathf.RoundToInt(damage * (1f + bonusOnSign)) : damage;

        int def = DamageFormula.ResolveDefense(target);
        int final = DamageFormula.ComputeFinalDamage(raw, def);

        target.ApplyDamage(final);
        Debug.Log($"[Effect/SignDamage] base={damage}, marked={marked}, raw={raw}, def={def}, final={final}");
    }
}

// 명중 보정 전달용
public static class SkillCastContext
{
    public static int CorrectionHit { get; set; } = 0;
}

// 명중률 계산
static class HitFormula
{
    /// <summary> 명중값 호출</summary>
    public static int ResolveHit(Combatant c)
        => c ? Mathf.Max(0, c.GetCurrentHit()) : 0;

    /// <summary> 민첩값 호출</summary>
    public static int ResolveSpeed(Combatant c)
        => c ? Mathf.Max(0, c.EffectiveSpeed) : 0;

    /// <summary> 명중 확률 판정 </summary>
    public static bool RollToHit(int attackerHit, int targetSpd, int correction,
                                 out int chance, out float roll)
    {
        chance = Mathf.Clamp(attackerHit - (targetSpd * 3) + correction, 0, 100);
        roll = UnityEngine.Random.value * 100f;
        Debug.Log($"[HitFormula] atkHit={attackerHit}, tgtSpd={targetSpd}, corr={correction}, chance={chance}, roll={roll:F1}");
        return roll < chance;
    }
}

/* =========================================================
 *  방어 적용 데미지 계산
 *  final = raw - (raw * def / 100)   // 소수점 내림
 * =========================================================*/
static class DamageFormula
{
    /// <summary> 방어값 조회 </summary>
    public static int ResolveDefense(Combatant target)
        => target ? Mathf.Max(0, target.GetCurrentDefense()) : 0;

    /// <summary> 최종 피해 계산(정수 내림) </summary>
    public static int ComputeFinalDamage(int raw, int defense)
    {
        raw = Mathf.Max(0, raw);
        defense = Mathf.Max(0, defense);
        int final = raw - (raw * defense) / 100; // 정수연산 → 자동 내림
        return Mathf.Max(0, final);
    }
}

//============ 회복 ===============
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
    public int value;       // 양수 버프, 음수 디버프
    public BuffType ability;

    public override void Apply(Combatant user, Combatant target)
    {
        if (!target) return;

        if (ability == BuffType.Remove)
        {
            int n = target.RemoveAllDebuffs(alsoClearNegativeAbilityMods: true);
            Debug.Log($"[Effect/Remove] {user?.DisplayName} cleansed {target.DisplayName} ({n} removed)");
            return;
        }

        // 먼저 등록 시도(First wins). 실패하면 아래 UI 태깅도 하지 않음.
        bool added = target.TryAddAbilityModFirstWins(ability, value, duration);
        if (!added)
        {
            Debug.Log($"[AbilityBuff] ignored duplicate ({ability}, {(value >= 0 ? "+" : "")}{value}) on {target.DisplayName}");
            return;
        }

        if (value >= 0) target.AddBuff(ability, duration);
        else target.AddDebuff(ability, duration);

        Debug.Log($"[Effect/AbilityMod] {user?.DisplayName} → {target.DisplayName} : {ability} {(value >= 0 ? "+" : "")}{value} ({duration}T)");
    }

}

// 디버프 제거
public class CleanseDebuffEffect : SkillEffect
{
    public BuffType[] removeTypes = new BuffType[]
    {
        BuffType.Burn,     // 화상
        BuffType.Poison,   // 중독
        BuffType.Bleeding,  // 출혈
        BuffType.Faint,
        BuffType.Taunt
    };

    public override void Apply(Combatant user, Combatant target)
    {
        if (target == null || !target.IsAlive) return;
        if (removeTypes == null || removeTypes.Length == 0) return;

        int removed = 0;

        // 지정된 디버프들을 개별적으로 제거 (스택/지속과 무관하게 즉시)
        for (int i = 0; i < removeTypes.Length; i++)
        {
            var t = removeTypes[i];
            // Combatant에 앞서 추가한 RemoveDebuff(BuffType)가 있다고 가정
            bool ok = target.RemoveDebuff(t);
            if (ok) removed++;
        }

        // 아이콘/상태판 UI 즉시 갱신 훅이 있다면 호출
        try
        {
            var m = target.GetType().GetMethod("RefreshEffectIcons", Type.EmptyTypes);
            if (m != null) m.Invoke(target, null);
        }
        catch {  }

        Debug.Log($"[Effect/CleanseDebuff] {user?.DisplayName} → {target.DisplayName} | removed={removed}/{removeTypes.Length}");
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
// 개별 디버프 타입들 — 최소한 "타입만" 명시
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
        // duration은 BattleManager 쪽에서 '도발자 자신의 턴 시작'에 해제되므로 여기선 별도 타이머 불필요
    }
}
