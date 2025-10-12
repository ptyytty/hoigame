using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemList : ListUIBase<EquipItem>
{
    public static ItemList instance;

    public delegate void EquipItemHandler(EquipItem item);
    public event EquipItemHandler OnEquipItemSelect;

    [Header("Toggles")]
    [SerializeField] private Toggle toggleConsume;
    [SerializeField] private Toggle toggleEquip;
    [SerializeField] private Sprite selectedImage;
    [SerializeField] private Sprite unselectedImage;

    [Header("Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory;
    [Header("Panel")]
    [SerializeField] private GameObject itemList;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private PartySelector partySelector;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }
    }
    void Start()
    {
        toggleConsume.onValueChanged.AddListener(OnToggleChanged);
        toggleEquip.onValueChanged.AddListener(OnToggleChanged);

        toggleConsume.isOn = true;
        RefreshItemList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PlayerProgressService.InventoryApplied += RefreshItemList;
    }

    void OnToggleChanged(bool _)
    {
        RefreshItemList();
        PlayerProgressService.InventoryApplied -= RefreshItemList;
    }

    protected override void LoadList()
    {
        if (toggleConsume.isOn)
            PrintConsumeItem();
        else if (toggleEquip.isOn)
        {
            var inv = InventoryRuntime.Instance;
            if (inv != null)
            {
                foreach (var ownedItem in inv.ownedEquipItem)
                    CreateButton(ownedItem.itemData);
            }
        }
    }

    protected override void SetLabel(Button button, EquipItem data)
    {
        TMP_Text itemName = button.transform.Find("ItemName").GetComponent<TMP_Text>();
        TMP_Text itemAmount = button.transform.Find("ItemAmount").GetComponent<TMP_Text>();
        Image itemIcon = button.transform.Find("ItemImage").GetComponent<Image>();

        itemName.text = data.name_item;
        itemAmount.text = $"수량: ";
        itemIcon.sprite = data.icon;
    }

    protected override void OnSelected(EquipItem item)
    {
        Debug.Log($"장비 선택됨: {item.name_item}");
        OnEquipItemSelect?.Invoke(item);
    }

    void PrintConsumeItem()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        var items = inv.GetOwnedConsumeItems();

        foreach (var ownedItem in items)
        {
            if (ownedItem.count <= 0) continue;
            Button itemButton = Instantiate(buttonPrefab, contentParent);
            TMP_Text itemName = itemButton.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text itemAmount = itemButton.transform.Find("ItemAmount").GetComponent<TMP_Text>();
            Image itemIcon = itemButton.transform.Find("ItemImage").GetComponent<Image>();

            itemName.text = ownedItem.itemData.name_item;
            itemAmount.text = "수량: " + ownedItem.count.ToString();
            itemIcon.sprite = ownedItem.itemData.icon;

            // 클로저 문제 방지: 로컬 변수로 복사
            var currentItem = ownedItem.itemData;

            itemButton.onClick.AddListener(() =>
            {
                if (inv.GetConsumeItemCount(currentItem) > 0)
                {
                    bool success = dungeonInventory.AddItem(currentItem);
                    if (success)
                    {
                        inv.AddConsumeItem(currentItem, -1);
                        RefreshItemList();
                    }
                }
            });
        }
    }

    public void RefreshItemList()
    {
        ClearList();

        var toggleEquipImage = toggleEquip.GetComponentInChildren<Image>();
        var toggleConsumeImage = toggleConsume.GetComponentInChildren<Image>();

        toggleEquipImage.sprite = toggleConsume.isOn ? selectedImage : unselectedImage;
        toggleConsumeImage.sprite = toggleEquip.isOn ? selectedImage : unselectedImage;

        LoadList();
    }

    public void SetEquipItemButtonInteractableByJob(JobCategory category)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            bool canEquip = dataList[i].jobCategory == category;
            buttons[i].interactable = canEquip;
        }
    }

    public void ResetItemListState()
    {
        ResetSelectedButton();
        SetAllButtonsInteractable(true);
    }

    // 이하 PartySelector 호출용 Public 메소드
    public void ResetItemButton()
    {
        ResetSelectedButton();
    }

    public void SetInteractable(bool state)
    {
        SetAllButtonsInteractable(state);
    }
}
