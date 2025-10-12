using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전에서 사용하는 인벤토리
public class DungeonInventory : MonoBehaviour
{
    public event Action Changed;
    void Notify() => Changed?.Invoke();
    private const int maxSlotCount = 6;
    private List<InventorySlot> slots = new();

    [System.Serializable]
    public struct SlotDTO
    {
        public ConsumeItem item;
        public int count;
    }

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

        // 1단계: 같은 아이템 있는 슬롯에 추가 (인덱스 기반)
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.CanAdd(item))
            {
                s.AddItem(item);
                slots[i] = s;        // ← struct일 경우를 대비해 반드시 되돌려쓰기
                Notify();            // ← UI에게 변경 알림
                return true;
            }
        }

        // 2단계: 빈 슬롯에 추가
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.IsEmpty)
            {
                s.AddItem(item);
                slots[i] = s;
                Notify();
                return true;
            }
        }
        // 3단계: 슬롯이 가득 찼음
        return false;
    }

    public bool RemoveItemAt(int index)
    {
        if (index < 0 || index >= slots.Count) return false;

        var s = slots[index];
        if (s.IsEmpty) return false;

        var removedItem = s.item;
        s.RemoveOne();
        slots[index] = s;                // ← struct 안전

        if (InventoryRuntime.Instance != null)
            InventoryRuntime.Instance.AddConsumeItem(removedItem, 1);
            
        Notify();                        // ← UI에게 변경 알림
        return true;
    }

    public List<SlotDTO> CreateSnapshot()
    {
        var result = new List<SlotDTO>(slots.Count);
        foreach (var s in slots)
        {
            result.Add(new SlotDTO { item = s.item, count = s.count });
        }
        return result;
    }

    public void ApplySnapshot(List<SlotDTO> snap)
    {
        InitializeSlots(); // 슬롯 비우고 6칸 재초기화
        int n = Mathf.Min(snap.Count, slots.Count);
        for (int i = 0; i < n; i++)
        {
            var dto = snap[i];
            if (dto.item == null || dto.count <= 0) continue;
            // 스택 상한을 지키면서 채우기
            for (int c = 0; c < dto.count; c++) AddItem(dto.item);
        }
        Changed?.Invoke(); // 이벤트 방식이면 갱신 신호 한 번
    }

    public List<InventorySlot> GetSlots() => slots;
}