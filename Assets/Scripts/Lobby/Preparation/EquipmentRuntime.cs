using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [런타임 전용] 파티 스폰 시 장비 효과를 Combatant에 반영하는 유틸리티
/// - 프리뷰(EquipmentPreviewPatcher)는 로비 표기용, 여기는 전투 실제 적용용
/// </summary>
public static class EquipmentRuntime
{
    /// <summary>
    /// 역할: 영웅이 장착한 장비의 persistent 효과를 Combatant에 한 번 적용
    /// - 스폰 직후 1회 호출 권장 (씬 언로드 시 자동 초기화 가정)
    /// </summary>
    public static void ApplyOnSpawn(Job hero, Combatant target)
    {
        if (hero == null || target == null) return;
        var equip = hero.equippedItem;
        if (equip == null || equip.effects == null) return;

        foreach (var e in equip.effects)
        {
            if (!e.persistent) continue; // 역할: 장비 지속 효과만 런타임 적용

            switch (e.op)
            {
                case EffectOp.AbilityMod:
                    // 역할: 능력치 증감 → Combatant 런타임 능력치에 가산
                    ApplyAbilityMod(target, e.stat, e.value);
                    break;

                case EffectOp.ApplyDebuff:
                    // 역할: 장비가 상시 부여하는 디버프가 있다면(희귀 케이스) 스폰 시 즉시 적용
                    ApplyDebuffOnSpawn(target, e);
                    break;

                case EffectOp.Cleanse:
                    // 역할: 상시 정화 타입은 스폰 시 1회 정화(게임 디자인에 맞게 조정)
                    Cleanse(target, e);
                    break;

                case EffectOp.Special:
                    // 역할: 특수키워드(면역 등) 부여. Combatant 단의 면역/태그 시스템으로 연결
                    ApplySpecial(target, e);
                    break;

                // Damage/Heal 은 ‘즉시형’이라 지속효과 취지와 다름 → 일반적으로 무시
            }
        }
    }

    /// <summary>역할: Combatant 런타임 능력치에 AbilityMod 반영</summary>
    static void ApplyAbilityMod(Combatant target, BuffType stat, int delta)
    {
        // ⚠️ 프로젝트의 Combatant/Status 구조에 맞게 맵핑하세요.
        // 예시: target.status.def/res/spd/hit 같은 필드나, AdditiveModifier 시스템이 있다면 그 API 호출로 대체.
        switch (stat)
        {
            case BuffType.Defense:    target.Status.def += delta; break;
            case BuffType.Resistance: target.Status.res += delta; break;
            case BuffType.Speed:      target.Status.spd += delta; break;
            case BuffType.Hit:        target.Status.hit += delta; break;
            case BuffType.Damage:
                // 예: 공격력 가산용 필드/모듈이 있다면 여기서 연결
                target.Status.damageBonus += delta;
                break;
            case BuffType.Heal:
                target.Status.healBonus += delta;
                break;
            // 필요 시 MaxHP/CRT 등 프로젝트 확장 스탯을 이어서 매핑
        }
    }

    /// <summary>역할: 스폰 시점 1회성 정화</summary>
    static void Cleanse(Combatant target, ItemEffectSpec s)
    {
        if (s.stat == BuffType.Burn || s.stat == BuffType.Poison || s.stat == BuffType.Bleeding || s.stat == BuffType.Faint || s.stat == BuffType.Taunt)
            target.RemoveDebuffs(new []{ s.stat });
        else
            target.RemoveAllDebuffs();
    }

    /// <summary>역할: 스폰 시 즉시 디버프를 걸어야 하는 특수 설계(선택)</summary>
    static void ApplyDebuffOnSpawn(Combatant target, ItemEffectSpec s)
    {
        // 예: 확률은 스폰시 확정 적용으로 해석. 디자인상 불필요하면 삭제 가능.
        target.AddDebuff(s.stat, s.duration);
    }

    /// <summary>역할: 특수키워드 처리(면역/태그)</summary>
    static void ApplySpecial(Combatant target, ItemEffectSpec s)
    {
        // 예시: 면역 태그 부여
        if (s.specialKey == "Immune_Burn")  target.AddImmunity(BuffType.Burn);
        if (s.specialKey == "Immune_Faint") target.AddImmunity(BuffType.Faint);
        // TODO: 프로젝트에 맞는 Special 키워드 확장
    }
}