using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoulType { RedSoul = 0, BlueSoul = 1, Green = 2} // 원하는 이름으로 교체

[Serializable]
public class DungeonRunRewardsData
{
    public int[] souls = new int[3]; // 인덱스 = (int)SoulType
    public int coins = 0;

    public void Add(SoulType type, int amount)
    {
        souls[(int)type] += Mathf.Max(0, amount);
    }
    public void AddCoins(int amount)
    {
        coins += Mathf.Max(0, amount);
    }
}

/// <summary>
/// 던전 진행 중 보상 모음
/// </summary>
public class RunReward : MonoBehaviour
{
    public static RunReward Instance { get; private set; }

    [Tooltip("변경될 때마다 호출됩니다(저장 훅 연결 지점).")]
    public Action<DungeonRunRewardsData> OnChanged;

    [SerializeField] private DungeonRunRewardsData totals = new DungeonRunRewardsData();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddBattleDrop(SoulType soulType, int soulAmount, int coinAmount)
    {
        totals.Add(soulType, soulAmount);
        totals.AddCoins(coinAmount);
        OnChanged?.Invoke(totals); // ← 여기서 PlayerProgressService.Save() 같은 거 호출 연결
    }

    public DungeonRunRewardsData GetTotals() => totals;

    public void Clear()
    {
        totals = new DungeonRunRewardsData();
        OnChanged?.Invoke(totals);
    }
}
