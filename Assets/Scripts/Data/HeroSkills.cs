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
            heroId = 0,
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
            heroId = 0,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new TauntEffect{duration = 2}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "저항",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 0,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 2, ability = BuffType.Defense, value = 10},
                new AbilityBuff{duration = 2, ability = BuffType.Resistance, value = 5}
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
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 2, value = 2, ability = BuffType.Speed},
                new AbilityBuff{duration = 2, value = 5, ability = BuffType.Hit}
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
            heroId = 1,
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
            heroId = 1,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 1, value = 15, ability = BuffType.Hit},
                new AbilityBuff{duration = 2, value = -6, ability = BuffType.Defense}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "견디기",
            target = Target.Self,
            loc = Loc.Front,
            area = Area.Single,
            heroId = 1,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 2, value = 5, ability = BuffType.Defense}
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
            heroId = 3,
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
            heroId = 3,
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
            heroId = 3,
            type = SkillType.Special,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 2, value = 3, ability = BuffType.Speed},
                new SpecialSkillEffect{
                    onApply = (user, target) =>{
                        user.isCountering = true;
                    }
                }
            }
        }
    };

    //------------ Ranger-------------

    public List<Skill> archerSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "사격",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 4,
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
            heroId = 4,
            type = SkillType.Special,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 2, value = 2, ability = BuffType.Speed}
                // 후열 이동 코드
            }
        },
        new Skill{
            skillId = 3,
            skillName = "목표 조준",
            target = Target.Enemy,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 4,
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
            heroId = 5,
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
            heroId = 5,
            type = SkillType.Damage,
            effects = new List<SkillEffect>{
                new DamageEffect {damage = 10},
                new AbilityBuff{duration = 1, value = -2, ability = BuffType.Speed}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "곰 덫",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 5,
            type = SkillType.Special
        }
    };

    //-------------------Special----------------
    // public List<Skill> ninzaSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "목표 암살",
    //         target = Target.Enemy,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 6,
    //         type = SkillType.SignDamage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 12},
    //             new BleedingEffect {duration = 3, probability = 0.7f}
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "인술",
    //         target = Target.Self,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 6,
    //         type = SkillType.Special,
    //         effects = new List<SkillEffect>{
    //             new SignEffect { duration = 1},
    //             new AbilityBuff {duration = 1, value = 5, ability = BuffType.Speed},
    //             new SpecialSkillEffect {
    //                 duration = 2,
    //                 onApply = (user, target) =>{
    //                     // 아군 위치 변환, 피해량 조정
    //                 }
    //             }
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "쿠나이 투척",
    //         target = Target.Enemy,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 6,
    //         type = SkillType.SignDamage,
    //         correctionHit = -15,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 16},
    //         }
    //     }
    // };

    // public List<Skill> GeneralWizardSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "단일 마법",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.SignDamage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 12},
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "광역 마법",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Entire,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 6},
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "지원 마법",
    //         target = Target.Ally,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new AbilityBuff{value = 5, ability = BuffType.Damage}
    //         }
    //     }
    // };
    // public List<Skill> FireWizardSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "염화",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 18},
    //             new BurnEffect {duration = 3}
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "화마",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Entire,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 8},
    //             new BurnEffect {duration = 2}
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "방화갑",
    //         target = Target.Ally,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new SpecialSkillEffect{
    //                 duration = 3,
    //                 onApply = (user, target) =>{
    //                     // 아군 공격 시 적 피해
    //                 }
    //             }
    //         }
    //     }
    // };

    // public List<Skill> IceWizardSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "용솟음",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 16},
    //             new AbilityBuff{value = 6, ability = BuffType.Heal}
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "홍수",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Entire,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 8},
    //             new AbilityBuff {duration = 2, value = -3, ability = BuffType.Speed}
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "우수",
    //         target = Target.Ally,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new AbilityBuff {value = 12, ability = BuffType.Heal},
    //             new AbilityBuff {ability = BuffType.Remove}
    //         }
    //     }
    // };

    // public List<Skill> ElectricWizardSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "뇌격",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.SignDamage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 14},
    //             new FaintEffect {duration = 1, probability = 0.75f}
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "폭풍뢰",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Entire,
    //         heroId = 7,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect {damage = 8},
    //             new AbilityBuff {duration = 1, value = -10, ability = BuffType.Hit}
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "발전",
    //         target = Target.Ally,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 7,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new AbilityBuff {duration = 3, value = 2, ability = BuffType.Speed}
    //         }
    //     }
    // };

    // public List<Skill> ShamanSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "강신",
    //         target = Target.Self,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 8,
    //         type = SkillType.Special,
    //         effects = new List<SkillEffect>{
    //             new AbilityBuff {duration = 1, value = 37, ability = BuffType.Defense},
    //             new SpecialSkillEffect {
    //                 onApply = (user, target) =>{
    //                     // 캐릭터 이동, 피해량 증가
    //                 }
    //             }
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "기원",
    //         target = Target.Ally,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 8,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new SpecialSkillEffect{
    //                 onApply = (user, target) =>{
    //                     switch (UnityEngine.Random.value){
                            
    //                     }
    //                 }
    //             }
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "저주",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 8,
    //         type = SkillType.Debuff,
    //         effects = new List<SkillEffect>{
    //             new SignEffect{ duration = 2},
    //             new AbilityBuff { duration = 2, value = -3, ability = BuffType.Speed}
    //         }
    //     }
    // };

    // public List<Skill> BomberSkills = new List<Skill>
    // {
    //     new Skill{
    //         skillId = 1,
    //         skillName = "폭탄이다!!!",
    //         target = Target.Enemy,
    //         loc = Loc.Back,
    //         area = Area.Row,
    //         heroId = 9,
    //         type = SkillType.Damage,
    //         effects = new List<SkillEffect>{
    //             new DamageEffect { damage = 18},
    //             new BurnEffect{duration = 2, value = 4},
    //             new SpecialSkillEffect{
    //                 onApply = (user, target) =>{
    //                     // 폭탄 폭발 데미지
    //                 }
    //             }
    //         }
    //     },
    //     new Skill{
    //         skillId = 2,
    //         skillName = "지뢰 파묻기",
    //         target = Target.Ally,
    //         loc = Loc.Back,
    //         area = Area.Single,
    //         heroId = 9,
    //         type = SkillType.Special,
    //         effects = new List<SkillEffect>{
    //             new BurnEffect{duration = 2, probability = 0.6f, value = 3},
    //             new SpecialSkillEffect{
    //                 onApply = (user, target) =>{
    //                     switch (UnityEngine.Random.value){
    //                         // 지뢰 폭발 메커니즘
    //                     }
    //                 }
    //             }
    //         }
    //     },
    //     new Skill{
    //         skillId = 3,
    //         skillName = "화약 추가",
    //         target = Target.Self,
    //         loc = Loc.None,
    //         area = Area.Single,
    //         heroId = 9,
    //         type = SkillType.Buff,
    //         effects = new List<SkillEffect>{
    //             new AbilityBuff {value = 10, ability = BuffType.Hit},
    //             new SpecialSkillEffect{
    //                 onApply = (user, target) =>{
    //                     // 폭탄 폭발 지연, 연속 사용 불가,
    //                 }
    //             }
    //         }
    //     }
    // };

    //============ Healer ===========
    public List<Skill> ClericSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "회복",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 6,
            type = SkillType.Heal,
            effects = new List<SkillEffect>{
                new AbilityBuff{ value = 10, ability = BuffType.Heal}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "대규모 치유",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Entire,
            heroId = 6,
            type = SkillType.Heal,
            effects = new List<SkillEffect>{
                new AbilityBuff{value = 6, ability = BuffType.Heal}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "기도",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 6,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{ability = BuffType.Remove}
            }
        }
    };

    public List<Skill> DoctorSkills = new List<Skill>
    {
        new Skill{
            skillId = 1,
            skillName = "긴급 수술",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 7,
            type = SkillType.Heal,
            effects = new List<SkillEffect>{
                new AbilityBuff{value = 16, ability = BuffType.Heal},
                new BleedingEffect{duration = 3}
            }
        },
        new Skill{
            skillId = 2,
            skillName = "투약",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 7,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{duration = 3, value = 1, ability = BuffType.Speed},
                new AbilityBuff{duration = 2, value = 5, ability = BuffType.Hit}
            }
        },
        new Skill{
            skillId = 3,
            skillName = "처방",
            target = Target.Ally,
            loc = Loc.Back,
            area = Area.Single,
            heroId = 7,
            type = SkillType.Buff,
            effects = new List<SkillEffect>{
                new AbilityBuff{ability = BuffType.Remove}
            }
        }
    };

    // 영웅 기본 스킬 외부 호출
    public IEnumerable<Skill> GetHeroSkills(Job job)
    {
        switch (job.id_job)
        {
            case 0: return holyKnightSkills;
            case 1: return woodcutterSkills;
            case 2: return warriorSkills;
            case 3: return boxerSkills;
            case 4: return archerSkills;
            case 5: return hunterSkills;
            // case 6: return ninzaSkills;
            // case 7: return GeneralWizardSkills;
            // case 8: return ShamanSkills;
            // case 9: return BomberSkills;
            case 6: return ClericSkills;
            case 7: return DoctorSkills;
            default: return System.Array.Empty<Skill>();
        }
    }
};
    

