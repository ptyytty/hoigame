using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemList : MonoBehaviour
{
    public static ItemList instance;

    public delegate void EquipItemHandler(EquipItem item);
    public event EquipItemHandler OnEquipItemSelect;

    [SerializeField] private HeroButtonObject.ChangedImage changedImage;

    [Header("Toggles")]
    [SerializeField] private Toggle toggleConsume;
    [SerializeField] private Toggle toggleEquip;
    [SerializeField] private Sprite selectedImage;
    [SerializeField] private Sprite unselectedImage;

    [Header("List")]
    [SerializeField] private Button itemButtonPrefab;
    [SerializeField] private Transform itemListPanel;

    [Header("Inventory")]
    [SerializeField] private DungeonInventory dungeonInventory;
    [Header("Panel")]
    [SerializeField] private GameObject itemList;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private PartySelector partySelector;

    private Button currentSelect;

    private List<Button> equipItemButtons = new();
    private List<EquipItem> equipItemDatas = new();

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

    void OnEnable()
    {
        var clickHandler = FindObjectOfType<UIClickResetHandler>();
        if (clickHandler != null)
            clickHandler.RegisterResetCallback(ResetItemButton);
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

    void PrintEquipItems()
    {
        var items = PlayerItemManager.instance.ownedEquipItem;

        foreach (var ownedItem in items)
        {
            Button button = Instantiate(itemButtonPrefab, itemListPanel);
            Image buttonImage = button.GetComponent<Image>();
            TMP_Text itemName = button.transform.Find("ItemName").GetComponent<TMP_Text>();
            TMP_Text itemCount = button.transform.Find("ItemAmount").GetComponent<TMP_Text>();

            itemName.text = ownedItem.itemData.name_item;
            itemCount.text = ownedItem.count.ToString();

            Button capturedButton = button; //영웅 버튼
            Image capturedImage = buttonImage;

            // 장비는 개별 장착 또는 처리 방식에 맞춰 로직 구성
            var currentItem = ownedItem.itemData;

            equipItemButtons.Add(button);
            equipItemDatas.Add(currentItem);

            button.onClick.AddListener(() =>
            {
                // 이미 선택된 버튼이면 무시
                if (currentSelect == capturedButton)
                    return;

                // 기존 선택된 버튼이 있으면 이미지 복원
                if (currentSelect != null)
                {
                    ResetItemButton();
                }

                Debug.Log($"장비 선택됨: {currentItem.name_item}");
                capturedImage.sprite = changedImage.selectedImage;
                currentSelect = capturedButton;
                OnEquipItemSelect?.Invoke(currentItem);
            });
        }
    }

    public void RefreshItemList()
    {
        equipItemButtons.Clear();
        equipItemDatas.Clear();

        foreach (Transform child in itemListPanel)
        {
            Destroy(child.gameObject);
        }

        if (toggleConsume.isOn)
        {
            var toggleEquipImage = toggleEquip.GetComponentInChildren<Image>();
            var toggleConsumeImage = toggleConsume.GetComponentInChildren<Image>();

            toggleConsumeImage.sprite = selectedImage;
            toggleEquipImage.sprite = unselectedImage;

            PrintConsumeItem();
        }
        else if (toggleEquip.isOn)
        {
            var toggleEquipImage = toggleEquip.GetComponentInChildren<Image>();
            var toggleConsumeImage = toggleConsume.GetComponentInChildren<Image>();

            toggleConsumeImage.sprite = unselectedImage;
            toggleEquipImage.sprite = selectedImage;

            PrintEquipItems();
        }
    }

    public void SetEquipItemButtonInteractableByJob(JobCategory category)
    {
        for (int i = 0; i < equipItemButtons.Count; i++)
        {
            if (equipItemButtons[i] == null) continue;
            
            bool canEquip = equipItemDatas[i].jobCategory == category;
            equipItemButtons[i].interactable = canEquip;
        }
    }

    public void SetAllEquipButtonsInteractable(bool state)
    {
        foreach (var btn in equipItemButtons)
        {
            if (btn != null)
                btn.interactable = state;
        }
    }

    public void ResetItemButton()
    {
        if (currentSelect == null) return;

        Image prevImage = currentSelect.GetComponent<Image>();
        prevImage.sprite = changedImage.defaultImage;
        currentSelect = null;
    }
}
