using System.Collections;
using System.Collections.Generic;
using System.Data;
using Save;
using UnityEngine;

public static class ItemDatabase
{
    public static readonly List<ConsumeItem> consumeItems = new List<ConsumeItem>
    {
        //-----------------소비 아이템----------------------
        new ConsumeItem{
            id_item = 1,
            name_item = "돈까스 김밥",
            price = 500,
            iconName = "kimbab",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "식당에서 파는 김밥이다. 싸고 맛있다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Heal, value = 10, percent = false}
            }
        },

        new ConsumeItem{
            id_item = 2,
            name_item = "베이컨",
            price = 800,
            iconName = "Bacon",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "짭짤한 베이컨. 하나씩 꺼내 먹기 좋다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Heal, value = 15, percent = false}
            }
        },

        new ConsumeItem{
            id_item = 3,
            name_item = "햄 덩어리",
            price = 1100,
            iconName = "Ham",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "커다란 햄 덩어리다. 무겁고 양도 많다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Heal, value = 20, percent = false}
            }
        },

        new ConsumeItem{
            id_item = 4,
            name_item = "조잡한 폭탄",
            price = 700,
            iconName = "bomb",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Enemy,
            area = Area.Single,
            description = "직접 만든 폭탄이다. 위험하니 조심하자.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Damage, value = 15 }
            }
        },

        new ConsumeItem{
            id_item = 5,
            name_item = "투척용 소화기",
            price = 550,
            iconName = "fire",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "던질 수 있는 소화기. 작은 불을 바로 끌 수 있다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Cleanse, stat = BuffType.Burn}
            }
        },

        new ConsumeItem{
            id_item = 6,
            name_item = "해독제",
            price = 650,
            iconName = "Detoxicant",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "끔찍한 맛이다. 살려면 먹어야 한다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Cleanse, stat = BuffType.Poison }
            }
        },

        new ConsumeItem{
            id_item = 7,
            name_item = "발화꽃",
            price = 1100,
            iconName = "Fire Flower",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "잎이 타오르는 꽃. 줄기는 태우지 않는다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.ApplyDebuff, stat = BuffType.Burn, duration = 3, probability = 1f}
            }
        },

        new ConsumeItem{
            id_item = 8,
            name_item = "방화포",
            price = 1050,
            iconName = "fire",
            itemType = ItemType.Consume,
            itemTarget = ItemTarget.Ally,
            area = Area.Single,
            description = "불을 막아주는 천이다. 보온 효과는 없다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{ op = EffectOp.Special, stat = BuffType.Burn, duration = 5, specialKey = "Immune_Burn"}
            }
        }
    };

    public static readonly List<EquipItem> equipItems = new List<EquipItem>
    {
        //-----------------장비 아이템----------------------
        new EquipItem{
            id_item = 101,
            name_item = "신속 신발",
            price = 2100,
            iconName = "Quick Shoes",
            jobCategory = JobCategory.Ranged,
            itemType = ItemType.Equipment,
            effectText = "민첩",
            description = "착용감 좋은 신발.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Speed, value = 2, persistent = true}
            }
        },

        new EquipItem{
            id_item = 102,
            name_item = "가죽 장갑",
            price = 2550,
            iconName = "Leather Gloves",
            jobCategory = JobCategory.Ranged,
            itemType = ItemType.Equipment,
            effectText = "명중",
            description = "가죽으로 만든 장갑이다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Hit, value = 10, persistent = true}
            }
        },

        new EquipItem{
            id_item = 103,
            name_item = "건틀릿",
            price = 1800,
            iconName = "Plate Gloves",
            jobCategory = JobCategory.Warrior,
            itemType = ItemType.Equipment,
            effectText = "방어",
            description = "튼튼한 건틀릿. 때리기에 좋지만 막기엔 더 좋다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Defense, value = 5, persistent = true}
            }
        },

        new EquipItem{
            id_item = 104,
            name_item = "마법사 스테프",
            price = 2600,
            iconName = "Mage Staff",
            jobCategory = JobCategory.Special,
            itemType = ItemType.Equipment,
            effectText = "스킬 피해",
            description = "마법사의 스테프. 휘두르기 좋다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Damage, value = 5, persistent = true}
            }
        },

        new EquipItem{
            id_item = 105,
            name_item = "요술봉",
            price = 2300,
            iconName = "Priest Staff",
            jobCategory = JobCategory.Healer,
            itemType = ItemType.Equipment,
            effectText = "회복량",
            description = "장난감 요술봉. 신비한 힘이 깃들어져 있다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Heal, value = 5, persistent = true}
            }
            
        },

        new EquipItem{
            id_item = 106,
            name_item = "망토",
            price = 1900,
            iconName = "Cloak",
            jobCategory = JobCategory.Ranged,
            itemType = ItemType.Equipment,
            effectText = "민첩",
            description = "어깨에 걸쳐지는 망토. 오버핏이다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.AbilityMod, stat = BuffType.Speed, value = 3, persistent = true}
            }
        },

        new EquipItem{
            id_item = 107,
            name_item = "투구",
            price = 2200,
            iconName = "Helmet",
            jobCategory = JobCategory.Warrior,
            itemType = ItemType.Equipment,
            description = "무거운 투구. 쓰고 다니기 힘들 정도다.",
            effects = new List<ItemEffectSpec>
            {
                new ItemEffectSpec{op = EffectOp.Special, persistent = true, specialKey = "Immune_Faint"}
            }
        }
    };
}
