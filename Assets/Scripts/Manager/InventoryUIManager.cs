using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 던전 진입 전 Preparation 인벤토리 UI 제어

public class InventoryUIManager : MonoBehaviour
{
    [SerializeField] private DungeonInventory source;
    [SerializeField] private Transform inventoryPanel;
    [SerializeField] private GameObject inventoryActive;

    private List<InventorySlotUI> slotUIs = new();

    void OnEnable()
    {
        if (source == null)
            source = FindObjectOfType<DungeonInventory>(true);

        if (source != null) source.Changed += RefreshUI;

        BuildSlots();   // ★ 하위 슬롯 스캔 & 바인딩
        RefreshUI();
    }

    void OnDisable()
    {
        if (source != null) source.Changed -= RefreshUI;
    }

    // 씬마다 다른 DungeonInventory 재바인딩
    public void SetSource(DungeonInventory inv)
    {
        if (source != null) source.Changed -= RefreshUI;
        source = inv;

        // [역할] 외부에서 SetSource로 갈아낄 때도 null 방어 + 즉시 구독
        if (source == null)
            source = FindObjectOfType<DungeonInventory>(true);

        if (source != null) source.Changed += RefreshUI;

        BuildSlots();   // ★ 소스 바뀌면 다시 바인딩
        RefreshUI();
    }

    // 하위에 있는 6개 item(슬롯)을 모아 InventorySlotUI에 연결
    private void BuildSlots()
    {
        slotUIs.Clear();
        if (inventoryPanel == null || source == null) return; // ← source 보장 후엔 정상 진행

        int index = 0;
        for (int i = 0; i < inventoryPanel.childCount; i++)
        {
            if (index >= 6) break; // 슬롯 6개만 사용
            var child = inventoryPanel.GetChild(i);

            var ui = child.GetComponent<InventorySlotUI>();
            if (ui == null) ui = child.gameObject.AddComponent<InventorySlotUI>();

            ui.Setup(source, index); // 클릭 → RemoveItemAt(index)
            slotUIs.Add(ui);
            index++;
        }
    }

    void InitializeSlots()
    {
        if (source == null) return;
        var slots = source.GetSlots();
        for (int i = 0; i < slotUIs.Count && i < slots.Count; i++)
            slotUIs[i].Setup(source, i);
    }

    // 슬롯 UI 아이콘 / 수량 새로고침
    public void RefreshUI()
    {
        if (source == null || slotUIs.Count == 0) return;

        var slots = source.GetSlots();
        int n = Mathf.Min(slots.Count, slotUIs.Count);
        for (int i = 0; i < n; i++)
        {
            slotUIs[i].UpdateSlot(slots[i]); // 아이콘 & 수량 반영
        }
    }

    // 인벤토리 패널 열기
    public void OpenInventoryPanel()
    {
        if (inventoryActive != null) inventoryActive.SetActive(true);
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null; // 1프레임 뒤 레이아웃 안정화 후
        RefreshUI();
    }

}
