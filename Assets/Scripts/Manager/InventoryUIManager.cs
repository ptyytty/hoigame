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
        InitializeSlots();
        RefreshUI();
    }

    void InitializeSlots()
    {
        slotUIs.Clear();

        for (int i = 0; i < inventory.GetSlots().Count; i++)
        {
            var slotObj = inventoryPanel.GetChild(i).gameObject;
            var ui = slotObj.GetComponent<InventorySlotUI>();
            ui.Setup(inventory, i);
            slotUIs.Add(ui);
        }
    }

    public void RefreshUI()
    {
        var slots = inventory.GetSlots();
        Debug.Log(slots);

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].UpdateSlot(slots[i]);
        }
    }


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
