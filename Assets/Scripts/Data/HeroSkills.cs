using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeroSkills
{
    public static List<HeroSkill> holyKnightSkills = new List<HeroSkill>
    {
        new HeroSkill{
            skillId = 1,
            skillName = "방패 타격",
            damage = 10,
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 0,
            effect = SkillType.Damage
        },
        new HeroSkill{
            skillId = 2,
            skillName = "아군 보호",
            damage = 0,
            target = Target.Ally,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 0,
            effect = SkillType.Buff,
            debuff = new Debuff{
                debuffType = SkillDebuffType.Taunt,
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
            heroId = 0,
            effect = SkillType.Buff
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

    public static List<HeroSkill> woodcutterSkills = new List<HeroSkill>
    {
        new HeroSkill{
            skillId = 1,
            skillName = "도끼질",
            damage = 22,
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 2
        },
        new HeroSkill{
            skillId = 2,
            skillName = "한 점 집중",
            damage = 0,
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 2
        },
        new HeroSkill{
            skillId = 3,
            skillName = "견디기",
            damage = 0,
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 2
        }
    };

    public static List<HeroSkill> boxerSkills = new List<HeroSkill>
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

    // Range
}

