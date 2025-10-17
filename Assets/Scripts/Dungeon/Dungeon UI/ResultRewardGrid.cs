using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultRewardGrid : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private Transform contentParent;      // 아이템들이 들어갈 부모(그리드/수평 레이아웃)
    [SerializeField] private GameObject rewardItemPrefab;  // "Icon"(Image) + "Amount"(TMP_Text)

    [Header("Icons")]
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Sprite[] soulSprites; // index = (int)SoulType

    [Header("Sort")]
    [Tooltip("수량 기준 내림차순 정렬")]
    [SerializeField] private bool sortByAmountDesc = true;

    [Tooltip("동률일 때 우선순위")]
    [SerializeField] private int coinPriority = 0;
    [SerializeField] private int redSoulPriority = 1;
    [SerializeField] private int blueSoulPriority = 2;
    [SerializeField] private int greenSoulPriority = 3;

    // ---------- 내부 상태(풀링/뷰 캐시) ----------
    private readonly List<GameObject> _pool = new();
    private readonly List<ItemUI> _uiCache = new(); // _pool 과 인덱스 동일 유지
    private int _activeCount = 0;

    private struct ItemUI
    {
        public Image icon;      // 재화 아이콘용 Image(자식 "Icon")
        public TMP_Text amount; // 수량 텍스트(자식 "Amount")
    }

    // ---------- 유틸 ----------

    /// <summary>역할: 자식에서 이름으로 컴포넌트를 찾는다(빠르고 확실하게 지정).</summary>
    private static T FindByName<T>(Transform root, string childName) where T : Component
    {
        var t = root.Find(childName);
        return t ? t.GetComponent<T>() : null;
    }

    /// <summary>역할: 풀에서 하나 꺼내거나 새로 생성하고, UI 캐시를 반환한다.</summary>
    private (GameObject go, ItemUI ui) GetPooled()
    {
        if (_activeCount < _pool.Count)
        {
            var go = _pool[_activeCount];
            go.SetActive(true);
            var ui = _uiCache[_activeCount];
            _activeCount++;
            return (go, ui);
        }

        var inst = Instantiate(rewardItemPrefab, contentParent);
        // ★ 여기서 배경이 아닌 자식 "Icon"/"Amount"를 정확히 캐시
        var uiNew = new ItemUI
        {
            icon = FindByName<Image>(inst.transform, "Icon"),
            amount = FindByName<TMP_Text>(inst.transform, "Amount")
        };

        _pool.Add(inst);
        _uiCache.Add(uiNew);
        _activeCount++;
        return (inst, uiNew);
    }

    /// <summary>역할: 현재 활성 아이템을 모두 비활성화(다음 Rebind 때 재사용).</summary>
    private void DeactivateAll()
    {
        for (int i = 0; i < _pool.Count; i++)
            _pool[i].SetActive(false);
        _activeCount = 0;
    }

    // ---------- 외부 진입점 ----------

    /// <summary>역할: 현재 RunReward 합산값으로 갱신한다.</summary>
    public void RebindFromCurrentRun()
    {
        var totals = RunReward.Instance ? RunReward.Instance.GetTotals() : null;
        Bind(totals);
    }

    /// <summary>역할: 전달된 합산 데이터로 프리팹 생성/정렬/표시.</summary>
    public void Bind(DungeonRunRewardsData totals)
    {
        DeactivateAll();
        if (totals == null) return;

        // 1) 목록 구성(수량 1 이상만)
        var list = new List<Entry>(4);
        if (totals.coins > 0)
            list.Add(new Entry("Coin", totals.coins, coinSprite, coinPriority));

        AddSoulIfPositive(list, SoulType.RedSoul,  totals.souls[(int)SoulType.RedSoul],  redSoulPriority);
        AddSoulIfPositive(list, SoulType.BlueSoul, totals.souls[(int)SoulType.BlueSoul], blueSoulPriority);
        AddSoulIfPositive(list, SoulType.Green,    totals.souls[(int)SoulType.Green],    greenSoulPriority);

        // 2) 정렬(수량 내림차순 → 우선순위 오름차순)
        list.Sort((a, b) =>
        {
            int byAmount = sortByAmountDesc ? b.amount.CompareTo(a.amount) : a.amount.CompareTo(b.amount);
            return (byAmount != 0) ? byAmount : a.priority.CompareTo(b.priority);
        });

        // 3) 슬롯에 주입
        for (int i = 0; i < list.Count; i++)
        {
            var (go, ui) = GetPooled();

            // ★ 배경이 아닌 자식 "Icon" 이미지에만 스프라이트 주입
            if (ui.icon)   ui.icon.sprite = list[i].icon;
            if (ui.amount) ui.amount.text = list[i].amount.ToString("N0");
        }

        // 4) 레이아웃 즉시 갱신(모바일 대비)
        if (contentParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private void OnEnable()
    {
        // 역할: 결과 패널이 열릴 때 최신 값으로 자동 갱신
        RebindFromCurrentRun();
    }

    // ---------- 내부 도우미 ----------
    private void AddSoulIfPositive(List<Entry> list, SoulType type, int amount, int prio)
    {
        if (amount <= 0) return;
        var idx = (int)type;
        var icon = (soulSprites != null && idx >= 0 && idx < soulSprites.Length) ? soulSprites[idx] : null;
        list.Add(new Entry(type.ToString(), amount, icon, prio));
    }

    private readonly struct Entry
    {
        public readonly string label;
        public readonly int amount;
        public readonly Sprite icon;
        public readonly int priority;

        public Entry(string label, int amount, Sprite icon, int priority)
        {
            this.label = label;
            this.amount = amount;
            this.icon = icon;
            this.priority = priority;
        }
    }
}
