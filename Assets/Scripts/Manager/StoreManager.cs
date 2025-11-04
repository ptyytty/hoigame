using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Firebase.Auth;
using Firebase.Firestore;



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
        public Text labelText; // 기본 Text 사용 (기존 구조 유지)
        public Color selectedTextColor = new Color(238f / 255f, 190f / 255f, 20f / 255f, 1f);
        public Color defaultTextcolor = new Color(1f, 1f, 1f, 1f);
    }

    [Header("Toggle Images")]
    public List<ToggleImagepair> itemTypeToggleImagePairs;            // 아이템 종류 토글(0:전체,1:소비,2:장비)
    public List<ToggleImagepair> storeTypeToggleImagePairs;           // 상점 토글(로컬/온라인)
    public List<ToggleImagepair> changeBuyOrSellToggleImagePairs;     // 구매/판매 토글
    public List<ToggleImagepair> selectItemToggleImagePairs;          // 아이템 정보 / 내 판매 목록 토글

    [Header("Panels")]
    [SerializeField] private GameObject localStore, onlineStore;
    [SerializeField] private GameObject itemToggleGroup;
    [SerializeField] private GameObject onlineBackground;

    [SerializeField] GameObject panelRight;
    [SerializeField] GameObject panelInfo;
    [SerializeField] GameObject panelSearch;

    [SerializeField] GameObject panelInfoToggle;   // 온라인 상점 아이템 클릭 시 토글 패널
    [SerializeField] GameObject panelMySalesList;  // 내 판매 목록 패널

    [Header("Toggle Group")]
    [SerializeField] GameObject onlineToggleGroup;

    private Toggle lastSelectedItemType = null;
    private Toggle lastSelectedStoreType = null;
    private Toggle lastSelectedOnlineStoreMode = null;
    private Toggle lastSelectedItemInfo = null;

    enum StoreKind { Local, Online }
    private StoreKind currentStore = StoreKind.Local;

    [Header("Button")]
    [SerializeField] GameObject btnApply;  // 구매 버튼 (재사용)
    [SerializeField] GameObject btnSell;   // 판매 버튼

    [Header("Scripts")]
    [SerializeField] private ItemDisplay onlineItemDisplay;
    [SerializeField] private SellPanel sellPanel;
    [SerializeField] private SortedDropdown sortedDropdown; // 정렬 드롭다운 참조

    // ─────────────────────────────────────────────────────────────
    // 온라인 구매 모드에서 선택 슬롯의 “표시 가격” 캐시
    // (Product.Price가 카탈로그 가격일 수 있어 슬롯 UI의 Txt_Price를 신뢰)
    int lastSelectedPrice = 0;
    // ─────────────────────────────────────────────────────────────

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

        // 아이템 종류 토글 (0:전체,1:소비,2:장비)
        for (int i = 0; i < itemTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            itemTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(itemTypeToggleImagePairs[index].toggle, itemTypeToggleImagePairs, ref lastSelectedItemType);

                    // [역할] 아이템 타입 필터 적용
                    if (onlineItemDisplay != null)
                    {
                        ItemDisplay.ItemTypeFilter f = ItemDisplay.ItemTypeFilter.All;
                        if (index == 1) f = ItemDisplay.ItemTypeFilter.Consume;
                        else if (index == 2) f = ItemDisplay.ItemTypeFilter.Equipment;

                        onlineItemDisplay.SetTypeFilter(f);
                        onlineItemDisplay.RefreshItemList();
                    }
                }
            });
        }

        // 상점 타입 토글 (로컬, 온라인)
        for (int i = 0; i < storeTypeToggleImagePairs.Count; i++)
        {
            int index = i;

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

                    bool isSell = (index == 1);
                    if (onlineItemDisplay != null)
                    {
                        onlineItemDisplay.isSellMode = isSell;
                        onlineItemDisplay.RefreshItemList();
                    }

                    if (currentStore == StoreKind.Online) SetOnlineIdleUI();

                    // 판매 탭 진입 시: 우측 내 판매 목록 탭이 기본
                    if (isSell)
                    {
                        ClearSelectionAndInfo();

                        SelectTab(selectItemToggleImagePairs, 1, ref lastSelectedItemInfo, ShowSelectedItemPanel);
                        UpdateToggle(selectItemToggleImagePairs);

                        if (panelRight) panelRight.SetActive(true);
                        if (panelMySalesList) panelMySalesList.SetActive(true);
                        if (panelSearch) panelSearch.SetActive(true);
                        if (panelInfoToggle) panelInfoToggle.SetActive(true);
                        if (panelInfo) panelInfo.SetActive(false);

                        if (sellPanel != null) sellPanel.RequestRefreshMySales();

                    }
                    else
                    {
                        ClearSelectionAndInfo();

                        // 구매 탭 진입 시: 정보 탭이 기본, 구매 버튼 초기화
                        SelectTab(selectItemToggleImagePairs, 0, ref lastSelectedItemInfo, ShowSelectedItemPanel);
                        UpdateToggle(selectItemToggleImagePairs);
                    }
                }
            });
        }

        // 정렬 드롭다운 변경 구독 (최신순/가격순 + 재클릭 역순)
        if (sortedDropdown != null)
        {
            sortedDropdown.OnSortChanged += (opt, asc) =>
            {
                if (onlineItemDisplay != null)
                {
                    onlineItemDisplay.SetSort(opt, asc);
                    onlineItemDisplay.RefreshItemList();
                }
            };
        }

        // 아이템 정보/내 판매 목록 토글
        for (int i = 0; i < selectItemToggleImagePairs.Count; i++)
        {
            int index = i;
            selectItemToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(selectItemToggleImagePairs[index].toggle, selectItemToggleImagePairs, ref lastSelectedItemInfo);

                if (index == 0) // 아이템 정보 탭
                {
                    // 정보 탭인데 '선택된 상품 없음'이면 내용물은 감춤
                    if (Product.CurrentSelected == null) HidePanelInfoChildren();
                }
                else // 내 판매 목록 탭
                {
                    ClearSelectionAndInfo();
                }

                ShowSelectedItemPanel(index);

            });
        }

        // 기본값 초기화
        if (itemTypeToggleImagePairs.Count > 0)
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true; // 전체
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
        }

        if (storeTypeToggleImagePairs.Count > 0)
        {
            storeTypeToggleImagePairs[0].toggle.isOn = true; // 로컬
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;
        }

        if (changeBuyOrSellToggleImagePairs.Count > 0)
        {
            changeBuyOrSellToggleImagePairs[0].toggle.isOn = true; // 구매
            lastSelectedOnlineStoreMode = changeBuyOrSellToggleImagePairs[0].toggle;
        }

        if (selectItemToggleImagePairs.Count > 0)
        {
            selectItemToggleImagePairs[0].toggle.isOn = true; // 아이템 정보
            lastSelectedItemInfo = selectItemToggleImagePairs[0].toggle;
        }

        UpdateToggle(itemTypeToggleImagePairs);
        UpdateToggle(storeTypeToggleImagePairs);
        UpdateToggle(changeBuyOrSellToggleImagePairs);
        UpdateToggle(selectItemToggleImagePairs);

        ShowSelectedItemPanel(0);
        HidePanelInfoChildren();

        // 버튼
        btnApply.GetComponent<Button>().onClick.AddListener(async () => await OnClickApply()); // 🔸 async로 래핑
        btnSell.GetComponent<Button>().onClick.AddListener(OnClickSell);

        var inv = InventoryRuntime.Instance;
        if (inv != null)
            InventoryRuntime.Instance.OnCurrencyChanged += UpdateApplyButtonState;

        UpdateApplyButtonState();
    }

    private void OnDestroy()
    {
        var inv = InventoryRuntime.Instance;
        if (inv != null) inv.OnCurrencyChanged -= UpdateApplyButtonState;

        Product.OnAnyProductClicked -= HandleProductClicked;

        if (sortedDropdown != null)
            sortedDropdown.OnSortChanged = null;
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
        HidePanelInfoChildren();

        // 오른쪽 패널 preset
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

        if (panelInfo) panelInfo.SetActive(showInfo);
        if (panelMySalesList) panelMySalesList.SetActive(showMyList);

        if (showMyList && panelMySalesList != null && sellPanel != null)
            sellPanel.RequestRefreshMySales();

    }

    // 토글 전환
    void OnToggleChanged(Toggle changedToggle, List<ToggleImagepair> toggleGroup, ref Toggle lastSelectedToggle)
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

    // 토글 버튼 이미지/텍스트 상태 업데이트
    void UpdateToggle(List<ToggleImagepair> toggleGroup)
    {
        if (toggleGroup == null) return;

        foreach (var pair in toggleGroup)
        {
            if (pair == null || pair.toggle == null) continue;

            bool isOn = pair.toggle.isOn;
            if (pair.image != null)
                pair.image.sprite = isOn ? pair.selectedSprite : pair.defaultSprite;

            if (pair.labelText != null)
            {
                Color targetColor = isOn ? pair.selectedTextColor : pair.defaultTextcolor;
                targetColor.a = 1f;
                pair.labelText.color = targetColor;
            }
        }
    }

    /// <summary>
    /// [역할] Apply(구매) 버튼 클릭 처리
    ///  - 온라인 구매 모드: 트랜잭션으로 수량 감소 → 결제/지급 → 새로고침
    ///  - 로컬 상점: 기존 로직
    /// </summary>
    private async System.Threading.Tasks.Task OnClickApply()
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

        bool isOnlineBuyMode = (currentStore == StoreKind.Online && onlineItemDisplay != null && !onlineItemDisplay.isSellMode);
        int uiPrice = ReadDisplayedPriceFromSlot(selected != null ? selected.gameObject : null);
        int price = isOnlineBuyMode && uiPrice > 0 ? uiPrice : selected.Price;

        if (isOnlineBuyMode)
        {
            await BuyOnlineAsync(selected, price); // 🔹 온라인 구매 처리
            return;
        }

        // ───────── 로컬 상점 구매 (기존 로직) ─────────
        if (!inv.TrySpendGold(price))
        {
            Debug.Log("[Store] 골드 부족으로 구매 불가.");
            UpdateApplyButtonState();
            return;
        }

        if (selected.IsConsume && selected.BoundConsume != null)
        {
            inv.AddConsumeItem(selected.BoundConsume, 1);

            ItemInfoPanel.instance.ShowItemInfo(
                selected.BoundConsume.name_item,
                selected.BoundConsume.description,
                price,
                selected.BoundConsume.icon,
                selected.BoundConsume.effects
            );
        }
        else if (selected.IsEquip && selected.BoundEquip != null)
        {
            inv.AddEquipItem(selected.BoundEquip);

            var btn = selected.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            selected.ResetToDefaultImage();
            ItemInfoPanel.instance.Hide();

            // 선택 해제
            typeof(Product)
                .GetField("currentSelectedProduct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);
        }
        else
        {
            Debug.LogWarning("[Store] 선택된 상품이 유효하지 않습니다.");
            return;
        }

        if (PlayerProgressService.Instance != null)
            _ = PlayerProgressService.Instance.SaveAsync();

        UpdateApplyButtonState();
    }

    /// <summary>
    /// [역할] 온라인 구매 트랜잭션
    ///  1) listingId 문서를 읽어 isActive/수량/가격 검증
    ///  2) 수량 1 감소(0이면 isActive=false)
    ///  3) 성공 시 내 골드 차감 + 인벤토리에 지급
    ///  4) UI/리스트 새로고침
    /// </summary>
    private async System.Threading.Tasks.Task BuyOnlineAsync(Product selected, int price)
    {

        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        if (inv.Gold < price)
        {
            UpdateApplyButtonState();
            return;
        }

        string listingId = selected.GetListingId();
        if (string.IsNullOrEmpty(listingId)) return;

        var db = FirebaseFirestore.DefaultInstance;
        var docRef = db.Collection("marketListings").Document(listingId);

        int newQtyServer = -1;           // 트랜잭션 결과 qty
        bool deletedOnServer = false;    // 트랜잭션에서 삭제했는지 여부

        try
        {
            await db.RunTransactionAsync(async tr =>
            {
                var snap = await tr.GetSnapshotAsync(docRef);
                if (!snap.Exists) throw new System.Exception("삭제되었거나 존재하지 않음");

                // 유효성
                bool isActive = snap.TryGetValue<bool>("isActive", out var _isActive) ? _isActive : true;
                if (!isActive) throw new System.Exception("비활성 상품");

                // qty/quantity 지원
                int qty = 0;
                bool useQuantity = false;
                if (snap.ContainsField("quantity") && snap.TryGetValue<int>("quantity", out var q1)) { qty = q1; useQuantity = true; }
                else if (snap.ContainsField("qty") && snap.TryGetValue<int>("qty", out var q2)) { qty = q2; useQuantity = false; }
                else qty = 1;

                if (qty <= 0) throw new System.Exception("품절");

                // 차감
                int newQty = Mathf.Max(0, qty - 1);
                newQtyServer = newQty;

                if (newQty == 0)
                {
                    // ✅ 수량 0이면 문서 자체를 삭제
                    tr.Delete(docRef);
                    deletedOnServer = true;
                }
                else
                {
                    // ✅ 남아있으면 수량만 업데이트
                    var updates = new Dictionary<string, object>
                    {
                        ["updatedAt"] = FieldValue.ServerTimestamp
                    };
                    if (useQuantity) updates["quantity"] = newQty;
                    else updates["qty"] = newQty;

                    tr.Update(docRef, updates);
                }
            });

            // (이하 결제/지급 + UI 갱신은 그대로)
            if (!inv.TrySpendGold(price))
            {
                UpdateApplyButtonState();
                return;
            }

            if (selected.IsConsume && selected.BoundConsume != null)
                inv.AddConsumeItem(selected.BoundConsume, 1);
            else if (selected.IsEquip && selected.BoundEquip != null)
                inv.AddEquipItem(selected.BoundEquip);

            if (PlayerProgressService.Instance != null)
                _ = PlayerProgressService.Instance.SaveAsync();

            // 슬롯 즉시 반영
            int remaining = selected.DecreaseOnlineQty(1);

            // 서버가 삭제했다면 리스트에서도 제거
            if (deletedOnServer || newQtyServer == 0)
            {
                typeof(Product)
                    .GetField("currentSelectedProduct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    ?.SetValue(null, null);

                SetApplyButtonVisible(false);
                var slotBtn = selected.GetComponent<UnityEngine.UI.Button>();
                if (slotBtn) slotBtn.interactable = false;
                Destroy(selected.gameObject); // UI에서 제거
            }
            else
            {
                // 남아있으면 계속 구매 가능
                SetApplyButtonLabel(price);
                UpdateApplyButtonState();
            }

            inv.NotifyChanged();
            UpdateApplyButtonState();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Store][Online] 구매 실패: {ex.Message}");
            UpdateApplyButtonState();
        }
    }

    /// <summary> [역할] Sell(판매) 버튼 클릭 처리 — 기존 로직 유지 </summary>
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

    /// <summary>
    /// [역할] Apply 버튼의 interactable을 현재 선택/골드로 갱신
    ///  - 온라인 구매 모드일 때는 lastSelectedPrice 우선 사용
    /// </summary>
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

        bool isOnlineBuyMode = (currentStore == StoreKind.Online && onlineItemDisplay != null && !onlineItemDisplay.isSellMode);
        int price = isOnlineBuyMode && lastSelectedPrice > 0 ? lastSelectedPrice : selected.Price;

        btnApply.GetComponent<Button>().interactable = (inv.Gold >= price);
    }

    /// <summary>
    /// [역할] 상품 클릭 시 UI 전환
    ///  - 로컬: 기존 구매 플로우
    ///  - 온라인: 구매/판매 모드에 따라 프리셋 분기
    /// </summary>
    private void HandleProductClicked(Product p)
    {
        if (currentStore == StoreKind.Local)
        {
            SetLocalSelectedUI();

            ShowPanelInfoChildren();

            if (p.IsConsume)
                ItemInfoPanel.instance.ShowItemInfo(p.BoundConsume.name_item, p.BoundConsume.description, p.Price, p.BoundConsume.icon, p.BoundConsume.effects);
            else if (p.IsEquip)
                ItemInfoPanel.instance.ShowItemInfo(p.BoundEquip.name_item, p.BoundEquip.description, p.Price, p.BoundEquip.icon, p.BoundEquip.effects);

            UpdateApplyButtonState();
        }
        else
        {
            bool isSellMode = (onlineItemDisplay != null && onlineItemDisplay.isSellMode);
            if (isSellMode)
            {
                // 1) 정보 탭으로 전환 (선택 해제 로직을 피하기 위해 먼저 탭 전환)
                SelectTab(selectItemToggleImagePairs, 0, ref lastSelectedItemInfo, ShowSelectedItemPanel);
                UpdateToggle(selectItemToggleImagePairs);

                // 2) ★ 선택 보강: 첫 클릭에도 확실히 선택/하이라이트 적용
                p.ForceSelectAsCurrent();                 // ← 추가 핵심

                // 3) 우측 패널 프리셋
                SetOnlineSelectedUI_Sell();
                ShowPanelInfoChildren();

                // 4) 판매 버튼 보이기/활성
                if (btnSell)
                {
                    btnSell.SetActive(true);
                    var sellBtn = btnSell.GetComponent<UnityEngine.UI.Button>();
                    if (sellBtn) sellBtn.interactable = true;
                }

                StartCoroutine(FocusSellButtonNextFrame());
            }
            else
            {
                SetOnlineSelectedUI_Buy(p);
            }
        }
    }

    /// <summary>
    /// [역할] 탭 전환 직후 첫 클릭이 '선택'으로 소모되지 않도록,
    /// 다음 프레임에 EventSystem 포커스를 판매 버튼으로 강제 이동
    /// </summary>
    private IEnumerator FocusSellButtonNextFrame()
    {
        yield return null;                 // 한 프레임 대기 (레이아웃/그래픽 갱신 보장)
        Canvas.ForceUpdateCanvases();      // 레이아웃 강제 반영 (모바일 빌드 안정성)
        if (btnSell && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(btnSell);
    }


    #region Mail helpers

    /// <summary>
    /// [역할] 판매자에게 '아이템 판매 수익' 우편을 1건 생성한다.
    ///  - mailboxes/{sellerUid}/inbox/{autoId}
    ///  - type: "SaleIncome", title: "아이템 판매 수익", amount: price
    ///  - isClaimed=false, createdAt=serverTime
    /// </summary>
    private async Task CreateSaleIncomeMailAsync(string sellerUid, string listingId, int amount)
    {
        try
        {
            if (string.IsNullOrEmpty(sellerUid) || amount <= 0) return;

            var db = FirebaseFirestore.DefaultInstance;
            var inbox = db.Collection("mailboxes")
                          .Document(sellerUid)
                          .Collection("inbox")
                          .Document(); // auto id

            var data = new Dictionary<string, object>
        {
            { "type", "SaleIncome" },                         // 우편 타입 (친구요청 등과 구분용)
            { "title", "아이템 판매 수익" },                       // 제목
            { "amount", amount },                             // 수익 골드
            { "listingId", listingId },                       // 원인 제공 listing
            { "isClaimed", false },                           // 수령 여부
            { "createdAt", FieldValue.ServerTimestamp }       // 정렬/표시용
        };

            await inbox.SetAsync(data);
            Debug.Log($"[Mail] 판매 수익 우편 발송: {sellerUid} / +{amount}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Mail] 우편 생성 실패: {e.Message}");
        }
    }

    #endregion


    // ============ 패널 프리셋/유틸 =================

    /// <summary> [역할] 로컬 상점: 첫 진입 상태 </summary>
    private void SetLocalIdleUI()
    {
        panelRight.SetActive(false); // 클릭 전엔 안 보이게

        InitExclusiveToggles(changeBuyOrSellToggleImagePairs, ref lastSelectedOnlineStoreMode);
        UpdateToggle(changeBuyOrSellToggleImagePairs);
    }

    /// <summary> [역할] 로컬 상점: 상품 클릭 후 상태 </summary>
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

    /// <summary> [역할] 온라인 상점: 탭 진입 기본 상태 (요청: 우측패널 On + 내 판매목록 기본) </summary>
    private void SetOnlineIdleUI()
    {
        if (panelRight) panelRight.SetActive(true);
        if (panelSearch) panelSearch.SetActive(true);
        if (panelInfoToggle) panelInfoToggle.SetActive(true);
        if (panelMySalesList) panelMySalesList.SetActive(true);
        if (panelInfo) panelInfo.SetActive(false);

        if (btnApply) btnApply.SetActive(false);
        if (btnSell) btnSell.SetActive(false);

        // 기본으로 "내 판매 목록" 탭 선택
        if (selectItemToggleImagePairs != null && selectItemToggleImagePairs.Count > 1)
        {
            SelectTab(selectItemToggleImagePairs, 1, ref lastSelectedItemInfo, ShowSelectedItemPanel);
            UpdateToggle(selectItemToggleImagePairs);
        }

        // 아이템 타입 토글 초기화
        InitExclusiveToggles(itemTypeToggleImagePairs, ref lastSelectedItemType);
        UpdateToggle(itemTypeToggleImagePairs);

        // 최신 내 판매 목록 갱신
        if (sellPanel != null)
            sellPanel.RequestRefreshMySales();


        // 가격 캐시 초기화
        lastSelectedPrice = 0;
    }

    /// <summary>
    /// [역할] 온라인 상점: '구매' 모드에서 상품 클릭 후 상태
    ///  - Info 패널 갱신
    ///  - btnApply 표시 + 가격 라벨링
    ///  - 골드 보유량에 따라 interactable 제어
    /// </summary>
    private void SetOnlineSelectedUI_Buy(Product p)
    {
        if (panelRight) panelRight.SetActive(true);
        if (panelSearch) panelSearch.SetActive(true);
        if (panelInfo) panelInfo.SetActive(true);
        if (panelInfoToggle) panelInfoToggle.SetActive(true);
        if (panelMySalesList) panelMySalesList.SetActive(false);

        if (btnSell) btnSell.SetActive(false);
        if (btnApply) btnApply.SetActive(true);

        ShowPanelInfoChildren();

        // 슬롯의 표시 가격을 읽어 '온라인 가격'으로 사용
        lastSelectedPrice = ReadDisplayedPriceFromSlot(p != null ? p.gameObject : null);

        int priceToUse = (lastSelectedPrice > 0) ? lastSelectedPrice : p.Price;

        if (p.IsConsume)
            ItemInfoPanel.instance.ShowItemInfo(p.BoundConsume.name_item, p.BoundConsume.description, priceToUse, p.BoundConsume.icon, p.BoundConsume.effects);
        else if (p.IsEquip)
            ItemInfoPanel.instance.ShowItemInfo(p.BoundEquip.name_item, p.BoundEquip.description, priceToUse, p.BoundEquip.icon, p.BoundEquip.effects);

        SetApplyButtonLabel(priceToUse);
        UpdateApplyButtonState();
    }

    /// <summary>
    /// [역할] 온라인 상점: '판매' 모드에서 상품 클릭 후 상태 (기존 로직 유지)
    /// </summary>
    private void SetOnlineSelectedUI_Sell()
    {
        panelRight.SetActive(true);
        panelSearch.SetActive(true);
        panelInfo.SetActive(true);
        btnApply.SetActive(false);
        btnSell.SetActive(true);
        panelMySalesList.SetActive(false);
        if (panelInfoToggle) panelInfoToggle.SetActive(true);
        if (panelMySalesList) panelMySalesList.SetActive(false);

        // ★ 버튼 즉시 클릭 보장
        var sellBtn = btnSell ? btnSell.GetComponent<UnityEngine.UI.Button>() : null;
        if (sellBtn) sellBtn.interactable = true;   // [역할] 판매 버튼 즉시 활성화
    }

    /// <summary>
    /// [역할] Apply 버튼의 라벨을 “구매 (n,nnnG)”로 갱신
    ///  - Text와 TMP_Text 둘 다 지원
    /// </summary>
    private void SetApplyButtonLabel(int price)
    {
        if (btnApply == null) return;

        var txt = btnApply.GetComponentInChildren<Text>(true);
        if (txt != null)
        {
            txt.text = $"{price}";
            return;
        }

        var tmp = btnApply.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.text = $"{price}";
        }
    }

    /// <summary> [역할] Apply 버튼 표시/숨김 </summary>
    private void SetApplyButtonVisible(bool visible)
    {
        if (btnApply) btnApply.SetActive(visible);
    }

    /// <summary>
    /// [역할] 슬롯 하위의 Txt_Price에서 정수 가격을 파싱
    ///  - Text/TMP_Text 모두 지원, 천단위/문자 포함 대비
    /// </summary>
    private int ReadDisplayedPriceFromSlot(GameObject slot)
    {
        if (slot == null) return 0;

        string raw = null;

        var t1 = slot.transform.Find("Txt_Price")?.GetComponent<Text>();
        if (t1 != null) raw = t1.text;

        if (string.IsNullOrEmpty(raw))
        {
            var t2 = slot.transform.Find("Txt_Price")?.GetComponent<TMP_Text>();
            if (t2 != null) raw = t2.text;
        }

        if (string.IsNullOrEmpty(raw)) return 0;

        // 숫자만 추출
        System.Text.StringBuilder sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsDigit(c)) sb.Append(c);
        }

        if (int.TryParse(sb.ToString(), out int price))
            return price;

        return 0;
    }

    /// <summary>
    /// [역할] 토글 그룹의 특정 인덱스를 강제로 선택하고 후처리 콜백 실행
    /// </summary>
    private void SelectTab(List<ToggleImagepair> group, int index, ref Toggle lastSelected, System.Action<int> after = null)
    {
        if (group == null || index < 0 || index >= group.Count) return;

        var t = group[index].toggle;

        if (!t.isOn)
        {
            t.isOn = true; // 기존 리스너(OnToggleChanged/ShowSelectedItemPanel)가 호출됨
        }
        else
        {
            OnToggleChanged(t, group, ref lastSelected);
            UpdateToggle(group);
            after?.Invoke(index);
        }
    }

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

    /// <summary>
    /// [역할] Panel_Info의 자식(Img_ItemFrame, Txt_* 등)을 전부 숨긴다.
    ///  - 패널 프레임(panelInfo)은 켜둔 채로 내용물만 감춤
    /// </summary>
    private void HidePanelInfoChildren()
    {
        if (!panelInfo) return;
        for (int i = 0; i < panelInfo.transform.childCount; i++)
            panelInfo.transform.GetChild(i).gameObject.SetActive(false);
    }

    /// <summary>
    /// [역할] Panel_Info의 자식들을 전부 다시 보이게 한다.
    ///  - 상품 클릭 등으로 정보가 채워질 때 호출
    /// </summary>
    private void ShowPanelInfoChildren()
    {
        if (!panelInfo) return;
        for (int i = 0; i < panelInfo.transform.childCount; i++)
            panelInfo.transform.GetChild(i).gameObject.SetActive(true);
    }

    /// <summary>
    /// [역할] 선택/가격/버튼/정보패널을 깨끗하게 초기화
    ///  - 토글 전환(상점/구매↔판매/내 판매목록 등) 시 호출
    /// </summary>
    private void ClearSelectionAndInfo()
    {
        // 현재 선택된 슬롯 시각 효과 원복
        if (Product.CurrentSelected != null)
            Product.CurrentSelected.ResetToDefaultImage();

        // 정적 선택 참조 해제
        typeof(Product)
            .GetField("currentSelectedProduct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, null);

        // 가격 캐시/버튼 초기화
        lastSelectedPrice = 0;
        SetApplyButtonVisible(false);
        UpdateApplyButtonState();

        // 정보 내용물 숨김 (패널 자체는 상황에 따라 켜둘 수 있음)
        HidePanelInfoChildren();
    }

}
