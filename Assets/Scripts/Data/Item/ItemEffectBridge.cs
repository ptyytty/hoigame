using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemEffectSpec을 기존 SkillEffect로 변환(공용 파이프라인 입구)
/// - Damage/Heal/AbilityMod/ApplyDebuff/Cleanse/Special 매핑
/// </summary>
public class ItemEffectBridge : MonoBehaviour
{
    /// <summary>
    /// 역할: 스펙 리스트를 SkillEffect 리스트로 변환
    /// </summary>
    public static List<SkillEffect> BuildEffects(List<ItemEffectSpec> specs)
    {
        var result = new List<SkillEffect>();
        if (specs == null) return result;

        foreach (var s in specs)
        {
            switch (s.op)
            {
                case EffectOp.Damage:
                    result.Add(new DamageEffect { damage = s.value });
                    break;

                case EffectOp.Heal:
                    result.Add(new HealEffect { amount = s.value, percent = s.percent, rate = s.rate });
                    break;

                case EffectOp.AbilityMod:
                    result.Add(new AbilityBuff { ability = s.stat, value = s.value, duration = s.duration });
                    break;

                case EffectOp.ApplyDebuff:
                    result.Add(BuildDebuff(s));
                    break;

                case EffectOp.Cleanse:
                    {
                        var cleanse = new CleanseDebuffEffect();

                        if (s.stat == BuffType.Burn || s.stat == BuffType.Poison || s.stat == BuffType.Bleeding || s.stat == BuffType.Faint || s.stat == BuffType.Taunt)
                        {
                            cleanse.removeTypes = new BuffType[] { s.stat };
                        }
                        else
                        {
                            // 전체 정화
                            cleanse.removeTypes = new BuffType[] { BuffType.Burn, BuffType.Poison, BuffType.Bleeding, BuffType.Faint, BuffType.Taunt };
                        }

                        result.Add(cleanse);
                        break;
                    }

                case EffectOp.Special:
                    result.Add(BuildSpecial(s));
                    break;
            }
        }
        return result;
    }

    // 역할: 디버프 타입 매핑해서 DebuffEffect 생성
    static SkillEffect BuildDebuff(ItemEffectSpec s)
    {
        DebuffEffect e = s.stat switch
        {
            BuffType.Poison   => new PoisonEffect(),
            BuffType.Bleeding => new BleedingEffect(),
            BuffType.Burn     => new BurnEffect(),
            BuffType.Sign     => new SignEffect(),
            BuffType.Faint    => new FaintEffect(),
            BuffType.Taunt    => new TauntEffect(),
            _                 => new PoisonEffect()
        };
        e.duration = s.duration;
        e.probability = s.probability;
        return e;
    }

    // 역할: 특수 효과(면역/턴 훅 등)를 콜백형으로 구성
    static SkillEffect BuildSpecial(ItemEffectSpec s)
    {
        return new SpecialSkillEffect
        {
            // NOTE: 현재 SpecialSkillEffect는 Action<Job,Job> onApply만 지원하므로
            // Combatant 훅이 필요하면 이후 EquipmentRuntime 단계에서 확장
            onApply = (user, target) =>
            {
                if (target == null) return;
                // 예시: 면역 부여
                if (s.specialKey == "Immune_Burn")    target.AddBuff(BuffType.Burn, 0);   // 표식용(표시/로직은 Combatant 측 구현에 맞게)
                if (s.specialKey == "Immune_Faint")   target.AddBuff(BuffType.Faint, 0);
                // 필요 시 특수 키워드 확장
            }
        };
    }
}
