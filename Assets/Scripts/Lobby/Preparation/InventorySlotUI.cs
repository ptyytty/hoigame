using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Preparation 인벤토리 단일 칸 처리
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;      // 슬롯에 표시할 아이콘
    [SerializeField] private TMP_Text countText;   // 수량 텍스트

    private int slotIndex = -1;                   // 이 UI가 바라보는 DungeonInventory 슬롯 인덱스
    private DungeonInventory inventory;           // 연결된 던전 준비 인벤토리
    private Button button;                        // 클릭 처리용 버튼

    /// <summary>
    /// 역할: InventoryUIManager에서 슬롯 번호와 DungeonInventory를 지정해 줄 때 사용
    ///  - 인덱스/인벤토리 지정
    ///  - 버튼 클릭 리스너 등록
    ///  - 현재 상태로 UI 1회 갱신
    /// </summary>
    public void Init(int index, DungeonInventory inv)
    {
        slotIndex = index;
        inventory = inv;

        EnsureRefs();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickSlot);
        }

        RefreshView();
    }

    /// <summary>
    /// 역할: 인스펙터에서 참조를 안 넣었을 때
    ///       같은 오브젝트 하위에서 Image / Text / Button을 찾아 채워준다.
    /// </summary>
    private void EnsureRefs()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (itemImage == null)
        {
            var img = transform.Find("ItemImage");
            if (img != null) itemImage = img.GetComponent<Image>();
        }

        if (countText == null)
        {
            var txt = transform.Find("Text_Count");
            if (txt != null) countText = txt.GetComponent<TMP_Text>();
        }
    }

    /// <summary>
    /// 역할: 현재 inventory 상태에 맞춰 이 슬롯의 UI를 갱신
    /// </summary>
    public void RefreshView()
    {
        if (inventory == null || slotIndex < 0)
        {
            Clear();
            return;
        }

        var slots = inventory.GetSlots();
        if (slots == null || slotIndex >= slots.Count)
        {
            Clear();
            return;
        }

        var slot = slots[slotIndex];

        if (slot.IsEmpty || slot.item == null || slot.count <= 0)
        {
            Clear();
        }
        else
        {
            if (itemImage != null)
            {
                itemImage.enabled = true;
                itemImage.sprite = slot.item.icon;
            }

            if (countText != null)
                countText.text = slot.count.ToString();
        }
    }

    /// <summary>
    /// 역할: 비어 있는 슬롯 상태로 UI 초기화
    /// </summary>
    private void Clear()
    {
        if (itemImage != null)
        {
            itemImage.enabled = false;
            itemImage.sprite = null;
        }

        if (countText != null)
            countText.text = string.Empty;
    }

    /// <summary>
    /// 역할: 슬롯이 클릭되었을 때 호출
    ///  - DungeonInventory에서 해당 인덱스 아이템을 1개 제거
    ///  - 제거된 아이템은 DungeonInventory.RemoveItemAt 내부에서
    ///    InventoryRuntime(전체 인벤토리)에 자동으로 복구됨
    ///  - 성공 시 준비 인벤토리 UI + 아이템 리스트 UI 둘 다 갱신
    /// </summary>
    private void OnClickSlot()
    {
        if (inventory == null || slotIndex < 0)
            return;

        bool removed = inventory.RemoveItemAt(slotIndex);
        if (!removed)
            return;

        // 슬롯 UI는 DungeonInventory.Changed → InventoryUIManager.RefreshUI 로 갱신됨

        // 내 아이템 리스트 수량 갱신 (DungeonInventory에서 돌려보낸 것 반영)
        if (ItemList.instance != null)
            ItemList.instance.RefreshItemList();
    }
}
