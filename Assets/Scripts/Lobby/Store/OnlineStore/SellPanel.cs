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

    [Header("My Sales")]
    [SerializeField] private Transform myListContent;   // 스크롤뷰 Content
    [SerializeField] private GameObject sellItemPrefab; // Name/Price/Qty/Status/BtnClose 포함

    private int ownedCount = 0;    // 보유 수량
    private int sellCount = 1;     // 판매 예정 수량
    private string itemName = "";
    private Product selectedProduct;

    void Start()
    {
        // 역할: 버튼 이벤트 바인딩 및 초기 비활성화
        btnPlus.onClick.AddListener(OnPlus);
        btnMinus.onClick.AddListener(OnMinus);
        btnConfirm.onClick.AddListener(OnConfirm);
        btnCancel.onClick.AddListener(Hide);
        gameObject.SetActive(false);
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

        _ = RefreshMySalesAsync();
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
    /// [역할] 판매 확정 버튼: 보안 규칙에 맞춘 필드로 Firestore에 문서 생성
    /// 규칙 필수 필드: sellerUid, itemId, type, priceGold(int), qty(int>0), isActive(bool), createdAt, (updatedAt 옵션)
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
        var nick = auth?.CurrentUser?.DisplayName ?? "Unknown";
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("[SellPanel] 로그인 필요");
            return;
        }

        // 5) 업로드 데이터 (✅ 보안 규칙과 정확히 일치하는 키/타입)
        var db = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("marketListings").Document(); // auto id

        var data = new Dictionary<string, object> {
            { "sellerUid", uid },                                   // string
            { "type", type },                                        // "Consume" | "Equipment"
            { "itemId", itemId },                                    // int (로컬 카탈로그 복원용)
            { "priceGold", priceRounded },                           // int
            { "qty", qty },                                          // int (>0)
            { "isActive", true },                                    // bool
            { "createdAt", FieldValue.ServerTimestamp },             // timestamp(서버)
            { "updatedAt", FieldValue.ServerTimestamp },             // timestamp(서버)
            // 참고: 규칙에는 없지만 표시용으로만 쓰는 필드는 제외하거나 규칙에 추가해야 함.
            // { "sellerNickname", nick } // 표시용을 쓰려면 규칙 keys 목록에 추가 필요
        };

        try
        {
            await doc.SetAsync(data);
            Debug.Log($"[SellPanel] 업로드 완료: {doc.Id} / {itemId} / {priceRounded} x {qty}");

            // ✅ 업로드 후 로컬 인벤토리 차감
            ReduceLocalItemStock(type, itemId, qty);

            // ✅ 인벤토리 UI 갱신
            var display = FindObjectOfType<ItemDisplay>();
            if (display != null) display.RefreshItemList();

            // ✅ 내 판매 목록 갱신
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
            // [역할] 장비는 개별 제거 (한 번에 하나만 올린다고 가정)
            inv.RemoveEquipItem(itemId);
        }
    }

    /// <summary>
    /// [역할] '내 판매 목록' 갱신: 내가 올린 글만 최신순(createdAt desc)으로 가져와 리스트에 출력
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
                  .OrderByDescending("createdAt"); // 최신순

        var snap = await q.GetSnapshotAsync();

        foreach (var doc in snap.Documents)
        {
            string type = doc.GetValue<string>("type");
            int itemId = doc.GetValue<int>("itemId");
            int price = doc.GetValue<int>("priceGold");
            int qty = doc.GetValue<int>("qty");                 // ✅ 변경: quantity → qty
            bool isActive = doc.GetValue<bool>("isActive");     // ✅ 변경: status(string) → isActive(bool)

            // 다 팔린 글/비활성 글은 목록에서 제외
            if (qty <= 0 || !isActive) continue;

            // 로컬 DB에서 이름/아이콘 복원
            string displayName =
                (type == "Consume")
                ? (ItemCatalog.GetConsume(itemId)?.name_item ?? $"Consume#{itemId}")
                : (ItemCatalog.GetEquip(itemId)?.name_item ?? $"Equip#{itemId}");

            var row = Instantiate(sellItemPrefab, myListContent);

            // 프리팹 내부 바인딩 (오브젝트 이름은 프로젝트에 맞춰 수정)
            var nameText = row.transform.Find("Txt_ItemName")?.GetComponent<TMP_Text>();
            var priceText = row.transform.Find("Txt_Price")?.GetComponent<TMP_Text>();
            var qtyText = row.transform.Find("Txt_Count")?.GetComponent<TMP_Text>();
            // var statusText = row.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            // var btnClose = row.transform.Find("BtnClose")?.GetComponent<Button>();

            if (nameText) nameText.text = displayName;
            if (priceText) priceText.text = $"{price:N0}";
            if (qtyText) qtyText.text = $"수량: {qty}";
            // if (statusText) statusText.text = isActive ? "active" : "closed";

            // if (btnClose)
            // {
            //     // [역할] 판매 종료(닫기) 버튼 — isActive일 때만 가능
            //     btnClose.interactable = isActive;
            //     btnClose.onClick.RemoveAllListeners();
            //     btnClose.onClick.AddListener(() => _ = CloseMyListingAsync(doc.Id));
            // }
        }
    }

    /// <summary>
    /// [역할] 내 판매글을 비활성화(isActive=false)로 변경. 성공 시 목록 갱신.
    /// 규칙: update는 priceGold/qty/isActive/updatedAt만 변경 가능.
    /// </summary>
    private async Task CloseMyListingAsync(string listingId)
    {
        var db = FirebaseFirestore.DefaultInstance;
        try
        {
            await db.Collection("marketListings")
                    .Document(listingId)
                    .UpdateAsync(new Dictionary<string, object> {
                        { "isActive", false },                         // ✅ 규칙 허용 필드
                        { "updatedAt", FieldValue.ServerTimestamp }    // ✅ 규칙 허용 필드
                    });

            await RefreshMySalesAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SellPanel] close 실패: {e}");
        }
    }

    /// <summary>
    /// [역할] 패널 숨김
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
