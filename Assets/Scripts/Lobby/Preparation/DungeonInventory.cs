using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 던전에서 사용할 인벤토리에 아이템 추가 / 제거
public class DungeonInventory : MonoBehaviour
{
    public event Action Changed;
    void Notify() => Changed?.Invoke();

    private const int maxSlotCount = 6;
    private List<InventorySlot> slots = new();

    [Serializable]
    public struct SlotDTO
    {
        public ConsumeItem item;
        public int count;
    }

    /// <summary>
    /// 역할: 던전 인벤토리 컴포넌트가 생성될 때 슬롯을 6칸으로 초기화
    /// </summary>
    void Awake()
    {
        InitializeSlots();
    }

    /// <summary>
    /// 역할: 슬롯 리스트를 6칸 비어 있는 상태로 재구성
    /// </summary>
    public void InitializeSlots()
    {
        slots.Clear();
        for (int i = 0; i < maxSlotCount; i++)
        {
            slots.Add(new InventorySlot());
        }
    }

    /// <summary>
    /// 역할: 던전 인벤토리에 소비 아이템 1개를 추가
    ///  - 같은 아이템이 들어 있는 슬롯이 있으면 거기에 스택
    ///  - 없다면 비어 있는 슬롯에 새로 추가
    ///  - 슬롯이 전부 꽉 차 있으면 false 리턴
    ///  - 빌드에서 Awake가 안 불렸거나 프리팹이 참조되었을 때를 대비해
    ///    slots가 비어 있으면 InitializeSlots()를 한 번 더 호출
    /// </summary>
    public bool AddItem(ConsumeItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[DungeonInventory] AddItem 호출됨 - item == null");
            return false;
        }

        // 🔒 방어 코드: 슬롯이 아직 초기화되지 않았으면 한 번 더 초기화
        if (slots == null)
        {
            Debug.LogWarning("[DungeonInventory] slots == null, 새 리스트 생성");
            slots = new List<InventorySlot>();
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("[DungeonInventory] slots.Count == 0, InitializeSlots() 재호출");
            InitializeSlots();
        }

        // 현재 슬롯 상태를 한 번 덤프 (디버그용)
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            string itemName = (s.item != null) ? s.item.name_item : "null";
            Debug.Log($"[DungeonInventory] Slot[{i}] item={itemName}, count={s.count}, empty={s.IsEmpty}");
        }

        // 1단계: 같은 아이템 있는 슬롯에 추가 (인덱스 기반)
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s.CanAdd(item))
            {
                Debug.Log($"[DungeonInventory] Slot[{i}] 에 스택 추가");
                s.AddItem(item);
                slots[i] = s;        // ← struct일 경우 되돌려쓰기
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
                Debug.Log($"[DungeonInventory] 빈 Slot[{i}] 에 신규 추가");
                s.AddItem(item);
                slots[i] = s;
                Notify();
                return true;
            }
        }

        // 3단계: 슬롯이 가득 찼음
        Debug.LogWarning("[DungeonInventory] AddItem 실패 - 모든 슬롯이 가득 찼습니다.");
        return false;
    }

    /// <summary>
    /// 역할: 던전 인벤토리에서 특정 인덱스의 아이템 1개를 제거하고
    ///       제거된 아이템은 다시 InventoryRuntime(전체 인벤토리)에 반환
    /// </summary>
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

    /// <summary>
    /// 역할: 던전 인벤토리에 있는 모든 아이템을 원래 인벤토리로 돌려보내고
    ///       슬롯을 완전히 비운 뒤 UI에 갱신 신호를 보냄
    /// </summary>
    public void ClearToInventory()
    {
        Debug.Log("[DungeonInventory] ClearToInventory 호출");

        var inv = InventoryRuntime.Instance;
        if (inv != null)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.IsEmpty) continue;
                if (s.item != null && s.count > 0)
                {
                    // [역할] 준비 칸에 쌓여 있던 수량을 원래 보유 인벤토리로 복귀
                    inv.AddConsumeItem(s.item, s.count);
                }
            }
        }

        InitializeSlots(); // [역할] 6칸 비우기
        Notify();          // [역할] UI에 즉시 갱신 통지
    }

    // ================== 6칸 스냅샷 저장 / 복원 ==================
    /// <summary>
    /// 역할: 현재 6칸(슬롯)의 상태를 (아이템, 수량) DTO 리스트로 스냅샷 생성
    /// </summary>
    public List<SlotDTO> CreateSnapshot()
    {
        var result = new List<SlotDTO>(slots.Count);
        foreach (var s in slots)
        {
            result.Add(new SlotDTO { item = s.item, count = s.count });
        }
        return result;
    }

    /// <summary>
    /// 역할: 던전 진입 시, 이전에 저장해둔 스냅샷(로드아웃)을 적용
    /// </summary>
    /// <param name="snap">ConsumeItem, count</param>
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

    /// <summary>
    /// 역할: 외부에서 슬롯 전체 상태를 읽을 때 사용
    /// </summary>
    public List<InventorySlot> GetSlots() => slots;
}