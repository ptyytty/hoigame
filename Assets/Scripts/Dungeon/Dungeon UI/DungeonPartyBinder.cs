using System.Linq;
using UnityEngine;

public class DungeonPartyBinder : MonoBehaviour
{
    [SerializeField] private DungeonPartyUI dungeonPartyUI; // 중앙 2x2 표시용
    [SerializeField] private DungeonInventoryBinder inventoryBinder; // 네가 쓰는 바인더가 있으면

    void Awake()
    {
        var bridge = PartyBridge.Instance;
        if (bridge == null || !bridge.HasParty())
        {
            Debug.LogWarning("⚠️ PartyBridge가 비어있음");
            return;
        }

        // (안전망) equippedItemId -> 런타임 캐시 복원 (스탯 재적용은 하지 않음)
        foreach (var h in bridge.ActiveParty)
        {
            if (h == null) continue;
            if (h.equippedItem == null && h.equippedItemId != 0)
            {
                var item = InventoryRuntime.Instance != null
                    ? InventoryRuntime.Instance.LookupEquipItemById(h.equippedItemId)
                    : null;
                if (item != null) h.equippedItem = item;
            }
        }

        // 중앙 2x2 파티 UI 채우기
        if (dungeonPartyUI != null)
            dungeonPartyUI.ApplyHeroes(bridge.ActiveParty.ToArray());

        // 인벤토리 바인더: 씬 내 DungeonInventory 찾아 Bind (ApplySnapshot 호출 제거)
        if (inventoryBinder != null)
        {
            var inv = FindObjectOfType<DungeonInventory>(true);
            if (inv != null) inventoryBinder.Bind(inv);
            else inventoryBinder.Refresh();
        }
    }

}
