
//직업의 속성 정보
using System;
using System.Collections.Generic;

[System.Serializable]
public class Job
{
    public int id_job;
    public string name_job;
    public int hp;
    public int def;
    public int res;
    public int spd;
    public int hit;
    public int loc;
    public int category;
    public List<SpecialBuffType> activeBuffs = new();
    public JobCategory jobCategory;
    [NonSerialized] public EquipItem equippedItem;
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