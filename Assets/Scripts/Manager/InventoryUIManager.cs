using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 던전 진입 전 Preparation 인벤토리 UI 제어

public class InventoryUIManager : MonoBehaviour
{
    [Header("Inventory Source")]
    [SerializeField] private DungeonInventory source;     // 준비용 DungeonInventory (같은 오브젝트에 붙음)

    [Header("UI Refs")]
    [SerializeField] private Transform inventoryPanel;    // 6칸 슬롯들이 들어있는 부모
    [SerializeField] private GameObject inventoryActive;  // 인벤토리 패널 루트 (On/Off)

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    /// <summary>
    /// 역할: 이 오브젝트에 붙어 있는 DungeonInventory를 자동으로 가져옴
    /// </summary>
    private void Awake()
    {
        if (source == null)
            source = GetComponent<DungeonInventory>();

        if (source == null)
            Debug.LogError("[InventoryUIManager] 같은 오브젝트에 DungeonInventory가 없습니다. (준비 씬)");
    }

    /// <summary>
    /// 역할: UI가 활성화될 때 DungeonInventory 변경 이벤트를 구독하고 슬롯 빌드/갱신
    /// </summary>
    private void OnEnable()
    {
        if (source == null)
            source = GetComponent<DungeonInventory>();

        if (source == null)
        {
            Debug.LogError("[InventoryUIManager] source가 없습니다. DungeonInventory를 같은 오브젝트에 붙이세요.");
            return;
        }

        source.Changed += RefreshUI;

        BuildSlots();
        RefreshUI();
    }

    /// <summary>
    /// 역할: 비활성화 시 이벤트 구독 해제
    /// </summary>
    private void OnDisable()
    {
        if (source != null)
            source.Changed -= RefreshUI;
    }

    /// <summary>
    /// 역할: inventoryPanel 아래에 있는 모든 InventorySlotUI를 찾아
    ///       슬롯 인덱스와 DungeonInventory 참조를 설정
    /// </summary>
    private void BuildSlots()
    {
        slotUIs.Clear();

        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryUIManager] inventoryPanel이 설정되어 있지 않습니다.");
            return;
        }

        // 비활성 슬롯까지 전부 포함해서 가져오기
        var slots = inventoryPanel.GetComponentsInChildren<InventorySlotUI>(true);
        if (slots == null || slots.Length == 0)
        {
            Debug.LogWarning("[InventoryUIManager] InventorySlotUI가 자식에 없습니다.");
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            var ui = slots[i];
            ui.Init(i, source);   // ← 슬롯 번호 & 인벤토리 지정
            slotUIs.Add(ui);
        }
    }

    /// <summary>
    /// 역할: DungeonInventory 현재 상태에 맞춰 모든 슬롯 UI 갱신
    ///       (Changed 이벤트, 패널 열기, 슬롯 빌드 후에 호출)
    /// </summary>
    public void RefreshUI()
    {
        if (source == null)
            return;

        if (slotUIs.Count == 0)
            BuildSlots();

        foreach (var ui in slotUIs)
        {
            if (ui != null)
                ui.RefreshView();
        }
    }

    /// <summary>
    /// 역할: 인벤토리 패널 열기 버튼에서 호출
    /// </summary>
    public void OpenInventoryPanel()
    {
        if (inventoryActive != null)
            inventoryActive.SetActive(true);

        // 레이아웃 정리 후 한 프레임 뒤에 갱신
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        RefreshUI();
    }
}
