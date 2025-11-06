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
    /// 역할: 던전 입장 직후 1회, 장비의 지속효과(persistent)를 Job 실수치에 가산
    /// </summary>
    public static void ApplyToJobOnEnterDungeon(Job job)
    {
        if (job == null || job.runtimeEquipApplied) return;
        var equip = job.equippedItem;
        if (equip == null || equip.effects == null) { job.runtimeEquipApplied = true; return; }

        foreach (var e in equip.effects)
        {
            if (!e.persistent) continue; // 지속효과만 런타임 가산

            switch (e.op)
            {
                case EffectOp.AbilityMod:
                    ApplyAbilityModToJob(job, e.stat, e.value);   // 역할: Job DEF/RES/SPD/HIT 등에 직접 가산
                    break;

                // 필요시 확장 (설계에 따라 off)
                case EffectOp.Cleanse:   /* 입장 1회 정화 */ break;
                case EffectOp.ApplyDebuff: /* 입장 1회 상태부여(비권장) */ break;
                case EffectOp.Special:   /* 면역/태그 부여 설계 시 */ break;
            }
        }
        job.runtimeEquipApplied = true;
    }

    /// <summary>
    /// 역할: 던전 종료(또는 파티 해산) 시, ApplyToJobOnEnterDungeon으로 가산했던 지속효과를 되돌림
    /// </summary>
    public static void RevertFromJobOnExitDungeon(Job job)
    {
        if (job == null || !job.runtimeEquipApplied) return;
        var equip = job.equippedItem;
        if (equip == null || equip.effects == null) { job.runtimeEquipApplied = false; return; }

        foreach (var e in equip.effects)
        {
            if (!e.persistent) continue;

            switch (e.op)
            {
                case EffectOp.AbilityMod:
                    ApplyAbilityModToJob(job, e.stat, -e.value);  // 역할: 가산분 회수
                    break;
                // Cleanse/ApplyDebuff/Special은 1회성이라 일반적으로 회수 없음
            }
        }
        job.runtimeEquipApplied = false;
    }

    /// <summary>
    /// 역할: AbilityMod를 Job 실수치에 매핑 (프로젝트 스탯명에 맞게 유지)
    /// </summary>
    static void ApplyAbilityModToJob(Job job, BuffType stat, int delta)
    {
        switch (stat)
        {
            case BuffType.Defense:    job.def += delta; break;
            case BuffType.Resistance: job.res += delta; break;
            case BuffType.Speed:      job.spd += delta; break;
            case BuffType.Hit:        job.hit += delta; break;
            case BuffType.Damage:
                // 필요 시 공격력/가중치에 매핑
                break;
            case BuffType.Heal:
                // 필요 시 치유량 보정에 매핑
                break;
            default: break;
        }
    }
}