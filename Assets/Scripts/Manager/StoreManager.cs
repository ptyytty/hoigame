using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// 상점 UI 제어
public class StoreManager : MonoBehaviour
{
    [System.Serializable]
    public class ToggleImagepair    // 토글 버튼 정보
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
        public Text labelText;
        public Color selectedTextColor = new Color(238f / 255f, 190f / 255f, 20f / 255f, 1f);
        public Color defaultTextcolor = new Color(1f, 1f, 1f, 1f);
    }

    [Header("Toggle Images")]
    public List<ToggleImagepair> itemTypeToggleImagePairs;            // 아이템 종류 토글
    public List<ToggleImagepair> storeTypeToggleImagePairs;           // 상점 토글
    public List<ToggleImagepair> changeBuyOrSellToggleImagePairs;     // 구매/판매 토글
    public List<ToggleImagepair> selectItemToggleImagePairs;          // 아이템 정보 / 판매 목록 토글

    [Header("Panels")]
    [SerializeField] private GameObject localStore, onlineStore;
    [SerializeField] private GameObject itemToggleGroup;
    [SerializeField] private GameObject onlineBackground;

    [SerializeField] GameObject panelRight;
    [SerializeField] GameObject panelInfo;
    [SerializeField] GameObject panelSearch;

    [SerializeField] GameObject panelInfoToggle; // 온라인 상점 아이템 클릭 시 토글 패널
    [SerializeField] GameObject panelMySalesList;   // 내 판매 목록 패널

    [Header("Toggle Group")]
    [SerializeField] GameObject onlineToggleGroup;

    private Toggle lastSelectedItemType = null;
    private Toggle lastSelectedStoreType = null;
    private Toggle lastSelectedOnlineStoreMode = null;
    private Toggle lastSelectedItemInfo = null;

    enum StoreKind { Local, Online }
    private StoreKind currentStore = StoreKind.Local;

    [Header("Button")]
    [SerializeField] GameObject btnApply;
    [SerializeField] GameObject btnSell;

    [Header("Scripts")]
    [SerializeField] private ItemDisplay onlineItemDisplay;
    [SerializeField] private SellPanel sellPanel;

    void OnEnable()
    {
        InitExclusiveToggles(itemTypeToggleImagePairs, ref lastSelectedItemType);
        InitExclusiveToggles(storeTypeToggleImagePairs, ref lastSelectedStoreType);
        InitExclusiveToggles(changeBuyOrSellToggleImagePairs, ref lastSelectedOnlineStoreMode);
        InitExclusiveToggles(selectItemToggleImagePairs, ref lastSelectedItemInfo);

        UpdateToggle(itemTypeToggleImagePairs);
        UpdateToggle(storeTypeToggleImagePairs);
        UpdateToggle(changeBuyOrSellToggleImagePairs);
        UpdateToggle(selectItemToggleImagePairs);
    }

    void Start()
    {
        Product.OnAnyProductClicked += HandleProductClicked;

        // 아이템 타입 토글 (전체, 장비, 소모)
        for (int i = 0; i < itemTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            itemTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(itemTypeToggleImagePairs[index].toggle, itemTypeToggleImagePairs, ref lastSelectedItemType);
                }
            });
        }

        // 상점 타입 토글 (로컬, 온라인)
        for (int i = 0; i < storeTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            // 클릭할 때, 토글의 isOn이 true가 될 때 전부 호출
            storeTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(storeTypeToggleImagePairs[index].toggle, storeTypeToggleImagePairs, ref lastSelectedStoreType);
                    ShowPannelByIndex(index);
                }
            });
        }

        // 구매/판매 전환 토글
        for (int i = 0; i < changeBuyOrSellToggleImagePairs.Count; i++)
        {
            int index = i;

            changeBuyOrSellToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(changeBuyOrSellToggleImagePairs[index].toggle, changeBuyOrSellToggleImagePairs, ref lastSelectedOnlineStoreMode);

                    // ✅ 구매/판매 토글 연동
                    bool isSell = (index == 1);
                    onlineItemDisplay.isSellMode = isSell;
                    onlineItemDisplay.RefreshItemList();

                    if (currentStore == StoreKind.Online) SetOnlineIdleUI();
                }
            });
        }

        // 아이템 정보 토글
        for (int i = 0; i < selectItemToggleImagePairs.Count; i++)
        {
            int index = i;
            selectItemToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(selectItemToggleImagePairs[index].toggle, selectItemToggleImagePairs, ref lastSelectedItemInfo);
                ShowSelectedItemPanel(index);
            });
        }

        // 기본값 초기화
        if (itemTypeToggleImagePairs.Count > 0)
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
        }

        if (storeTypeToggleImagePairs.Count > 0)
        {
            storeTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;
        }

        if (changeBuyOrSellToggleImagePairs.Count > 0)
        {
            changeBuyOrSellToggleImagePairs[0].toggle.isOn = true;
            lastSelectedOnlineStoreMode = changeBuyOrSellToggleImagePairs[0].toggle;
        }
        if (selectItemToggleImagePairs.Count > 0)
        {
            selectItemToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemInfo = selectItemToggleImagePairs[0].toggle;
        }

        UpdateToggle(itemTypeToggleImagePairs);
        UpdateToggle(storeTypeToggleImagePairs);
        UpdateToggle(changeBuyOrSellToggleImagePairs);
        UpdateToggle(selectItemToggleImagePairs);

        ShowSelectedItemPanel(0);

        // 기본은 비활성화
        btnApply.GetComponent<Button>().onClick.AddListener(OnClickApply);
        btnSell.GetComponent<Button>().onClick.AddListener(OnClickSell);

        // ✅ 항상 싱글턴 기준으로 inventory 보정
        var inv = InventoryRuntime.Instance;
        if (inv != null)
        {
            InventoryRuntime.Instance.OnCurrencyChanged += UpdateApplyButtonState; // [역할] 재화 변동 시 버튼 재평가
        }

        UpdateApplyButtonState();
    }

    private void OnDestroy()
    {
        var inv = InventoryRuntime.Instance;
        if (inv != null) inv.OnCurrencyChanged -= UpdateApplyButtonState;

        Product.OnAnyProductClicked -= HandleProductClicked;
    }

    // 상점 타입 토글 전환에 따른 패널 변경
    public void ShowPannelByIndex(int index)
    {
        bool islocal = index == 0;
        bool isonline = index == 1;

        localStore.SetActive(islocal);
        onlineToggleGroup.SetActive(isonline);
        onlineStore.SetActive(isonline);
        onlineBackground.SetActive(isonline);
        itemToggleGroup.SetActive(isonline);

        currentStore = islocal ? StoreKind.Local : StoreKind.Online;

        // 기존 선택 초기화
        if (Product.CurrentSelected != null) Product.CurrentSelected.ResetToDefaultImage();
        ItemInfoPanel.instance?.Hide();

        // ✅ 오른쪽 패널 preset
        if (islocal) SetLocalIdleUI();
        else SetOnlineIdleUI();

        UpdateApplyButtonState();
    }

    /// <summary>
    /// [역할] 우측 패널 하위 탭(0: 아이템 정보, 1: 내 판매 목록)에 따라 콘텐츠 패널 토글
    /// </summary>
    private void ShowSelectedItemPanel(int index)
    {
        bool showInfo = (index == 0);
        bool showMyList = (index == 1);

        // 아이템 정보 탭
        if (panelInfo) panelInfo.SetActive(showInfo);
        // 내 판매 목록 탭
        if (panelMySalesList) panelMySalesList.SetActive(showMyList);

        // 판매 목록 탭 활성화 시, 최신 데이터로 갱신
        if (showMyList && panelMySalesList != null)
        {
            // TODO: 최신 데이터 갱신 함수
        }
    }

    // 토글 전환
    void OnToggleChanged(Toggle changedToggle, List<ToggleImagepair> toggleGroup, ref Toggle lastSelectedToggle)    //ref: lastSelectedToggle 참조 호출
    {
        if (changedToggle.isOn)
        {
            if (changedToggle != lastSelectedToggle)
            {
                lastSelectedToggle = changedToggle;
                UpdateToggle(toggleGroup);
            }
            else
            {
                changedToggle.isOn = true;
            }
        }
    }

    // 토글 버튼 이미지 변경
    void UpdateToggle(List<ToggleImagepair> toggleGroup)
    {
        foreach (var pair in toggleGroup)
        {
            bool isOn = pair.toggle.isOn;

            pair.image.sprite = pair.toggle.isOn ? pair.selectedSprite : pair.defaultSprite;

            if (pair.labelText != null) // labelText 여부에 따른 텍스트 색상 변경
            {
                Color targetColor = isOn ? pair.selectedTextColor : pair.defaultTextcolor;
                targetColor.a = 1f;
                pair.labelText.color = targetColor;
            }
        }
    }

    /// <summary>
    /// Apply 버튼 클릭 시 선택된 상품을 구매 확정
    /// </summary>
    private void OnClickApply()
    {
        var selected = Product.CurrentSelected;
        if (selected == null)
        {
            btnApply.GetComponent<Button>().interactable = false;
            Debug.Log("[Store] 구매할 상품이 선택되지 않았습니다.");
            return;
        }

        var inv = InventoryRuntime.Instance;
        if (inv == null)
        {
            Debug.LogError("[Store] InventoryRuntime 인스턴스를 찾을 수 없습니다!");
            return;
        }

        int price = selected.Price;

        // ⚠️ 최종 가드: TrySpendGold가 false면 절대 진행하지 않음
        if (!inv.TrySpendGold(price))
        {
            Debug.Log("[Store] 골드 부족으로 구매 불가.");
            UpdateApplyButtonState(); // 남은 골드 기준으로 즉시 버튼 상태 반영
            return;
        }

        // 🛒 아이템 지급
        if (selected.IsConsume && selected.BoundConsume != null)
        {
            inv.AddConsumeItem(selected.BoundConsume, 1);
            Debug.Log($"[Store] {selected.BoundConsume.name_item}을(를) 1개 구매했습니다.");

            // 소비 아이템은 보유량 갱신된 정보 다시 표시
            ItemInfoPanel.instance.ShowItemInfo(
                selected.BoundConsume.name_item,
                selected.BoundConsume.description,
                selected.Price,
                selected.BoundConsume.icon,
                selected.BoundConsume.effects
            );
        }
        else if (selected.IsEquip && selected.BoundEquip != null)
        {
            inv.AddEquipItem(selected.BoundEquip);
            Debug.Log($"[Store] {selected.BoundEquip.name_item} 장비를 구매했습니다.");

            // 장비는 한 번만 구매 가능 → 버튼 비활성화 & 초기화
            var btn = selected.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            selected.ResetToDefaultImage();
            ItemInfoPanel.instance.Hide();

            // 선택 상태 해제
            typeof(Product)
                .GetField("currentSelectedProduct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);
        }
        else
        {
            Debug.LogWarning("[Store] 선택된 상품이 유효하지 않습니다.");
            return;
        }

        // 저장
        if (PlayerProgressService.Instance != null)
            _ = PlayerProgressService.Instance.SaveAsync();

        // ⭐ 구매 후 버튼 상태 재평가(골드 변동 반영)
        UpdateApplyButtonState();
    }

    // 온라인 상점 아이템 판매
    private void OnClickSell()
    {
        var selected = Product.CurrentSelected;
        if (selected == null)
        {
            Debug.Log("[Store] 판매할 아이템이 선택되지 않았습니다.");
            return;
        }

        var inv = InventoryRuntime.Instance;
        int count = 1;

        if (selected.IsConsume && selected.BoundConsume != null)
        {
            // 🔹 Find 대신 직접 반복문으로 탐색
            foreach (var owned in inv.GetOwnedConsumeItems())
            {
                if (owned.itemData == selected.BoundConsume)
                {
                    count = owned.count;
                    break;
                }
            }
        }

        sellPanel.Show(selected, count);
    }

    public void UpdateApplyButtonState()
    {
        var inv = InventoryRuntime.Instance;
        if (btnApply == null || inv == null) return;

        var selected = Product.CurrentSelected;

        if (selected == null)
        {
            btnApply.GetComponent<Button>().interactable = false;
            return;
        }

        int price = selected.Price;
        btnApply.GetComponent<Button>().interactable = (inv.Gold >= price);
    }

    // 상품 클릭 이벤트
    private void HandleProductClicked(Product p)
    {
        if (currentStore == StoreKind.Local)
        {
            // 로컬: Info + Apply 활성화
            SetLocalSelectedUI();

            // (로컬에서만) 정보 패널에 내용 채움
            if (p.IsConsume)
                ItemInfoPanel.instance.ShowItemInfo(p.BoundConsume.name_item, p.BoundConsume.description, p.Price, p.BoundConsume.icon, p.BoundConsume.effects);
            else if (p.IsEquip)
                ItemInfoPanel.instance.ShowItemInfo(p.BoundEquip.name_item, p.BoundEquip.description, p.Price, p.BoundEquip.icon, p.BoundEquip.effects);

            UpdateApplyButtonState();
        }
        else
        {
            SetOnlineSelectedUI();
        }
    }

    // ============ 패널 프리셋 =================

    // 토글 초기화
    private void InitExclusiveToggles(List<ToggleImagepair> pairs, ref Toggle currentTab)
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            var t = pairs[i].toggle;
            bool on = (i == 0);
            t.isOn = on;
            if (on) currentTab = t;
        }
    }

    private void SetLocalIdleUI()
    {
        panelRight.SetActive(false); // 클릭 전엔 안 보이게

        InitExclusiveToggles(changeBuyOrSellToggleImagePairs, ref lastSelectedOnlineStoreMode);
        UpdateToggle(changeBuyOrSellToggleImagePairs);

    }

    // [역할] 로컬 상점: 상품 클릭 후
    private void SetLocalSelectedUI()
    {
        panelRight.SetActive(true);
        panelInfo.SetActive(true);
        btnApply.SetActive(true);
        btnSell.SetActive(false);
        panelSearch.SetActive(false);
        panelMySalesList.SetActive(false);
        if (panelInfoToggle) panelInfoToggle.SetActive(false);
        if (panelMySalesList) panelMySalesList.SetActive(false);
    }

    // [역할] 온라인 상점: 탭 진입 시
    private void SetOnlineIdleUI()
    {
        panelRight.SetActive(true);
        panelSearch.SetActive(true);
        panelInfo.SetActive(false);
        btnApply.SetActive(false);
        btnSell.SetActive(false);
        panelMySalesList.SetActive(false);
        if (panelInfoToggle) panelInfoToggle.SetActive(false);
        if (panelMySalesList) panelMySalesList.SetActive(false);

        // 토글 초기화
        InitExclusiveToggles(itemTypeToggleImagePairs, ref lastSelectedItemType);
        UpdateToggle(itemTypeToggleImagePairs);

        InitExclusiveToggles(selectItemToggleImagePairs, ref lastSelectedItemInfo);
        UpdateToggle(selectItemToggleImagePairs);
    }

    // [역할] 온라인 상점: 상품 클릭 후
    private void SetOnlineSelectedUI()
    {
        panelRight.SetActive(true);
        panelSearch.SetActive(true);
        panelInfo.SetActive(true);
        btnApply.SetActive(false);
        btnSell.SetActive(true);
        panelMySalesList.SetActive(false);
        if (panelInfoToggle) panelInfoToggle.SetActive(true);
        if (panelMySalesList) panelMySalesList.SetActive(false);

        InitExclusiveToggles(selectItemToggleImagePairs, ref lastSelectedItemInfo);
        UpdateToggle(selectItemToggleImagePairs);
    }
}
