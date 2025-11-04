using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

/// <summary>
/// [역할] 온라인 상점의 '판매' 플로우 전체 UI 컨트롤러
///  - 판매 패널 열기/닫기
///  - 내 판매 목록 갱신/행 클릭→상세 패널 오픈
///  - 상세 패널에서 '판매 취소' 처리
///  - 모달 블로커(화면 입력 차단) 관리: 
///     • 판매 패널 열릴 때도 차단(SetRootModal)
///     • 상세 패널 열릴 때도 차단(SetModal)
/// </summary>
public class SellPanel : MonoBehaviour
{
    [Header("Sell Panel")]
    [SerializeField] private TMP_Text txtItemName;
    [SerializeField] private TMP_Text txtOwnedCount;
    [SerializeField] private TMP_Text txtSellCount;
    [SerializeField] private TMP_InputField inputPrice;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    [Header("My Sales (List)")]
    [SerializeField] private Transform myListContent;   // 스크롤뷰 Content
    [SerializeField] private GameObject sellItemPrefab; // 프리팹 내부에 Text들 + Button 포함

    [Header("Cancel Panel")]
    [SerializeField] private GameObject panelMyListingDetail; // 새로 뜨는 상세 패널
    [SerializeField] private TMP_Text txtDetailName;          // 아이템명
    [SerializeField] private TMP_Text txtDetailQty;           // 수량
    [SerializeField] private Button btnCancelSale;            // 판매 취소 버튼
    [SerializeField] private Button btnDetailClose;           // 닫기 버튼

    [SerializeField] private GameObject modalBlocker;         // 전체 입력 차단용 블로커(같은 Canvas 하위에 형제 배치)

    // ───────────────────────────────────────────────
    // 런타임 상태
    private int ownedCount = 0;    // 보유 수량
    private int sellCount = 1;     // 판매 예정 수량
    private string itemName = "";
    private Product selectedProduct;

    private int _refreshVersion = 0;                 // 갱신 버전(요청마다 증가)
    private bool _isRefreshing = false;              // 동시 실행 방지 락
    private readonly HashSet<string> _rowIds = new();// 현재 프리팹에 바인딩된 docId 집합(중복 생성 방지)

    // 상세 패널에서 사용: 현재 선택된 내 판매글
    private Listing _selectedListing;

    /// <summary>
    /// [역할] 내 판매글 데이터 구조 (상세 패널에 바인딩/취소 처리용)
    /// </summary>
    private struct Listing
    {
        public string DocumentId;
        public string Type;   // "Consume" | "Equipment"
        public int ItemId;
        public int Qty;
        public bool IsActive;
        public string DisplayName;
    }

    // ───────────────────────────────────────────────

    private void Awake()
    {
        // [역할] 버튼 리스너를 먼저 깨끗이 정리하고 다시 바인딩(중복/유실 방지)
        BindStaticButtons();
    }

    private void OnEnable()
    {
        // [역할] 오브젝트가 재활성화될 때도 리스너 재바인딩(씬 전환/패널 토글 시 유실 방지)
        BindStaticButtons();
    }

    private void OnDisable()
    {
        // [역할] 상세 패널/블로커 닫기 + 현재 진행 중 Refresh는 다음 버전으로 무효화
        ShowListingDetail(null);
        SetRootModal(false);
        _refreshVersion++; // 진행 중인 오래된 요청 무효화
    }

    void Start()
    {
        // [역할] 버튼 이벤트 바인딩 및 기본 비활성화
        if (btnPlus)    { btnPlus.onClick.AddListener(OnPlus); }
        if (btnMinus)   { btnMinus.onClick.AddListener(OnMinus); }
        if (btnConfirm) { btnConfirm.onClick.AddListener(OnConfirm); }
        if (btnCancel)  { btnCancel.onClick.AddListener(Hide); }

        if (btnCancelSale)  btnCancelSale.onClick.AddListener(OnClickCancelSale);
        if (btnDetailClose) btnDetailClose.onClick.AddListener(() => ShowListingDetail(null));

        gameObject.SetActive(false);
        if (panelMyListingDetail) panelMyListingDetail.SetActive(false);
        if (modalBlocker) modalBlocker.SetActive(false);
    }

