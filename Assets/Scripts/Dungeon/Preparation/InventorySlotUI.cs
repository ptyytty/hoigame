using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text countText;
    private int slotIndex;
    private DungeonInventory inventory;

    // 아이템 제거
    public void Setup(DungeonInventory inv, int index)
    {
        inventory = inv;
        slotIndex = index;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            inventory.RemoveItemAt(slotIndex);
            InventoryUIManager.instance.RefreshUI();
        });
    }

    // 슬롯이 비었을 때
    public void UpdateSlot(InventorySlot slot)
    {
        if (slot.IsEmpty)
        {
            itemImage.enabled = false;
            countText.text = "";
        }
        else
        {
            itemImage.enabled = true;
            itemImage.sprite = slot.item.icon;
            countText.text = slot.count.ToString();
        }
    }
}
