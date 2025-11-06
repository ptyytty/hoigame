
//직업의 속성 정보
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Job
{
    public int id_job;
    public Sprite portrait;
    public string name_job;     // 영웅 이름
    public int level = 1;
    public int exp;
    public int maxHp;           // 최대 체력
    public int hp;              // 현재 체력 (전투 후 체력)
    public int def;
    public int res;
    public int spd;
    public int hit;
    public int loc;             // 영웅 파티 위치
    public int category;        // 영웅 카테고리
    public JobCategory jobCategory;
    public int equippedItemId;
    [NonSerialized] public string displayName;      // 변경 가능한 이름
    [NonSerialized] public EquipItem equippedItem;
    [NonSerialized] public bool runtimeEquipApplied;
    [NonSerialized] public string instanceId;

    // 스킬 사용 관련 속성
    [NonSerialized] public bool CanAct = true;
    [NonSerialized] public bool Marked = false;
    [NonSerialized] public bool isCountering = false;
    [NonSerialized] public Job ForcedTarget;
    private Dictionary<BuffType, int> activeBuffs = new();
    private Dictionary<BuffType, int> activeDebuffs = new();
    internal Dictionary<BuffType, int> BuffsDict => activeBuffs;
    internal Dictionary<BuffType, int> DebuffsDict => activeDebuffs;

    // ======== 레벨 시스템 ==========
    // 필요 경험치 테이블
    private static readonly int[] ExpToNext = { 10, 15, 20, 25};
    public const int MaxLevel = 5;

    public void AddExp(int amount)
    {
        // 방어: 시작 레벨 1 보장
        if (level <= 0) level = 1;

        if (amount <= 0 || level >= MaxLevel) return;

        exp += amount;

        // 필요하면 연속 레벨업
        while (level < MaxLevel)
        {
            int need = GetExpToNext();
            if (exp < need) break;

            exp -= need;   // 다음 레벨로 넘어가며 필요치 차감
            level++;

            if (level >= MaxLevel)
            {
                exp = 0;   // Max에서는 exp 고정
                break;
            }
        }
    }

    public int GetRequiredExp()
    {
        return GameBalance.GetRequiredExpForLevel(level);
    }

    // 현재 레벨에서 다음 레벨까지 필요한 경험치량 반환.
    public int GetExpToNext()
    {
        if (level >= MaxLevel) return 0;
        int idx = Mathf.Clamp(level - 1, 0, ExpToNext.Length - 1);
        return ExpToNext[idx];
    }

    // 현재 레벨에서의 경험치 진행률(0~1).
    // Lv.5이면 1을 반환(막대 꽉 참).
    public float GetExpProgress()
    {
        int req = GetRequiredExp();
        if (req <= 0 || req == int.MaxValue) return 1f;
        return Mathf.Clamp01((float)exp / req);
    }

    // 외부에서 레벨/경험치를 임의 세팅할 때도 규칙을 맞춰 정리.
    // level은 최소 1, 최대 MaxLevel
    // MaxLevel이면 exp=0, 아니면 0<=exp<ExpToNext(level)
    public void NormalizeLevelExp()
    {
        level = Mathf.Clamp(level <= 0 ? 1 : level, 1, MaxLevel);

        if (level >= MaxLevel)
        {
            exp = 0;
        }
        else
        {
            int need = GetExpToNext();
            exp = Mathf.Clamp(exp, 0, Mathf.Max(0, need - 1));
        }
    }

    // 성장 확인
    public Dictionary<int, int> skillLevels = new();        // ★ key = heroId * BASE(100) + localSkillId

    // 상태 추가
    public void AddStatus(BuffType type, int duration)
    {
        if (duration <= 0) duration = 1;

        var target = BuffGroups.IsDebuff(type) ? activeDebuffs : activeBuffs;
        if (target.TryGetValue(type, out var cur))
            target[type] = Math.Max(cur, duration);  // 갱신 정책: 더 긴 쪽 우선
        else
            target[type] = duration;
    }

    public bool HasStatus(BuffType type)
    {
        var target = BuffGroups.IsDebuff(type) ? activeDebuffs : activeBuffs;
        return target.TryGetValue(type, out var remain) && remain > 0;
    }

    // 필요 시 유지 (호출부 점진 전환용)
    public void AddBuff(BuffType t, int d) => AddStatus(t, d);
    public void AddDebuff(BuffType t, int d) => AddStatus(t, d);
    public bool HasBuff(BuffType t) => !BuffGroups.IsDebuff(t) && HasStatus(t);
    public bool HasDebuff(BuffType t) => BuffGroups.IsDebuff(t) && HasStatus(t);
    public string GetDisplayName() => string.IsNullOrEmpty(displayName) ? name_job : displayName;

}

[System.Serializable]
public class JobList
{
    public Job[] jobs;
}

public enum JobCategory
{
    Warrior = 0,
    Ranged = 1,
    Special = 2,
    Healer = 3
}

/// <summary>
/// 경험치/레벨 요구치 밸런스 테이블
/// </summary>
public static class GameBalance
{
    private static readonly int[] requiredExpTable = { 10, 15, 20, 25 };

    public static int GetRequiredExpForLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, requiredExpTable.Length - 1);
        return requiredExpTable[idx];
    }
}