    /// <summary>
    /// [역할] 닫기/취소 등 고정 버튼 리스너 바인딩을 항상 최신 상태로 유지
    /// </summary>
    private void BindStaticButtons()
    {
        if (btnPlus) { btnPlus.onClick.RemoveListener(OnPlus); btnPlus.onClick.AddListener(OnPlus); }
        if (btnMinus){ btnMinus.onClick.RemoveListener(OnMinus); btnMinus.onClick.AddListener(OnMinus); }
        if (btnConfirm){ btnConfirm.onClick.RemoveListener(OnConfirm); btnConfirm.onClick.AddListener(OnConfirm); }
        if (btnCancel){ btnCancel.onClick.RemoveListener(Hide); btnCancel.onClick.AddListener(Hide); }

        if (btnCancelSale)
        {
            btnCancelSale.onClick.RemoveListener(OnClickCancelSale);
            btnCancelSale.onClick.AddListener(OnClickCancelSale);
        }
        if (btnDetailClose)
        {
            btnDetailClose.onClick.RemoveAllListeners();
            btnDetailClose.onClick.AddListener(() => ShowListingDetail(null));
        }
    }

    // ───────────────────────────────────────────────
    // 공개 API
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 판매 패널 열기 및 선택한 아이템 기본 정보 표시
    ///  - 패널을 보여주고, '판매 패널 레벨'의 모달 블로커를 켜서 다른 UI와의 상호작용을 차단
    /// </summary>
    public void Show(Product product, int count)
    {
        selectedProduct = product;
        ownedCount = count;
        sellCount = 1;

        itemName = product.IsConsume ? product.BoundConsume.name_item :
                   product.IsEquip ? product.BoundEquip.name_item : "알 수 없음";

        if (txtItemName)   txtItemName.text = itemName;
        if (txtOwnedCount) txtOwnedCount.text = $"보유 수량: {ownedCount}";
        if (txtSellCount)  txtSellCount.text = $"{sellCount}";
        if (inputPrice)    inputPrice.text = "10"; // 기본 10원

        _ = RefreshMySalesAsync(); // 내 판매글 리스트 갱신

        gameObject.SetActive(true);   // 패널 ON
        SetRootModal(true);           // ✅ 판매 패널을 켰을 때도 화면 전체 블로킹
    }

    /// <summary>
    /// [역할] '외부(UI 토글 전환 등)'에서 안전하게 내 판매 목록 갱신을 요청
    /// </summary>
    public void RequestRefreshMySales()
    {
        _ = RefreshMySalesAsync();
    }

    // ───────────────────────────────────────────────
    // 수량/가격/확정
    // ───────────────────────────────────────────────

    /// <summary> [역할] 판매 수량 +1 (보유 수량 한도) </summary>
    private void OnPlus()
    {
        if (sellCount < ownedCount)
        {
            sellCount++;
            if (txtSellCount) txtSellCount.text = $"{sellCount}";
        }
    }

    /// <summary> [역할] 판매 수량 -1 (최소 1) </summary>
    private void OnMinus()
    {
        if (sellCount > 1)
        {
            sellCount--;
            if (txtSellCount) txtSellCount.text = $"{sellCount}";
        }
    }

    /// <summary>
    /// [역할] 판매 확정: Firestore 등록 → 로컬 인벤토리 차감 → UI 갱신
    ///  - 가격은 10원 단위로 내림하여 저장(입력칸에도 반영)
    /// </summary>
    private async void OnConfirm()
    {
        if (selectedProduct == null) return;

        // 1) 판매 수량
        int qty = Mathf.Clamp(sellCount, 1, ownedCount);

        // 2) 가격 파싱 + 10원 단위 내림(입력칸에도 반영)
        if (!int.TryParse(inputPrice != null ? inputPrice.text : "10", out int rawPrice)) rawPrice = 10;
        int priceRounded = Mathf.Max(10, (rawPrice / 10) * 10);
        if (inputPrice) inputPrice.text = priceRounded.ToString();

        // 3) 타입/아이템ID 산출
        string type;
        int itemId;
        if (selectedProduct.IsConsume && selectedProduct.BoundConsume != null)
        {
            type = "Consume";
            itemId = selectedProduct.BoundConsume.id_item;
        }
        else if (selectedProduct.IsEquip && selectedProduct.BoundEquip != null)
        {
            type = "Equipment";
            itemId = selectedProduct.BoundEquip.id_item;
        }
        else
        {
            Debug.LogWarning("[SellPanel] 유효하지 않은 상품입니다.");
            return;
        }

        // 4) 판매자 정보
        var auth = FirebaseAuth.DefaultInstance;
        var uid = auth?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("[SellPanel] 로그인 필요");
            return;
        }

