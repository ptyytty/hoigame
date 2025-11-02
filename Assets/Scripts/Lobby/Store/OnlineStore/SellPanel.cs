using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellPanel : MonoBehaviour
{
    [Header("UI References")]
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

    [Header("My Sales (Detail Panel)")]
    [SerializeField] private GameObject panelMyListingDetail; // 새로 뜨는 상세 패널
    [SerializeField] private TMP_Text txtDetailName;          // 상세: 아이템명
    [SerializeField] private TMP_Text txtDetailPrice;         // 상세: 가격
    [SerializeField] private TMP_Text txtDetailQty;           // 상세: 수량
    [SerializeField] private Button btnCancelSale;            // 상세: 판매 취소 버튼
    [SerializeField] private Button btnDetailClose;           // 상세: 닫기 버튼

    private int ownedCount = 0;    // 보유 수량
    private int sellCount = 1;     // 판매 예정 수량
    private string itemName = "";
    private Product selectedProduct;

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

    void Start()
    {
        // [역할] 버튼 이벤트 바인딩 및 기본 비활성화
        btnPlus.onClick.AddListener(OnPlus);
        btnMinus.onClick.AddListener(OnMinus);
        btnConfirm.onClick.AddListener(OnConfirm);
        btnCancel.onClick.AddListener(Hide);

        if (btnCancelSale) btnCancelSale.onClick.AddListener(OnClickCancelSale);
        if (btnDetailClose) btnDetailClose.onClick.AddListener(() => ShowListingDetail(null));

        gameObject.SetActive(false);
        if (panelMyListingDetail) panelMyListingDetail.SetActive(false);
    }

    /// <summary>
    /// [역할] 판매 패널 열기 및 선택한 아이템 기본 정보 표시
    /// </summary>
    public void Show(Product product, int count)
    {
        selectedProduct = product;
        ownedCount = count;
        sellCount = 1;

        itemName = product.IsConsume ? product.BoundConsume.name_item :
                   product.IsEquip ? product.BoundEquip.name_item : "알 수 없음";

        txtItemName.text = itemName;
        txtOwnedCount.text = $"보유 수량: {ownedCount}";
        txtSellCount.text = $"{sellCount}";
        inputPrice.text = "10"; // 기본 10원

        _ = RefreshMySalesAsync(); // 내 판매글 리스트 갱신
        gameObject.SetActive(true);
    }

    /// <summary>
    /// [역할] 판매 수량 +1 (보유 수량 한도)
    /// </summary>
    private void OnPlus()
    {
        if (sellCount < ownedCount)
        {
            sellCount++;
            txtSellCount.text = $"{sellCount}";
        }
    }

    /// <summary>
    /// [역할] 판매 수량 -1 (최소 1)
    /// </summary>
    private void OnMinus()
    {
        if (sellCount > 1)
        {
            sellCount--;
            txtSellCount.text = $"{sellCount}";
        }
    }

    /// <summary>
    /// [역할] 판매 확정: Firestore에 등록(보안 규칙 준수), 로컬 인벤토리 차감, UI 갱신
    /// </summary>
    private async void OnConfirm()
    {
        if (selectedProduct == null) return;

        // 1) 판매 수량
        int qty = Mathf.Clamp(sellCount, 1, ownedCount);

        // 2) 가격 파싱 + 10원 단위 내림(입력칸에도 반영)
        if (!int.TryParse(inputPrice.text, out int rawPrice)) rawPrice = 10;
        int priceRounded = Mathf.Max(10, (rawPrice / 10) * 10);
        inputPrice.text = priceRounded.ToString();

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

        // 5) 업로드 데이터 (규칙과 정확히 일치)
        var db = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("marketListings").Document(); // auto id

        var data = new Dictionary<string, object> {
            { "sellerUid", uid },                      // string
            { "type", type },                          // "Consume" | "Equipment"
            { "itemId", itemId },                      // int (로컬 카탈로그 복원용)
            { "priceGold", priceRounded },             // int
            { "qty", qty },                            // int (>0)
            { "isActive", true },                      // bool
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

        Hide();
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
            // [역할] 소비 아이템 수량 차감
            inv.RemoveConsumeItem(itemId, qty);
        }
        else if (type == "Equipment")
        {
            // [역할] 장비는 개수만큼 제거
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
            // [역할] 소비 아이템 수량 복구 (정의 객체 필요)
            var def = ItemCatalog.GetConsume(itemId);
            if (def != null && qty > 0)
            {
                inv.AddConsumeItem(def, qty);
                inv.NotifyChanged();
            }
        }
        else if (type == "Equipment")
        {
            // [역할] 장비 복구 (정의 객체 필요)
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

    /// <summary>
    /// [역할] '내 판매 목록' 갱신: 내가 올린 글만 최신순(createdAt desc)으로 가져와 리스트에 출력
    /// - 각 행은 Button으로 동작하여 상세 패널을 열도록 구성
    /// </summary>
    public async Task RefreshMySalesAsync()
    {
        if (!myListContent || !sellItemPrefab) return;

        // 기존 행 정리
        foreach (Transform child in myListContent) Destroy(child.gameObject);

        var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        var db = FirebaseFirestore.DefaultInstance;
        var q = db.Collection("marketListings")
                  .WhereEqualTo("sellerUid", uid)
                  .OrderByDescending("createdAt"); // 최신순(호환성 안전)

        var snap = await q.GetSnapshotAsync();

        foreach (var doc in snap.Documents)
        {
            string type = doc.GetValue<string>("type");
            int itemId = doc.GetValue<int>("itemId");
            int price = doc.GetValue<int>("priceGold");
            int qty = doc.GetValue<int>("qty");
            bool isActive = doc.GetValue<bool>("isActive");

            // 다 팔렸거나 비활성화된 글은 리스트에서 제외
            if (qty <= 0 || !isActive) continue;

            // 로컬 카탈로그로 이름 복원
            string displayName =
                (type == "Consume")
                ? (ItemCatalog.GetConsume(itemId)?.name_item ?? $"Consume#{itemId}")
                : (ItemCatalog.GetEquip(itemId)?.name_item ?? $"Equip#{itemId}");

            // UI 행 생성
            var row = Instantiate(sellItemPrefab, myListContent);

            // 프리팹 바인딩 (오브젝트 경로/이름은 프로젝트에 맞춰 조정)
            var nameText  = row.transform.Find("Txt_ItemName")?.GetComponent<TMP_Text>();
            var priceText = row.transform.Find("Txt_Price")?.GetComponent<TMP_Text>();
            var qtyText   = row.transform.Find("Txt_Count")?.GetComponent<TMP_Text>();
            var rowBtn    = row.GetComponent<Button>(); // 행 자체가 버튼이라고 가정

            if (nameText)  nameText.text  = displayName;
            if (priceText) priceText.text = $"{price:N0}";
            if (qtyText)   qtyText.text   = $"수량: {qty}";

            // 상세용 Listing 객체 준비
            var listing = new Listing
            {
                DocumentId  = doc.Id,
                Type        = type,
                ItemId      = itemId,
                Qty         = qty,
                IsActive    = isActive,
                DisplayName = displayName
            };

            // [역할] 행 버튼을 누르면 상세 패널 열기
            if (rowBtn)
            {
                rowBtn.onClick.RemoveAllListeners();
                rowBtn.onClick.AddListener(() => ShowListingDetail(listing));
            }
        }
    }

    /// <summary>
    /// [역할] 상세 패널 열기/닫기 및 선택된 판매글 바인딩
    /// listing == null이면 패널을 닫는다.
    /// </summary>
    private void ShowListingDetail(Listing? listing)
    {
        if (panelMyListingDetail == null) return;

        if (listing == null)
        {
            _selectedListing = default;
            panelMyListingDetail.SetActive(false);
            return;
        }

        _selectedListing = listing.Value;

        if (txtDetailName)  txtDetailName.text  = _selectedListing.DisplayName;
        if (txtDetailQty)   txtDetailQty.text   = $"수량: {_selectedListing.Qty}";

        if (btnCancelSale)  btnCancelSale.interactable = _selectedListing.IsActive && _selectedListing.Qty > 0;

        panelMyListingDetail.SetActive(true);
    }

    /// <summary>
    /// [역할] 상세 패널의 "판매 취소" 버튼 눌렀을 때 처리:
    /// - Firestore: isActive=false, qty=0, updatedAt 갱신 (규칙 허용 필드만 수정)
    /// - 로컬 인벤토리: 해당 수량 복원
    /// - UI: 목록/인벤토리 갱신 후 상세 패널 닫기
    /// </summary>
    private async void OnClickCancelSale()
    {
        if (string.IsNullOrEmpty(_selectedListing.DocumentId) || _selectedListing.Qty <= 0) return;

        // 1) Firestore에서 비활성화 + 수량 0 처리
        var db = FirebaseFirestore.DefaultInstance;

        try
        {
            var updates = new Dictionary<string, object> {
                { "isActive", false },                         // 규칙 허용
                { "qty", 0 },                                  // 규칙 허용(가격/수량/활성만)
                { "updatedAt", FieldValue.ServerTimestamp }    // 규칙 허용
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

    /// <summary>
    /// [역할] (옵션) 내 판매글을 수동으로 닫는 메서드 — 현재 흐름에선 상세 패널에서 OnClickCancelSale로 대체
    /// </summary>
    private async Task CloseMyListingAsync(string listingId)
    {
        var db = FirebaseFirestore.DefaultInstance;
        try
        {
            await db.Collection("marketListings")
                    .Document(listingId)
                    .UpdateAsync(new Dictionary<string, object> {
                        { "isActive", false },
                        { "updatedAt", FieldValue.ServerTimestamp }
                    });

            await RefreshMySalesAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SellPanel] close 실패: {e}");
        }
    }

    /// <summary>
    /// [역할] 판매 패널 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
