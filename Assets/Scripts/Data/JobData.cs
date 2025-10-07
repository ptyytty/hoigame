
//직업의 속성 정보
using System;
using System.Collections.Generic;

[System.Serializable]
public class Job
{
    public int id_job;
    public string name_job;     // 영웅 이름
    public int level;
    public int exp;
    public int hp;
    public int def;
    public int res;
    public int spd;
    public int hit;
    public int loc;             // 영웅 파티 위치
    public int category;        // 영웅 카테고리
    public JobCategory jobCategory;
    [NonSerialized] public EquipItem equippedItem;
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