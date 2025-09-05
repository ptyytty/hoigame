using System.Collections.Generic;
using UnityEngine;
using Save;

public class InventoryRuntime : MonoBehaviour
{
    public static InventoryRuntime Instance { get; private set; }

    [Header("Wallet / Currencies")]
    public int Gold;
    public int redSoul;
    public int blueSoul;
    public int purpleSoul;
    public int greenSoul;

    // === 내부 보유 구조 ===
    private readonly List<OwnedItem<ConsumeItem>> ownedConsume = new();
    public  readonly List<OwnedItem<EquipItem>>   ownedEquipItem = new(); // ItemList가 그대로 씀

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Save → Runtime ----------
    public void LoadFromSave(InventorySave saveInv)
    {
        ownedConsume.Clear();
        ownedEquipItem.Clear();

        if (saveInv?.slots == null) return;

        foreach (var s in saveInv.slots)
        {
            if (s.count <= 0 || s.itemId <= 0) continue;

            if (s.type == ItemType.Consume)
            {
                var def = ItemCatalog.GetConsume(s.itemId);
                if (def == null) continue;
                AddConsumeItem(def, s.count);
            }
            else // Equipment
            {
                var def = ItemCatalog.GetEquip(s.itemId);
                if (def == null) continue;
                // 장비는 1개씩
                for (int i = 0; i < Mathf.Max(1, s.count); i++)
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
                num    = 0,
                type   = ItemType.Consume,
                count  = owned.count
            });
        }

        // 장비: 개별 1개씩
        foreach (var owned in ownedEquipItem)
        {
            if (owned.count <= 0) continue; // 일반적으론 항상 1
            slots.Add(new Save.Item
            {
                itemId = owned.itemData.id_item,
                num    = 0, // 필요하면 장비 식별번호 할당
                type   = ItemType.Equipment,
                count  = 1
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

    public void AddConsumeItem(ConsumeItem item, int delta)
    {
        var found = ownedConsume.Find(x => x.itemData.id_item == item.id_item);
        if (found == null)
        {
            if (delta > 0) ownedConsume.Add(new OwnedItem<ConsumeItem>(item, delta));
            return;
        }
        found.count += delta;
        if (found.count <= 0) ownedConsume.Remove(found);
    }

    public void AddEquipItem(EquipItem item)
    {
        // 장비는 1개씩 보유(필요 시 중복 허용)
        ownedEquipItem.Add(new OwnedItem<EquipItem>(item, 1));
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

    public void ClearAll()
    {
        ownedConsume.Clear();
        ownedEquipItem.Clear();
    }

    public void PushEmpty() { /* 빈 슬롯 개념이 없으니 무시해도 됨 */ }
}
