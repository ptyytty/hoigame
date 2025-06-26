using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeroSkills
{
    public static List<HeroSkill> holyKnightSkills = new List<HeroSkill>
    {
        new HeroSkill{
            skillId = 2,
            skillName = "방패 타격",
            damage = 10,
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            effect = EffectType.Damage
        },
        new HeroSkill{
            skillId = 2,
            skillName = "아군 보호",
            damage = 0,
            target = Target.Ally,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            effect = EffectType.Buff,
            debuff = new Debuff{
                debuffType = DebuffType.Taunt,
                duration = 1,
            }
        },
        new HeroSkill{
            skillId = 3,
            skillName = "저항",
            damage = 0,
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            effect = EffectType.Buff
        }
    };

    public static List<HeroSkill> warriorSkills = new List<HeroSkill>
    {
        new HeroSkill{
            skillId = 1,
            skillName = "용약일자세",
            damage = 10,
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1
        },
        new HeroSkill{
            skillId = 2,
            skillName = "시우상전세",
            damage = 0,
            target = Target.Ally,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1
        },
        new HeroSkill{
            skillId = 3,
            skillName = "은림세",
            damage = 0,
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1
        }
    };
}

