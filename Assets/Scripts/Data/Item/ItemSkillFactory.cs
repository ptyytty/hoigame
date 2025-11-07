using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 역할: ConsumeItem을 BattleManager가 이해하는 임시 Skill로 변환
/// - target/area는 아이템 데이터 그대로
/// - effects는 ItemEffectSpec을 적절한 SkillEffect로 래핑
/// </summary>
public static class ItemSkillFactory
{
    private static int _seq = 900000; // 임시 스킬 id 대역(충돌 방지용)

    /// <summary>
    /// 역할: 아이템을 임시 스킬로 만든다(아군/적/자신 대상 공통)
    /// - correctionHit=0(명중 보정 없음)으로 두고, 필요 시 아이템 특성으로 후처리 가능
    /// </summary>
    public static Skill BuildSkillFromItem(ConsumeItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemSkillFactory] item is null");
            return null;
        }

        var skill = new Skill
        {
            skillId    = _seq++,
            skillName  = $"[ITEM] {item.name_item}",
            target     = MapTarget(item.itemTarget), // Target: Enemy/Ally/Self
            loc        = Loc.None,                   // 사용 위치 제한 없음
            targetLoc  = Loc.None,                   // 타겟 위치 제한 없음
            area       = item.area,                  // Area: Single/Row/Entire
            type       = SkillType.Special,          // 아이템은 스킬 타입 의미가 약하므로 Special로 태깅
            correctionHit = 0,                       // 명중 보정은 0(필요 시 조절)
            effects    = BuildEffectsFromItem(item)  // ItemEffectSpec → SkillEffect[]
        };

        return skill;
    }

    /// <summary>
    /// 역할: ItemTarget → Skill.Target 매핑
    /// </summary>
    private static Target MapTarget(ItemTarget t)
    {
        // Skill.Target 정의: Enemy=0, Ally=1, Self=2
        // (SkillData.cs의 Target/Area/Loc 정의와 일치)
        switch (t)
        {
            case ItemTarget.Enemy: return Target.Enemy;
            case ItemTarget.Ally:  return Target.Ally;
            default:               return Target.Enemy;
        }
    }

    /// <summary>
    /// 역할: ItemEffectSpec 리스트를 프로젝트 표준 SkillEffect 리스트로 변환
    /// - 변환 로직은 ItemEffectBridge에 집약(필드명/클래스 일치)
    /// </summary>
    private static List<SkillEffect> BuildEffectsFromItem(ConsumeItem item)
    {
        // ItemEffectBridge.BuildEffects:
        // Damage → DamageEffect.damage
        // Heal   → HealEffect.amount/percent/rate
        // AbilityMod → AbilityBuff.ability/value/duration
        // ApplyDebuff → {PoisonEffect, BleedingEffect, BurnEffect, SignEffect, FaintEffect, TauntEffect} + duration/probability
        // Cleanse → CleanseDebuffEffect(removeTypes 설정)
        // Special → SpecialSkillEffect(onApply 콜백)
        return ItemEffectBridge.BuildEffects(item.effects);
    }
}
