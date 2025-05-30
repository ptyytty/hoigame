using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemList : MonoBehaviour
{
    [SerializeField] private Toggle toggleConsume;
    [SerializeField] private Toggle toggleEquip;

    [SerializeField] private Button itemButtonPrefab;
    [SerializeField] private Transform itemListPanel;
    [SerializeField] private DungeonInventory dungeonInventory;

    public static ItemList instance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        toggleConsume.onValueChanged.AddListener(OnToggleChanged);
        toggleEquip.onValueChanged.AddListener(OnToggleChanged);

        toggleConsume.isOn = true;
        RefreshItemList();
    }

    void OnToggleChanged(bool _)
    {
        RefreshItemList();
    }

    void PrintConsumeItem()
    {
        foreach (Transform child in itemListPanel)
        {
            Destroy(child.gameObject);
        }

        var items = PlayerItemManager.instance.GetOwnedConsumeItems();

        foreach (var ownedItem in items)
        {
            if (ownedItem.count <= 0) continue;
            Button itemButton = Instantiate(itemButtonPrefab, itemListPanel);
            TMP_Text itemName = itemButton.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text itemAmount = itemButton.transform.Find("ItemAmount").GetComponent<TMP_Text>();

            itemName.text = ownedItem.itemData.name_item;
            itemAmount.text = "수량: " + ownedItem.count.ToString();

            // 🔥 클로저 문제 방지: 로컬 변수로 복사
            var currentItem = ownedItem.itemData;

            itemButton.onClick.AddListener(() =>
            {
                if (PlayerItemManager.instance.GetConsumeItemCount(currentItem) > 0)
                {
                    bool success = dungeonInventory.AddItem(currentItem);
                    if (success)
                    {
                        PlayerItemManager.instance.AddConsumeItem(currentItem, -1);
                        // 🔥 이 UIManager가 열려 있는 상태일 때만 갱신
                        if (InventoryUIManager.instance.gameObject.activeInHierarchy)
                            InventoryUIManager.instance.RefreshUI();
                        RefreshItemList();
                    }
                }
            });
        }
    }

    // void PrintEquipItems()
    // {
    //     var items = PlayerItemManager.instance.ownedEquipItem;

    //     foreach (var ownedItem in items)
    //     {
    //         var button = Instantiate(itemButtonPrefab, itemListPanel);
    //         var itemName = button.transform.Find("ItemName").GetComponent<TMP_Text>();
    //         var itemCount = button.transform.Find("ItemAmount").GetComponent<TMP_Text>();

    //         itemName.text = ownedItem.itemData.name_item;
    //         itemCount.text = $"장비 1개";

    //         // 장비는 개별 장착 또는 처리 방식에 맞춰 로직 구성
    //         var currentItem = ownedItem.itemData;
    //         button.onClick.AddListener(() =>
    //         {
    //             Debug.Log($"장비 선택됨: {currentItem.name_item}");
    //             // 장비 처리 로직 추가 가능
    //         });
    //     }
    // }

    public void RefreshItemList()
    {
        foreach (Transform child in itemListPanel)
        {
            Destroy(child.gameObject);
        }

        if (toggleConsume.isOn)
        {
            PrintConsumeItem();
        }
        else if (toggleEquip.isOn)
        {
            //PrintEquipItems();
        }
    }
}
