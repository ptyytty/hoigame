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

    [Header("Consume Item")]
    [SerializeField] protected Sprite consumeDefaultSprite;

    [Header("Toggles")]
    [SerializeField] private Toggle toggleConsume;
    [SerializeField] private Toggle toggleEquip;
    [SerializeField] private Sprite selectedImage;
    [SerializeField] private Sprite unselectedImage;

    [Header("Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory;  // 던전 준비용 6칸 인벤토리

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

        if (!dungeonInventory)
            dungeonInventory = GetComponent<DungeonInventory>();
    }

    void Start()
    {
        toggleConsume.onValueChanged.AddListener(OnToggleChanged);
        toggleEquip.onValueChanged.AddListener(OnToggleChanged);

        toggleConsume.isOn = true;
        RefreshItemList();
    }

    /// <summary>
    /// 역할: 패널이 활성화될 때 인벤토리/이벤트를 준비하고 아이템 리스트 갱신
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();

        //  - InventoryRuntime / DungeonInventory가 준비될 때까지 기다렸다가
        //    이벤트 구독 + 리스트 갱신을 한 번에 처리한다.
        StartCoroutine(EnsureInventoryAndDungeonReady());
    }

    /// <summary>
    /// 역할: InventoryRuntime / DungeonInventory 준비 후
    ///       이벤트 구독 + 아이템 리스트 갱신을 안전하게 수행
    ///       (빌드에서 실행 순서 차이 문제 방지)
    /// </summary>
    private IEnumerator EnsureInventoryAndDungeonReady()
    {
        while (InventoryRuntime.Instance == null)
            yield return null;

        if (!dungeonInventory)
        {
            // 혹시 Awake 이전에 호출되었으면 한 번 더 시도
            dungeonInventory = GetComponent<DungeonInventory>();
        }

        if (!dungeonInventory)
        {
            Debug.LogError("[ItemList] DungeonInventory를 찾지 못했습니다. 같은 오브젝트에 컴포넌트를 붙여주세요. (던전 준비 씬)");
            yield break;
        }

        PlayerProgressService.InventoryApplied -= RefreshItemList;
        PlayerProgressService.InventoryApplied += RefreshItemList;

        dungeonInventory.Changed -= RefreshItemList;
        dungeonInventory.Changed += RefreshItemList;

        RefreshItemList();
    }

    /// <summary>
    /// 역할: 패널이 비활성화될 때 이벤트 구독 해제
    /// </summary>
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

    /// <summary>
    /// 역할: 현재 탭 상태에 맞게 아이템 리스트를 다시 구성
    /// </summary>
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

    /// <summary>
    /// 역할: 소비 아이템 탭일 때 보유 소비 아이템 목록을 출력하고
    ///       버튼 클릭 시 던전 준비용 인벤토리에 아이템을 추가
    /// </summary>
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
            Image bgImage = itemButton.gameObject.GetComponent<Image>();
            Image itemIcon = itemButton.transform.Find("ItemImage").GetComponent<Image>();

            itemName.text = ownedItem.itemData.name_item;
            itemAmount.text = "수량: " + ownedItem.count.ToString();
            bgImage.sprite = consumeDefaultSprite;
            itemIcon.sprite = ownedItem.itemData.icon;

            var currentItem = ownedItem.itemData;

            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() =>
            {
                if (dungeonInventory == null || currentItem == null) return;

                Debug.Log($"[DungeonInventory] TryAdd consume item: {currentItem.name_item}");

                // 🔍 AddItem의 반환값을 바로 로그로 확인
                bool added = dungeonInventory.AddItem(currentItem);
                Debug.Log($"[DungeonInventory] AddItem 결과 = {added}");

                if (!added)
                {
                    Debug.LogWarning("[DungeonInventory] AddItem 실패 - 슬롯이 가득 찼거나, 슬롯 상태 이상");
                    return;
                }

                // 성공 시 보유 인벤토리에서 1개 감소
                inv.AddConsumeItem(currentItem, -1);
                RefreshItemList();
            });
        }
    }

    /// <summary>
    /// 역할: 현재 토글 상태에 맞게 리스트를 비우고 다시 리빌드
    /// </summary>
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
