/// <summary>
/// 역할: 파티 편성/로비 화면에서 '장비의 AbilityMod'만 읽어 Job의 표기 스탯을 증감
/// - 전투 런타임의 onApply/onRemove는 이후 EquipmentRuntime에서 처리(다음 단계)
/// </summary>
public static class EquipmentPreviewPatcher
{
    /// <summary>역할: EquipItem의 AbilityMod 효과를 Job에 더함(표기용)</summary>
    public static void ApplyToJob(Job job, EquipItem equip)
    {
        if (job == null || equip == null || equip.effects == null) return;
        foreach (var e in equip.effects)
        {
            if (e.op != EffectOp.AbilityMod) continue;
            Add(job, e.stat, e.value);
        }
    }

    /// <summary>역할: EquipItem의 AbilityMod 효과를 Job에서 뺌(표기용)</summary>
    public static void RemoveFromJob(Job job, EquipItem equip)
    {
        if (job == null || equip == null || equip.effects == null) return;
        foreach (var e in equip.effects)
        {
            if (e.op != EffectOp.AbilityMod) continue;
            Add(job, e.stat, -e.value);
        }
    }

    // 역할: BuffType 별 수치 증감 → Job 필드에 반영
    static void Add(Job job, BuffType stat, int delta)
    {
        switch (stat)
        {
            case BuffType.Defense:    job.def += delta; break;
            case BuffType.Resistance: job.res += delta; break;
            case BuffType.Speed:      job.spd += delta; break;
            case BuffType.Hit:        job.hit += delta; break;
            case BuffType.Damage:     /* 공격력 표기 필드가 따로 있으면 거기에 반영 */ break;
            // MaxHP 증가 등이 필요하면 BuffType 확장 후 여기에 추가
        }
    }
}