using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestMoney", menuName = "Game/Create Test Money")]
public class TestMoney : ScriptableObject
{
    public int money;
    public int redSoul;
    public int blueSoul;
    public int purpleSoul;
    public int greenSoul;

    private Dictionary<JobCategory, Func<int>> soulGetter;

    private void OnEnable() => Initialize();

    public void Initialize()
    {
        soulGetter = new Dictionary<JobCategory, Func<int>>()
        {
            { JobCategory.Warrior, () => redSoul },
            { JobCategory.Ranged, () => blueSoul },
            { JobCategory.Special, () => purpleSoul },
            { JobCategory.Healer, () => greenSoul }
        };
    }

    public int GetSoul(JobCategory category)
    {
        if (soulGetter == null) Initialize();
        return soulGetter.TryGetValue(category, out var getter) ? getter() : 0;
    }

    // required 이상 있는지 확인
    public bool HasEnoughSoul(JobCategory category, int required) => GetSoul(category) >= required;

    public string SoulCost(JobCategory category, int required)
    {
        int have = GetSoul(category);

        return $"{have}/{required}";
    }

    // ✅ 소울 감소 함수
    public void DecreaseSoul(JobCategory category, int amount)
    {
        switch (category)
        {
            case JobCategory.Warrior: redSoul -= amount; break;
            case JobCategory.Ranged: blueSoul -= amount; break;
            case JobCategory.Special: purpleSoul -= amount; break;
            case JobCategory.Healer: greenSoul -= amount; break;
        }
    }

    // ✅ 소울 증가 함수
    public void IncreaseSoul(JobCategory category, int amount)
    {
        switch (category)
        {
            case JobCategory.Warrior: redSoul += amount; break;
            case JobCategory.Ranged: blueSoul += amount; break;
            case JobCategory.Special: purpleSoul += amount; break;
            case JobCategory.Healer: greenSoul += amount; break;
        }
    }

    // ✅ 가격 지불 함수
    public void PayHeroPrice(JobCategory category, int price)
    {
        DecreaseSoul(category, price);
    }
}
