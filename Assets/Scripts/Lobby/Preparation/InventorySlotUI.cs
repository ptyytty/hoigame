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

    private void EnsureRefs()
    {
        if (itemImage == null)
        {
            var t = transform.Find("itemimage");
            if (t) itemImage = t.GetComponent<Image>();
            if (itemImage == null) itemImage = GetComponentInChildren<Image>(true);
        }
        if (countText == null)
        {
            var t = transform.Find("amount");
            if (t) countText = t.GetComponent<TMP_Text>();
            if (countText == null) countText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    // 아이템 제거
    public void Setup(DungeonInventory inv, int index)
    {
        inventory = inv;
        slotIndex = index;

        EnsureRefs();

        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => inventory.RemoveItemAt(slotIndex));
        }
    }

    // 슬롯이 비었을 때
    public void UpdateSlot(InventorySlot slot)
    {
        EnsureRefs();

        if (slot.IsEmpty)
        {
            if (itemImage) itemImage.enabled = false;
            if (countText) countText.text = "";
        }
        else
        {
            if (itemImage)
            {
                itemImage.enabled = true;
                itemImage.sprite = slot.item.icon;
            }
            if (countText) countText.text = slot.count.ToString();
        }

    }
}
