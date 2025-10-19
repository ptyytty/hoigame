using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MonsterSkill
{
    private static readonly Dictionary<int, List<Skill>> monsterSkillDB = new()
    {
        // Mouse
        [1001] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "단일 공격",
                target = Target.Enemy,     // 시전자 기준: 적(=영웅 진영)
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1001,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 12 }
                }
            }
        },

        // Keyboard
        [1002] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "도발",
                target = Target.Self,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1002,
                type = SkillType.Buff,
                effects = new List<SkillEffect> {
                    new TauntEffect {duration = 1}
                }
            },
            new Skill {
                skillId = 2,
                skillName = "단일 공격",
                target = Target.Enemy,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1002,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 10 }
                }
            }
        },

        // Monitor
        [1003] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "광역 공격",
                target = Target.Enemy,
                loc = Loc.Back,
                targetLoc = Loc.None,
                area = Area.Row,
                monsterId = 1003,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 8}
                }
            }
        },

        // Computer
        [1004] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "아군 전체 데미지 증가",
                target = Target.Ally,
                loc = Loc.Back,
                targetLoc = Loc.None,
                area = Area.Entire,
                monsterId = 1004,
                type = SkillType.Buff,
                effects = new List<SkillEffect> {
                    new AbilityBuff { value = 5, ability = BuffType.Damage }
                }
            },
            new Skill {
                skillId = 2,
                skillName = "단일 공격",
                target = Target.Enemy,
                loc = Loc.Back,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1004,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 6 }
                }
            }
        },

        // Manequin
        [1005] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "기절",
                target = Target.Enemy,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1005,
                type = SkillType.Debuff,
                effects = new List<SkillEffect> {
                    new FaintEffect {duration = 2}
                }
            },
            new Skill {
                skillId = 2,
                skillName = "단일 공격",
                target = Target.Enemy,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1005,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 18 }
                }
            }
        },

        // Brown Eyeball
        [1006] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "단일 공격",
                target = Target.Enemy,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1006,
                type = SkillType.Damage,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 10 }
                }
            }
        },

        // Green Eyeball
        [1007] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "중독 공격",
                target = Target.Enemy,
                loc = Loc.Front,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1007,
                type = SkillType.Buff,
                effects = new List<SkillEffect> {
                    new DamageEffect { damage = 8 },
                    new PoisonEffect { duration = 3}
                }
            }
        },

        // Red Eyeball
        [1008] = new List<Skill>
        {
            new Skill {
                skillId = 1,
                skillName = "노려보기",
                target = Target.Enemy,
                loc = Loc.Back,
                targetLoc = Loc.None,
                area = Area.Single,
                monsterId = 1008,
                type = SkillType.Debuff,
                effects = new List<SkillEffect> {
                    new SignEffect { duration = 2}
                }
            },
            new Skill {
                skillId = 2,
                skillName = "광역 공격",
                target = Target.Enemy,
                loc = Loc.Back,
                targetLoc = Loc.None,
                area = Area.Row,
                monsterId = 1008,
                type = SkillType.SignDamage,
                effects = new List<SkillEffect> {
                    new SignDamageEffect { damage = 8 }
                }
            }
        },
    };

    public static IReadOnlyList<Skill> GetMonsterSkill(int monsterId)
        => monsterSkillDB.TryGetValue(monsterId, out var list) ? list : empty;

    private static readonly List<Skill> empty = new();
}
