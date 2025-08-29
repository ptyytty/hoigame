using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill
{
    public int skillId;
    public string skillName;
    public Target target;       //스킬 대상 0: 적     1: 아군     2: 자신
    public Loc loc;             //사용 위치 0: 상관없음   1: 전열     2: 후열
    public Area area;           //사용 범위 0: 단일     1: 같은 열     3: 전체
    public int heroId;
    public int monsterId;
    public SkillType type;
    public int correctionHit;               // 명중 보정
    public List<SkillEffect> effects = new();
}

[System.Serializable]
public class SkillList
{
    public Skill[] skills;
}

// 스킬 타입
public enum SkillType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    SignDamage,
    Special
}

public enum BuffType
{
    //-----Buff-----
    Defense,
    Resistance,
    Speed,
    Hit,
    Damage,
    Heal,
    Remove,

    //-----Debuff----

    Poison,     // 중독
    Bleeding,   // 출혈
    Burn,       // 화상
    Sign,       // 표식
    Faint,      // 기절
    Taunt       // 도발
}

public static class BuffGroups
{
    public static readonly HashSet<BuffType> StatBuffs = new()
    { BuffType.Defense, BuffType.Resistance, BuffType.Speed, BuffType.Hit, BuffType.Damage, BuffType.Heal };

    public static readonly HashSet<BuffType> DotDebuffs = new()
    { BuffType.Poison, BuffType.Bleeding, BuffType.Burn };

    public static readonly HashSet<BuffType> CrowdControls = new()
    { BuffType.Faint, BuffType.Taunt };

    public static bool IsDebuff(BuffType t) =>
        DotDebuffs.Contains(t) || CrowdControls.Contains(t) || t == BuffType.Sign;
}

[System.Serializable]
public class Buff
{
    public BuffType buffType;      // 버프, 디버프
    public int duration;                // 지속 턴 수
    public float probability;            // 적용 확률
    public int figure;                  // 증감 수치

    public StackMode stack = StackMode.Refresh;

}

public enum StackMode { Refresh, StackValue, Ignore }       // 버프 및 효과 추가 관련 변수

public enum Target
{
    Enemy = 0,
    Ally = 1,
    Self = 2
}

public enum Loc{
    None = 0,
    Front = 1,
    Back = 2
}

public enum Area{
    Single = 0,
    Row = 1,
    Entire = 2
}