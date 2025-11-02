using System.Collections.Generic;
using UnityEngine;
using Save;
using System;

// 실시간 사용 인벤토리
public class InventoryRuntime : MonoBehaviour
{
    public static InventoryRuntime Instance { get; private set; }

    [Header("Wallet / Currencies")]
    public int Gold;
    public int redSoul;
    public int blueSoul;
    public int greenSoul;

    // [역할] 재화 값이 바뀔 때 구독 UI/시스템에 알려주는 이벤트
    public event System.Action OnCurrencyChanged;

    public enum CurrencyType { Gold, RedSoul, BlueSoul, GreenSoul }

    // === 내부 보유 구조 ===
    private readonly List<OwnedItem<ConsumeItem>> ownedConsume = new();
    public readonly List<OwnedItem<EquipItem>> ownedEquipItem = new(); // ItemList가 그대로 씀

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 재화 설정
    private void SetCurrency(CurrencyType type, int value)
    {
        value = Mathf.Max(0, value);
        switch (type)
        {
            case CurrencyType.Gold: Gold = value; break;
            case CurrencyType.RedSoul: redSoul = value; break;
            case CurrencyType.BlueSoul: blueSoul = value; break;
            case CurrencyType.GreenSoul: greenSoul = value; break;
        }
    }

    // === 편의 API: 골드 ===
    /// <summary> [역할] 골드 지급(음수 방지, HUD 갱신) </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold = Mathf.Max(0, Gold + amount);
        OnCurrencyChanged?.Invoke();
    }

    /// <summary> [역할] 골드 차감 시도(성공 시 true, 실패 시 false) </summary>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (Gold < amount) return false;
        Gold -= amount;
        OnCurrencyChanged?.Invoke();
        return true;
    }

    // === 편의 API: 소울(직군별) ===
    /// <summary> [역할] 직군별 소울 현재 보유량 조회 </summary>
    public int GetSoul(JobCategory category)
    {
        switch (category)
        {
            case JobCategory.Warrior: return redSoul;
            case JobCategory.Ranged: return blueSoul;
            case JobCategory.Healer: return greenSoul;
            default: return 0;         // Special 등은 현재 미사용 정책
        }
    }

    /// <summary> [역할] 직군별 소울 지급 </summary>
    public void AddSoul(JobCategory category, int amount)
    {
        if (amount <= 0) return;
        switch (category)
        {
            case JobCategory.Warrior: redSoul = Mathf.Max(0, redSoul + amount); break;
            case JobCategory.Ranged: blueSoul = Mathf.Max(0, blueSoul + amount); break;
            case JobCategory.Healer: greenSoul = Mathf.Max(0, greenSoul + amount); break;
            default: return;
        }
        OnCurrencyChanged?.Invoke();
    }

    /// <summary> [역할] 직군별 소울 차감 시도(성공/실패) </summary>
    public bool TrySpendSoul(JobCategory category, int amount)
    {
        if (amount <= 0) return true;
        switch (category)
        {
            case JobCategory.Warrior:
                if (redSoul < amount) return false; redSoul -= amount; break;
            case JobCategory.Ranged:
                if (blueSoul < amount) return false; blueSoul -= amount; break;
            case JobCategory.Healer:
                if (greenSoul < amount) return false; greenSoul -= amount; break;
            default: return false;
        }
        OnCurrencyChanged?.Invoke();
        return true;
    }

    public bool HasEnoughSoul(JobCategory category, int required)
        => GetSoul(category) >= required;

    // === 내부 유틸 ===
    private void RaiseChanged() => OnCurrencyChanged?.Invoke();     // [역할] 내부 로딩/복구 후 HUD 갱신 트리거


    // ---------- Save → Runtime ----------
    public void LoadFromSave(InventorySave saveInv)
    {
        // 보유 아이템 초기화
        ownedConsume.Clear();
        ownedEquipItem.Clear();

        if (saveInv?.slots == null) return;

        foreach (var item in saveInv.slots)
        {
            if (item.count <= 0 || item.itemId <= 0) continue;

            if (item.type == ItemType.Consume)
            {
                var def = ItemCatalog.GetConsume(item.itemId);
                if (def == null) continue;
                AddConsumeItem(def, item.count);
            }
            else // Equipment
            {
                var def = ItemCatalog.GetEquip(item.itemId);
                if (def == null) continue;
                // 장비는 1개씩
                for (int i = 0; i < Mathf.Max(1, item.count); i++)
                    AddEquipItem(def);
            }
        }
    }

    // ---------- Runtime → Save ----------
    public List<Save.Item> ToSaveSlots()
    {
        var slots = new List<Save.Item>();

        // 소비: 스택형
        foreach (var owned in ownedConsume)
        {
            if (owned.count <= 0) continue;
            slots.Add(new Save.Item
            {
                itemId = owned.itemData.id_item,
                num = 0,
                type = ItemType.Consume,
                count = owned.count
            });
        }

        // 장비: 개별 1개씩
        foreach (var owned in ownedEquipItem)
        {
            if (owned.count <= 0) continue; // 일반적으론 항상 1
            slots.Add(new Save.Item
            {
                itemId = owned.itemData.id_item,
                num = 0, // 필요하면 장비 식별번호 할당
                type = ItemType.Equipment,
                count = 1
            });
        }

        return slots;
    }

    // ---------- UI/전투용 편의 API ----------
    public IEnumerable<OwnedItem<ConsumeItem>> GetOwnedConsumeItems() => ownedConsume;

    public int GetConsumeItemCount(ConsumeItem item)
    {
        var found = ownedConsume.Find(x => x.itemData.id_item == item.id_item);
        return found?.count ?? 0;
    }

    public void AddConsumeItem(ConsumeItem item, int count)
    {
        var found = ownedConsume.Find(x => x.itemData.id_item == item.id_item);
        if (found == null)
        {
            if (count > 0) ownedConsume.Add(new OwnedItem<ConsumeItem>(item, count));
            return;
        }
        found.count += count;
        if (found.count <= 0) ownedConsume.Remove(found);
    }

    // [역할] 소비 아이템을 수량만큼 감소시키고 0 이하이면 제거
    public void RemoveConsumeItem(int itemId, int amount)
    {
        if (amount <= 0) return;

        var slot = ownedConsume.Find(x => x.itemData != null && x.itemData.id_item == itemId);
        if (slot == null) return;

        slot.count = Mathf.Max(0, slot.count - amount);
        if (slot.count == 0)
            ownedConsume.Remove(slot);

        // [역할] UI/HUD 등 구독자에게 변경 알림
        NotifyChanged();
    }

    public void AddEquipItem(EquipItem item)
    {
        // 장비는 1개씩 보유(필요 시 중복 허용)
        ownedEquipItem.Add(new OwnedItem<EquipItem>(item, 1));
    }

    public void RemoveEquipItem(int itemId)
    {
        var slot = ownedEquipItem.Find(x => x.itemData != null && x.itemData.id_item == itemId);
        if (slot == null) return;

        ownedEquipItem.Remove(slot);

        // [역할] UI/HUD 등 구독자에게 변경 알림
        NotifyChanged();
    }

    // PlayerProgressService가 쓰는 최소 API (슬롯 열람)
    public struct RuntimeSlot { public int itemId, num, count; public ItemType type; }
    public IEnumerable<RuntimeSlot> GetAllSlots()
    {
        foreach (var c in ownedConsume)
            yield return new RuntimeSlot { itemId = c.itemData.id_item, num = 0, type = ItemType.Consume, count = c.count };
        foreach (var e in ownedEquipItem)
            yield return new RuntimeSlot { itemId = e.itemData.id_item, num = 0, type = ItemType.Equipment, count = 1 };
    }

    // 던전 진입 시 아이템 캐시 복구
    public EquipItem LookupEquipItemById(int id)
    {
        if (id <= 0) return null;

        // 1) 보유 목록에서 먼저 검색
        var owned = ownedEquipItem.Find(x => x.itemData != null && x.itemData.id_item == id);
        if (owned != null) return owned.itemData;

        // 2) 카탈로그에서 최후 보정(세이브 복구 등)
        return ItemCatalog.GetEquip(id);
    }

    public void PushEmpty() { /* 빈 슬롯 개념이 없으니 무시해도 됨 */ }

    internal void Clear()
    {
        ownedConsume.Clear();
        ownedEquipItem.Clear();
    }

    public void NotifyChanged() => OnCurrencyChanged?.Invoke();
}
