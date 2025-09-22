using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager instance;

    [SerializeField] private DungeonInventory inventory;
    [SerializeField] private Transform inventoryPanel;
    [SerializeField] private GameObject inventoryActive;

    private List<InventorySlotUI> slotUIs = new();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartCoroutine(WaitForInventoryReady());
    }

    private IEnumerator WaitForInventoryReady()
    {
        // DungeonInventory가 SetActive(false) 상태면 대기
        while (!inventory.gameObject.activeInHierarchy)
            yield return null;

        // Awake() 완료까지 한 프레임 더 대기
        yield return null;

        if (inventory.GetSlots().Count == 0)
            inventory.InitializeSlots();

        InitializeSlots();
        RefreshUI();
    }

    private void InitializeSlots()
    {
        slotUIs.Clear();
        var slots = inventory.GetSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            var slotObj = inventoryPanel.GetChild(i).gameObject;
            var ui = slotObj.GetComponent<InventorySlotUI>();
            if (ui != null)
            {
                ui.Setup(inventory, i);
                slotUIs.Add(ui);
            }
            else
            {
                Debug.LogWarning($"❗ 슬롯 오브젝트 {slotObj.name}에 InventorySlotUI가 없습니다.");
            }
        }
    }

    public void RefreshUI()
    {
        var slots = inventory.GetSlots();

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateSlot(slots[i]);
        }
    }


    // 인벤토리 패널 true
    public void OpenInventoryPanel()
    {
        inventoryActive.SetActive(true);      // 패널을 보여주고
        StartCoroutine(DelayedRefresh());
    }
    
    private IEnumerator DelayedRefresh()
    {
        yield return null; // 1 프레임 뒤에 실행
        RefreshUI();
    }

}
