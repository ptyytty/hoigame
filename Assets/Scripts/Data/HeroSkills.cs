using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroSkills
{
    public List<Skill> holyKnightSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "방패 타격",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 10},
                new FaintEffect {duration = 1, probability = 0.15f}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "아군 보호",
            target = Target.Ally,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            type = SkillType.Buff,
            // debuff = new List<Debuff>{
            //     new Debuff{
            //         debuffType = SkillDebuffType.Taunt,
            //         duration = 2,
            //     }
            // }
            effects = new List<SkillEffect>{

            }
        },
        new Skill{
            skillId = 3,
            skillName = "저항",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Defense,
            //         duration = 2,
            //         figure = 10
            //     },
            //     new Buff{
            //         buffType = SkillBuffType.Resistance,
            //         duration = 2,
            //         figure = 5
            //     }
            // }
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 10},
                new FaintEffect {duration = 1, probability = 0.15f}
            }
        }
    };

    public List<Skill> warriorSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "용약일자세",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 2,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 18},
                new BleedingEffect {duration = 2, probability = 0.2f}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "시우상전세",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Row,
            heroId = 2,
            type = SkillType.Damage,
            correctionHit = -15,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 10},
                new FaintEffect {duration = 2, probability = 0.8f}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "은림세",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 2,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 2,
            //         figure = 2
            //     },
            //     new Buff{
            //         buffType = SkillBuffType.Hit,
            //         duration = 2,
            //         figure = 5
            //     }
            // }
            effects = new List<SkillEffect>{

            }
        }
    };

    public List<Skill> woodcutterSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "도끼질",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 3,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 22}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "한 점 집중",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 3,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Hit,
            //         duration = 1,
            //         figure = 15
            //     },
            //     new Buff{
            //         buffType = SkillBuffType.Defense,
            //         duration = 2,
            //         figure = -6
            //     }
            // }
            effects = new List<SkillEffect>{

            }
        },
        new Skill{
            skillId = 3,
            skillName = "견디기",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 3,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Defense,
            //         duration = 2,
            //         figure = 5
            //     }
            // }
            effects = new List<SkillEffect>{
            }
        }
    };

    public List<Skill> boxerSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "스트레이트",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 4,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 12},
            }
        },
        new Skill{
            skillId = 2,
            skillName = "어퍼컷",
            target = Target.Enemy,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 4,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 6},
                new FaintEffect {duration = 1, probability = 0.7f}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "카운터",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 4,
            type = SkillType.Special,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 2,
            //         figure = 3
            //     }
            // }
            effects = new List<SkillEffect>{
            }
        }
    };

    //------------ Range-------------

    public List<Skill> archerSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "사격",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 5,
            type = SkillType.SignDamage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 20},
            }
        },
        new Skill{
            skillId = 2,
            skillName = "백스텝",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 5,
            type = SkillType.Special,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 2,
            //         figure = 2
            //     }
            // }
            effects = new List<SkillEffect>{
            }
        },
        new Skill{
            skillId = 3,
            skillName = "목표 조준",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 5,
            type = SkillType.Debuff,
            effects = new List<SkillEffect>{
                new SignEffect {duration = 2}
            }
        }
    };

    public List<Skill> hunterSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "저격",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 6,
            type = SkillType.SignDamage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 24},
            }
        },
        new Skill{
            skillId = 2,
            skillName = "산탄",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 6,
            type = SkillType.Damage,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 1,
            //         figure = -2
            //     }
            // }
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 10},
            }
        },
        new Skill{
            skillId = 3,
            skillName = "곰 덫",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 6,
            type = SkillType.Special
        }
    };

    //-------------------Special----------------
    public List<Skill> ninzaSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "목표 암살",
            target = Target.Enemy,
            loc = Loc.None,
            area = Area.Single,
            heroId = 7,
            type = SkillType.SignDamage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 12},
                new BleedingEffect {duration = 3, probability = 0.7f}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "인술",
            target = Target.Self,
            loc = Loc.None,
            area = Area.Single,
            heroId = 7,
            type = SkillType.Special,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 1,
            //         figure = 5
            //     }
            // }
            effects = new List<SkillEffect>{
                new SignEffect { duration = 1}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "쿠나이 투척",
            target = Target.Enemy,
            loc = Loc.None,
            area = Area.Single,
            heroId = 7,
            type = SkillType.SignDamage,
            correctionHit = -15,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 16},
            }
        }
    };

    public List<Skill> GeneralWizardSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "단일 마법",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.SignDamage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 12},
            }
        },
        new Skill{
            skillId = 2,
            skillName = "광역 마법",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 8,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 6},
            }
        },
        new Skill{
            skillId = 3,
            skillName = "지원 마법",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Damage,
            //         duration = 1,
            //         figure = 5
            //     }
            // }
        }
    };
    public List<Skill> FireWizardSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "염화",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 18},
                new BurnEffect {duration = 3}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "화마",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 8,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 8},
                new BurnEffect {duration = 2}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "방화갑",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Buff,
            // buff = new List<Buff>{
            //     new Buff{
            //         // figure 값은 데미지 값으로 사용
            //         duration = 3,
            //         figure = 8
            //     }
            // }
            effects = new List<SkillEffect>{
            }
        }
    };
    public List<Skill> IceWizardSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "용솟음",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Damage,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Heal,
            //         figure = 6
            //     }
            // }
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 16},
                new FaintEffect {duration = 1, probability = 0.15f}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "홍수",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 8,
            type = SkillType.Damage,
            // buff = new List<Buff>{
            //     new Buff{
            //         buffType = SkillBuffType.Speed,
            //         duration = 2,
            //         figure = -3
            //     }
            // }
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 8}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "우수",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
            }
        }
    };
    public List<Skill> ElectricWizardSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "단일 마법",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.SignDamage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 12},
                new FaintEffect {duration = 1, probability = 0.15f}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "광역 마법",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 8,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
            }
        },
        new Skill{
            skillId = 3,
            skillName = "지원 마법",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 8,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
            }
        }
    };
}

