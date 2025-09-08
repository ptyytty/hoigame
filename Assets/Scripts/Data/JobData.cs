
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

    // 버프 적용
    public void AddBuff(BuffType type, int duration)
    {
        if (activeDebuffs.ContainsKey(type))
        {
            activeDebuffs[type] += duration;
        }
        else
        {
            activeDebuffs.Add(type, duration);
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