        // 5) 업로드 데이터
        var db = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("marketListings").Document(); // auto id

        var data = new Dictionary<string, object> {
            { "sellerUid", uid },
            { "type", type },                          // "Consume" | "Equipment"
            { "itemId", itemId },                      // 로컬 카탈로그 복원용
            { "priceGold", priceRounded },             // 가격
            { "qty", qty },                            // 수량
            { "isActive", true },
            { "createdAt", FieldValue.ServerTimestamp },
            { "updatedAt", FieldValue.ServerTimestamp },
        };

        try
        {
            await doc.SetAsync(data);
            Debug.Log($"[SellPanel] 업로드 완료: {doc.Id} / {itemId} / {priceRounded} x {qty}");

            // 로컬 인벤토리 차감
            ReduceLocalItemStock(type, itemId, qty);

            // 인벤토리 UI 갱신
            var display = FindObjectOfType<ItemDisplay>();
            if (display != null) display.RefreshItemList();

            // 내 판매 목록 갱신
            await RefreshMySalesAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SellPanel] 업로드 실패: {e}");
        }

        Hide(); // 패널 닫기(아래서 블로커도 꺼짐)
    }

    /// <summary>
    /// [역할] Firestore 업로드 성공 후 로컬 인벤토리에서 수량 차감 및 제거
    /// </summary>
    private void ReduceLocalItemStock(string type, int itemId, int qty)
    {
        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        if (type == "Consume")
        {
            inv.RemoveConsumeItem(itemId, qty);
        }
        else if (type == "Equipment")
        {
            int count = Mathf.Max(1, qty);
            for (int i = 0; i < count; i++)
                inv.RemoveEquipItem(itemId);
        }
    }

    /// <summary>
    /// [역할] 판매 취소 시 로컬 인벤토리에 수량 복원
    /// </summary>
    private void RestoreLocalItemStock(string type, int itemId, int qty)
    {
        var inv = InventoryRuntime.Instance;
        if (inv == null) return;

        if (type == "Consume")
        {
            var def = ItemCatalog.GetConsume(itemId);
            if (def != null && qty > 0)
            {
                inv.AddConsumeItem(def, qty);
                inv.NotifyChanged();
            }
        }
        else if (type == "Equipment")
        {
            var def = ItemCatalog.GetEquip(itemId);
            if (def != null)
            {
                int count = Mathf.Max(1, qty);
                for (int i = 0; i < count; i++)
                    inv.AddEquipItem(def);
                inv.NotifyChanged();
            }
        }
    }

    // ───────────────────────────────────────────────
    // 내 판매 목록 갱신/행 클릭
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] '내 판매 목록' 갱신: 내가 올린 글만 최신순(createdAt desc)으로 가져와 리스트에 출력
    /// - 각 행은 Button으로 동작하여 상세 패널을 열도록 구성
    /// </summary>
    public async Task RefreshMySalesAsync()
    {
        if (!myListContent || !sellItemPrefab) return;

        // (A) 요청 버전 발급
        int requestVersion = ++_refreshVersion;

        // (B) 동시 실행 허용하되 최신 버전만 반영
        if (_isRefreshing) { /* 버전 체크로 무효화 */ }
        _isRefreshing = true;

        try
        {
            var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (string.IsNullOrEmpty(uid)) return;

            var db = FirebaseFirestore.DefaultInstance;

            Query q = db.Collection("marketListings")
                        .WhereEqualTo("sellerUid", uid)
                        .WhereEqualTo("isActive", true)
                        .WhereGreaterThan("qty", 0)
                        .OrderByDescending("createdAt");

            QuerySnapshot snap = null;

            try
            {
                snap = await q.GetSnapshotAsync();
            }
            catch (System.Exception)
            {
                // 인덱스/스키마 이슈 폴백
                q = db.Collection("marketListings")
                      .WhereEqualTo("sellerUid", uid)
                      .OrderByDescending("createdAt");
                snap = await q.GetSnapshotAsync();
            }

            // (C) 오래된 요청이면 중단
            if (requestVersion != _refreshVersion) return;

            // (D) 새로 그릴 준비 — 기존 리스트 싹 비우기
            ClearMyListContent();

            foreach (var doc in snap.Documents)
            {
                if (requestVersion != _refreshVersion) return;

                bool isActive = doc.TryGetValue<bool>("isActive", out var a) ? a : false;
                int qty = doc.TryGetValue<int>("qty", out var qQty) ? qQty : (doc.TryGetValue<int>("quantity", out var alt) ? alt : 0);
                if (!isActive || qty <= 0) continue;

                string docId = doc.Id;
                if (_rowIds.Contains(docId)) continue; // 중복 생성 방지
                _rowIds.Add(docId);

                string type = doc.TryGetValue<string>("type", out var t) ? t : "Consume";
                int itemId  = doc.TryGetValue<int>("itemId", out var iid) ? iid : 0;
                int price   = doc.TryGetValue<int>("priceGold", out var p) ? p : 0;

                string displayName =
                    (type == "Consume")
                    ? (ItemCatalog.GetConsume(itemId)?.name_item ?? $"Consume#{itemId}")
                    : (ItemCatalog.GetEquip(itemId)?.name_item ?? $"Equip#{itemId}");

                // (F) UI 행 생성
                var row = Instantiate(sellItemPrefab, myListContent);

                var nameText  = row.transform.Find("Txt_ItemName")?.GetComponent<TMP_Text>();
                var priceText = row.transform.Find("Txt_Price")?.GetComponent<TMP_Text>();
                var qtyText   = row.transform.Find("Txt_Count")?.GetComponent<TMP_Text>();
                var rowBtn    = row.GetComponent<Button>();

                if (nameText)  nameText.text  = displayName;
                if (priceText) priceText.text = $"{price:N0}";
                if (qtyText)   qtyText.text   = $"수량: {qty}";

                var listing = new Listing
                {
                    DocumentId  = docId,
                    Type        = type,
                    ItemId      = itemId,
                    Qty         = qty,
                    IsActive    = isActive,
                    DisplayName = displayName
                };

                if (rowBtn)
                {
                    rowBtn.onClick.RemoveAllListeners();
                    rowBtn.onClick.AddListener(() =>
                    {
                        if (requestVersion == _refreshVersion)
                            ShowListingDetail(listing);
                    });
                }
            }
        }
        finally
        {
            // 최신 요청만 락 해제
            _isRefreshing = (requestVersion != _refreshVersion);
            if (!_isRefreshing) _isRefreshing = false;
        }
    }

    /// <summary>
    /// [역할] '내 판매 목록' 콘텐츠를 모두 제거(중복 방지용 집합도 초기화)
    /// </summary>
    private void ClearMyListContent()
    {
        if (!myListContent) return;
        _rowIds.Clear();
        for (int i = myListContent.childCount - 1; i >= 0; i--)
            Destroy(myListContent.GetChild(i).gameObject);
    }

    // ───────────────────────────────────────────────
    // 상세 패널(판매 취소) 열기/닫기 + 포커스/정렬/블로킹
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 상세 패널 열기/닫기 및 선택된 판매글 바인딩
    ///  listing == null이면 패널을 닫는다.
    ///  - 상세 패널 열 때: SetModal(true)로 블로커 ON
    /// </summary>
    private void ShowListingDetail(Listing? listing)
    {
        if (panelMyListingDetail == null) return;

        if (listing == null)
        {
            _selectedListing = default;
            panelMyListingDetail.SetActive(false);
            SetModal(false);              // 상세 블로커 OFF
            return;
        }

        _selectedListing = listing.Value;

        if (txtDetailName) txtDetailName.text = _selectedListing.DisplayName;
        if (txtDetailQty)  txtDetailQty.text  = $"수량: {_selectedListing.Qty}";

        SetModal(true);                   // ✅ 상세 패널 블로커 ON
        EnsureDetailPanelClickable();     // CanvasGroup 보정
        panelMyListingDetail.SetActive(true);

        // 정렬/레이캐스트 보정
        EnsureSortingForDetailAndBlocker();

        if (btnCancelSale)  btnCancelSale.interactable = _selectedListing.IsActive && _selectedListing.Qty > 0;
        if (btnDetailClose) btnDetailClose.interactable = true;

        // 첫 클릭이 포커싱에 소모되지 않게 다음 프레임 버튼 포커스
        if (gameObject.activeInHierarchy)
            StartCoroutine(FocusDetailPrimaryButtonNextFrame());
        else
            RunNextFrame(FocusDetailPrimaryButtonNextFrame);
    }

    /// <summary>
    /// [역할] 다음 프레임에 상세 패널의 기본 버튼(판매 취소)에 포커스 고정
    /// </summary>
    private IEnumerator FocusDetailPrimaryButtonNextFrame()
    {
        yield return null; // 한 프레임 대기
        Canvas.ForceUpdateCanvases();
        if (btnCancelSale && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(btnCancelSale.gameObject);
    }

    /// <summary>
    /// [역할] 상세 패널이 반드시 클릭 가능하도록 CanvasGroup 보정
    /// </summary>
    private void EnsureDetailPanelClickable()
    {
        if (!panelMyListingDetail.activeSelf)
            panelMyListingDetail.SetActive(true);

        var cg = panelMyListingDetail.GetComponent<CanvasGroup>();
        if (cg == null) cg = panelMyListingDetail.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // (옵션) 상위 가림막 이름 규칙이 있을 경우 끔
        var blocker = panelMyListingDetail.transform.parent?.Find("RaycastBlocker");
        if (blocker != null && blocker.gameObject.activeSelf)
            blocker.gameObject.SetActive(false);
    }

    /// <summary>
    /// [역할] 상세 패널과 블로커의 정렬을 강제로 보정
    ///  - 둘 다 같은 부모(Canvas) 아래
    ///  - 블로커 sortingOrder=5000, 상세 패널=5001
    /// </summary>
    private void EnsureSortingForDetailAndBlocker()
    {
        if (!panelMyListingDetail || !modalBlocker) return;

        var panelCanvas = panelMyListingDetail.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = panelMyListingDetail.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;

        var blockerCanvas = modalBlocker.GetComponent<Canvas>();
        if (blockerCanvas == null) blockerCanvas = modalBlocker.AddComponent<Canvas>();
        blockerCanvas.overrideSorting = true;

        const int baseOrder = 5000;
        blockerCanvas.sortingOrder = baseOrder;
        panelCanvas.sortingOrder   = baseOrder + 1;

        if (!panelMyListingDetail.GetComponent<GraphicRaycaster>())
            panelMyListingDetail.gameObject.AddComponent<GraphicRaycaster>();
        if (!modalBlocker.GetComponent<GraphicRaycaster>())
            modalBlocker.gameObject.AddComponent<GraphicRaycaster>();

        // 시블링 순서: 블로커를 끝-1, 상세 패널을 끝으로
        modalBlocker.transform.SetAsLastSibling();
        panelMyListingDetail.transform.SetAsLastSibling();
    }

    // ───────────────────────────────────────────────
    // 모달 블로커(판매 패널 레벨) — 새로 추가
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] '판매 패널(Panel_Sell) 레벨'에서 화면 전체 입력을 막는 블로커 토글
    ///  - Panel_Sell의 부모(같은 Canvas) 아래에 'UIBlocker'를 두고 정렬/레이캐스트 보장
    ///  - 판매 패널을 열 때 ON, 닫을 때 OFF
    /// </summary>
    private void SetRootModal(bool on)
    {
        var parent = transform.parent as RectTransform;
        if (parent == null) return;

        const int baseOrder = 5000;

        // 1) 블로커 준비(없으면 생성) + 풀스크린 앵커
        EnsureBlockerUnderParent(parent);

        // 2) Panel_Sell을 블로커보다 위로 정렬(중요!)
        EnsureSellPanelOnTop(baseOrder);

        // 3) 블로커 정렬/레이캐스트 보장
        var bc = modalBlocker.GetComponent<Canvas>();
        if (bc == null) bc = modalBlocker.AddComponent<Canvas>();
        bc.overrideSorting = true;
        bc.sortingOrder = baseOrder; // 5000 (아래)

        if (!modalBlocker.GetComponent<GraphicRaycaster>())
            modalBlocker.AddComponent<GraphicRaycaster>();

        var cg = modalBlocker.GetComponent<CanvasGroup>();
        if (cg == null) cg = modalBlocker.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.interactable = true;

        var img = modalBlocker.GetComponent<Image>();
        if (img == null) img = modalBlocker.AddComponent<Image>();
        img.raycastTarget = true;
        // 필요 시 어둡게 하고 싶으면 알파만 조절(입력 차단은 알파 0이어도 됨)
        if (img.color.a == 0f) img.color = new Color(0, 0, 0, 0f);

        // 4) 활성/비활성
        modalBlocker.SetActive(on);

        // 5) 혹시 레이아웃/시블링 순서가 꼬였을 때 대비해 두 개를 맨 끝으로 정렬
        //    (Blocker가 먼저, Panel_Sell이 그 위)
        modalBlocker.transform.SetAsLastSibling();
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// [역할] 부모 아래에 블로커를 안전하게 배치(없으면 생성, 있으면 위치/사이즈 보정)
    /// </summary>
    private void EnsureBlockerUnderParent(RectTransform parent)
    {
        if (modalBlocker == null)
        {
            var exist = parent.Find("UIBlocker");
            if (exist != null) modalBlocker = exist.gameObject;
            else
            {
                modalBlocker = new GameObject("UIBlocker",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasGroup),
                    typeof(GraphicRaycaster),
                    typeof(Image));
                var rt = modalBlocker.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var canvas = modalBlocker.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 5000;

                var cg = modalBlocker.GetComponent<CanvasGroup>();
                cg.blocksRaycasts = true;
                cg.interactable = true;

                var img = modalBlocker.GetComponent<Image>();
                img.color = new Color(0, 0, 0, 0f); // 기본은 투명
                img.raycastTarget = true;
            }
        }
        else
        {
            var rt = modalBlocker.GetComponent<RectTransform>();
            if (modalBlocker.transform.parent != parent)
                rt.SetParent(parent, false);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    // ───────────────────────────────────────────────
    // 상세 패널 전용 블로킹 (기존 방식 유지)
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 상세 패널 열릴 때 화면 전체 입력을 막는 블로커를 보장 생성/토글
    ///  - 블로커는 항상 상세 패널과 '같은 부모' 아래에 두고
    ///    블로커가 먼저(아래), 상세 패널이 그 위(마지막 시블링)가 되게 배치한다.
    /// </summary>
    private void SetModal(bool on)
    {
        if (panelMyListingDetail == null) return;

        // 1) 블로커 오브젝트 없으면 생성(상세 패널 부모 아래)
        if (modalBlocker == null)
        {
            var parent = panelMyListingDetail.transform.parent as RectTransform;
            if (parent == null) return;

            var exist = parent.Find("UIBlocker");
            if (exist != null) modalBlocker = exist.gameObject;
            else
            {
                modalBlocker = new GameObject("UIBlocker",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image),
                    typeof(CanvasGroup));

                var rt = modalBlocker.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = modalBlocker.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0, 0, 0, 0.5f); // 필요하면 0으로(완전 투명)
                img.raycastTarget = true;            // ★ 레이캐스트 차단 핵심

                var cg = modalBlocker.GetComponent<CanvasGroup>();
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }
        else
        {
            // 블로커가 다른 부모로 가 있었다면 상세 패널과 같은 부모로 강제 이동
            var parent = panelMyListingDetail.transform.parent as RectTransform;
            if (parent && modalBlocker.transform.parent != parent)
            {
                var rt = modalBlocker.GetComponent<RectTransform>();
                rt.SetParent(parent, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        // 2) 활성/비활성
        modalBlocker.SetActive(on);

        if (on)
        {
            EnsureSortingForDetailAndBlocker();
        }
    }

    /// <summary>
    /// [역할] Panel_Sell 쪽에 Canvas/GraphicRaycaster를 보장하고
    /// 블로커보다 한 단계 높은 sortingOrder로 올려서 내부 버튼들이 클릭 가능하게 만든다.
    /// </summary>
    private void EnsureSellPanelOnTop(int baseOrder = 5000)
    {
        // this == Panel_Sell
        var cv = GetComponent<Canvas>();
        if (cv == null) cv = gameObject.AddComponent<Canvas>();
        cv.overrideSorting = true;
        cv.sortingOrder = baseOrder + 1; // Blocker(5000)보다 위 = 5001

        // GraphicRaycaster도 보장 (클릭 받으려면 필요)
        if (!GetComponent<GraphicRaycaster>())
            gameObject.AddComponent<GraphicRaycaster>();
    }


    // ───────────────────────────────────────────────
    // 코루틴 유틸(비활성에서도 안전)
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 씬 어디서든 코루틴을 안전하게 돌리기 위한 전역 러너 (DontDestroyOnLoad)
    ///  - Panel_Sell가 비활성이어도 코루틴 가능
    /// </summary>
    private static CoroutineRunner _runner;
    private static CoroutineRunner Runner
    {
        get
        {
            if (_runner == null)
            {
                var go = new GameObject("UIRoutineRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<CoroutineRunner>();
            }
            return _runner;
        }
    }
    private class CoroutineRunner : MonoBehaviour { }

    /// <summary>
    /// [역할] 다음 프레임에 실행할 코루틴을 전역 러너에서 시작
    ///  - 비활성 오브젝트에서 StartCoroutine 금지 오류 방지
    /// </summary>
    private void RunNextFrame(Func<IEnumerator> routineFactory)
    {
        Runner.StartCoroutine(RunNextFrameRoutine(routineFactory));
    }

    private IEnumerator RunNextFrameRoutine(Func<IEnumerator> factory)
    {
        yield return null;              // 한 프레임 대기 (레이아웃 반영)
        Canvas.ForceUpdateCanvases();   // 모바일에서 레이아웃 갱신 보장
        yield return factory();
    }

    // ───────────────────────────────────────────────
    // 상세 패널: 판매 취소 처리
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 상세 패널의 "판매 취소" 버튼 눌렀을 때 처리:
    ///  - Firestore: isActive=false, qty=0, updatedAt 갱신
    ///  - 로컬 인벤토리: 해당 수량 복원
    ///  - UI: 목록/인벤토리 갱신 후 상세 패널 닫기
    /// </summary>
    private async void OnClickCancelSale()
    {
        if (string.IsNullOrEmpty(_selectedListing.DocumentId) || _selectedListing.Qty <= 0) return;

        // 1) Firestore 비활성화 + 수량 0
        var db = FirebaseFirestore.DefaultInstance;

        try
        {
            var updates = new Dictionary<string, object> {
                { "isActive", false },
                { "qty", 0 },
                { "updatedAt", FieldValue.ServerTimestamp }
            };

            await db.Collection("marketListings")
                    .Document(_selectedListing.DocumentId)
                    .UpdateAsync(updates);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SellPanel] 판매 취소 실패: {e}");
            return;
        }

        // 2) 로컬 인벤토리 복원
        RestoreLocalItemStock(_selectedListing.Type, _selectedListing.ItemId, _selectedListing.Qty);

        // 3) 인벤토리/내 판매 목록 UI 갱신
        var display = FindObjectOfType<ItemDisplay>();
        if (display != null) display.RefreshItemList();
        await RefreshMySalesAsync();

        // 4) 상세 패널 닫기
        ShowListingDetail(null);
    }

    // ───────────────────────────────────────────────
    // 패널 닫기
    // ───────────────────────────────────────────────

    /// <summary>
    /// [역할] 판매 패널 숨김
    ///  - 상세 패널 블로커/루트 블로커 모두 OFF
    /// </summary>
    public void Hide()
    {
        ShowListingDetail(null); // 상세 패널/블로커 OFF
        SetRootModal(false);     // 판매 패널 레벨 블로커 OFF
        gameObject.SetActive(false);
    }
}
