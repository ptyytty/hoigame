
//직업의 속성 정보
using System;
using System.Collections.Generic;

[System.Serializable]
public class Job
{
    public int id_job;
    public string name_job;
    public int level;
    public int exp;
    public int hp;
    public int def;
    public int res;
    public int spd;
    public int hit;
    public int loc;
    public int category;
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

    // 성장 확인
    public Dictionary<int, int> skillLevels = new();        // ★ key = heroId * BASE(100) + localSkillId


    /// <summary>
    /// 버프 추가
    /// </summary>
    /// <param name="type"> BuffType </param>
    /// <param name="duration"> Int </param>
    public void AddBuff(BuffType type, int duration)
    {
        if (duration <= 0) duration = 1;
        if (activeBuffs.TryGetValue(type, out var cur))
            activeBuffs[type] = Math.Max(cur, duration);
        else
            activeBuffs[type] = duration;
    }

    // ✅ 디버프 추가 (새로 추가)
    public void AddDebuff(BuffType type, int duration)
    {
        if (duration <= 0) duration = 1;
        if (activeDebuffs.TryGetValue(type, out var cur))
            activeDebuffs[type] = Math.Max(cur, duration);
        else
            activeDebuffs[type] = duration;
    }

    // ✅ 조회 (Combatant.HasBuff에서 호출)
    public bool HasBuff(BuffType type)
    {
        return activeBuffs.TryGetValue(type, out var remain) && remain > 0;
    }

    // (선택) 디버프 조회도 있으면 편함
    public bool HasDebuff(BuffType type)
    {
        return activeDebuffs.TryGetValue(type, out var remain) && remain > 0;
    }

    // (선택) 턴 종료 시 지속턴 감소용 - 필요할 때 호출
    public void TickStatuses()
    {
        TickDict(activeBuffs);
        TickDict(activeDebuffs);

        static void TickDict(Dictionary<BuffType,int> dict)
        {
            if (dict.Count == 0) return;
            // 키 목록을 복사해서 안전하게 순회
            var keys = new List<BuffType>(dict.Keys);
            foreach (var k in keys)
            {
                dict[k] = dict[k] - 1;
                if (dict[k] <= 0) dict.Remove(k);
            }
        }
    }
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