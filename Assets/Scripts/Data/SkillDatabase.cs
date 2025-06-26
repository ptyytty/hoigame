using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HeroSkill
{
    public int skillId;
    public string skillName;
    public int damage;
    public Target target;
    public Loc loc;
    public Area area;
    public int heroId;
    public int monsterId;
    public EffectType effect;
    public Debuff debuff;
}