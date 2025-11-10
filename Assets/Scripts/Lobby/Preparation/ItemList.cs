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
        PlayerProgressService.InventoryApplied += RefreshItemList; // 저장/적용 신호
                                                                   // [역할] 던전 준비 인벤토리 변경(칸 추가/제거)도 즉시 반영
        if (!dungeonInventory)
            dungeonInventory = FindObjectOfType<DungeonInventory>(true);
        if (dungeonInventory != null)
            dungeonInventory.Changed += RefreshItemList;

        // [역할] 구독을 마친 직후 목록 1회 강제 갱신(패널 재입장 즉시 최신)
        RefreshItemList();
    }

    protected void OnDisable()
    {
        PlayerProgressService.InventoryApplied -= RefreshItemList;
        if (dungeonInventory != null)
            dungeonInventory.Changed -= RefreshItemList; // [역할] 메모리 누수 방지
    }

    void OnToggleChanged(bool _)
    {
        RefreshItemList();
    }

    protected override void LoadList()
    {
        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        if (toggleConsume.isOn)
        {
            // ✅ 소비 아이템은 소비 전용 빌더로 처리 (타입 불일치 방지)
            PrintConsumeItem();
        }
        else if (toggleEquip.isOn)
        {
            // ✅ 장비만 베이스 빌더 사용 (TData=EquipItem)
            foreach (var owned in inv.ownedEquipItem)
                if (owned != null && owned.itemData != null)
                    CreateButton(owned.itemData);
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
            Destroy(child.gameObject);

        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        // ✅ 스냅샷 + 고정 정렬(id_item 기준)
        var snapshot = new List<OwnedItem<ConsumeItem>>();
        foreach (var owned in inv.GetOwnedConsumeItems())
            if (owned != null && owned.itemData != null && owned.count > 0)
                snapshot.Add(owned);

        snapshot.Sort((a, b) => a.itemData.id_item.CompareTo(b.itemData.id_item));

        // 버튼 생성
        foreach (var ownedItem in snapshot)
        {
            Button itemButton = Instantiate(buttonPrefab, contentParent);
            TMP_Text itemName = itemButton.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text itemAmount = itemButton.transform.Find("ItemAmount").GetComponent<TMP_Text>();
            Image itemIcon = itemButton.transform.Find("ItemImage").GetComponent<Image>();

            itemName.text = ownedItem.itemData.name_item;
            itemAmount.text = "수량: " + ownedItem.count.ToString();
            itemIcon.sprite = ownedItem.itemData.icon;

            var currentItem = ownedItem.itemData;

            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() =>
            {
                if (!dungeonInventory)
                    dungeonInventory = FindObjectOfType<DungeonInventory>(true);
                if (dungeonInventory == null || currentItem == null) return;

                // 1) 던전 준비 인벤토리에 추가
                bool added = dungeonInventory.AddItem(currentItem);
                // 2) 성공 시 보유 인벤토리 -1
                if (added)
                {
                    inv.AddConsumeItem(currentItem, -1);
                    RefreshItemList(); // 즉시 재빌드(정렬 유지)
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
