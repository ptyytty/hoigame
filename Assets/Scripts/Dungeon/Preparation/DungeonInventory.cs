using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전에서 사용하는 인벤토리
public class DungeonInventory : MonoBehaviour
{
    private const int maxSlotCount = 6;
    private List<InventorySlot> slots = new();

    [SerializeField] private List<Button> inventory;
    [SerializeField] private List<Image> image;
    [SerializeField] private List<TMP_Text> amount;

    void Awake()
    {
        InitializeSlots();
    }

    public void InitializeSlots()
    {
        slots.Clear();
        for (int i = 0; i < maxSlotCount; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    public bool AddItem(ConsumeItem item)
    {
        if (item == null) return false;

        // 1단계: 같은 아이템 있는 슬롯에 추가
        foreach (var slot in slots)
        {
            if (slot.CanAdd(item))
            {
                slot.AddItem(item);
                return true;
            }
        }

        // 2단계: 빈 슬롯에 추가
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.AddItem(item);
                return true;
            }
        }

        // 3단계: 슬롯이 가득 찼음
        return false;
    }

    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= slots.Count) return false;

        var slot = slots[index];
        if (slot.IsEmpty) return false;

        var removedItem = slot.item;

        slot.RemoveOne();

        PlayerItemManager.instance.AddConsumeItem(removedItem, 1);

        InventoryUIManager.instance.RefreshUI();
        ItemList.instance.RefreshItemList();  // 수량 회복 반영
        return true;
    }

    public List<InventorySlot> GetSlots() => slots;
}