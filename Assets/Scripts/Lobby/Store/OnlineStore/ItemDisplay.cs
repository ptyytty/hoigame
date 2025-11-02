using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase.Auth;

/// <summary>
/// [역할] 온라인 상점 중앙부에 아이템을 출력하고,
///        StoreManager에서 주입한 필터/정렬(타입, 최신/가격, 역순)을 반영한다.
/// </summary>
public class ItemDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;     // 아이템 슬롯 부모
    [SerializeField] private GameObject itemSlotPrefab;   // Product 프리팹 (공용)

    [Header("Mode")]
    public bool isSellMode = false;                       // true=판매, false=구매

    // 필터/정렬 옵션 (StoreManager에서 주입)
    public enum ItemTypeFilter { All, Consume, Equipment }

    [Header("Filter & Sort")]
    [SerializeField] private ItemTypeFilter typeFilter = ItemTypeFilter.All;                       
    [SerializeField] private SortedDropdown.SortOption sortKey = SortedDropdown.SortOption.Newest; 
    [SerializeField] private bool isAscending = false; // 최신순의 기본은 내림(false)

    private InventoryRuntime inv;

    /// <summary> [역할] 외부에서 타입 필터 설정(전체/소비/장비) </summary>
    public void SetTypeFilter(ItemTypeFilter t) => typeFilter = t;

    /// <summary> [역할] 외부에서 정렬 기준/방향 설정(최신/가격, 오름/내림) </summary>
    public void SetSort(SortedDropdown.SortOption key, bool ascending)
    {
        sortKey = key;
        isAscending = ascending;
    }

    void Start()
    {
        inv = InventoryRuntime.Instance;
        RefreshItemList();
    }

    /// <summary>
    /// [역할] 현재 모드(구매/판매)에 맞게 리스트 갱신
    /// </summary>
    public void RefreshItemList()
    {
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        if (isSellMode) DisplayMyItems();
        else _ = DisplayOnlineItemsAsync();
    }

    // ===========================================================
    // 🔹 판매 탭 (내 아이템)
    // ===========================================================
    private void DisplayMyItems()
    {
        if (inv == null)
        {
            Debug.LogWarning("[ItemDisplay] InventoryRuntime이 없습니다.");
            return;
        }

        var spawned = new List<GameObject>();

        // 소비 아이템
        foreach (var owned in inv.GetOwnedConsumeItems())
        {
            if (owned.count <= 0) continue;
            if (typeFilter == ItemTypeFilter.Equipment) continue; // 장비만 보기일 때 스킵

            var go = Instantiate(itemSlotPrefab, contentParent);
            spawned.Add(go);

            var p = go.GetComponent<Product>();
            if (p == null) continue;

            p.SetConsumeItemData(owned.itemData);
            HideCoinOnly(go.transform);
            SetCount(go.transform, owned.count);
        }

        // 장비 아이템
        foreach (var owned in inv.ownedEquipItem)
        {
            if (owned.itemData == null) continue;
            if (typeFilter == ItemTypeFilter.Consume) continue; // 소비만 보기일 때 스킵

            var go = Instantiate(itemSlotPrefab, contentParent);
            spawned.Add(go);

            var p = go.GetComponent<Product>();
            if (p == null) continue;

            p.SetSlotImageByJob(owned.itemData.jobCategory);
            p.SetEquipItemData(owned.itemData);
            HideCoinOnly(go.transform);
            SetCount(go.transform, 1);
        }

        // 판매 탭은 서버 필드(createdAt/priceGold)가 없으니, 단순 역/정만 제공
        if (sortKey == SortedDropdown.SortOption.Newest && isAscending)
        {
            for (int i = 0; i < contentParent.childCount; i++)
                contentParent.GetChild(i).SetSiblingIndex(contentParent.childCount - 1 - i);
        }
    }

    // ===========================================================
    // 🔹 구매 탭 (온라인 상점: marketListings)
    // ===========================================================
    private async System.Threading.Tasks.Task DisplayOnlineItemsAsync()
    {
        var db  = FirebaseFirestore.DefaultInstance;
        var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;

        // 1) 기본 쿼리: 항상 isActive + (가능하면) type 까지 포함한 baseQ 유지
        Query baseQ = db.Collection("marketListings").WhereEqualTo("isActive", true);
        if (typeFilter == ItemTypeFilter.Consume)   baseQ = baseQ.WhereEqualTo("type", "Consume");
        else if (typeFilter == ItemTypeFilter.Equipment) baseQ = baseQ.WhereEqualTo("type", "Equipment");

        // 2) 정렬 시도: 실패 시 baseQ로 재조회(= 타입 필터는 유지됨)
        QuerySnapshot snap = null;
        try
        {
            Query q = baseQ;
            if (sortKey == SortedDropdown.SortOption.Price)
                q = isAscending ? q.OrderBy("priceGold") : q.OrderByDescending("priceGold");
            else
                q = isAscending ? q.OrderBy("createdAt") : q.OrderByDescending("createdAt");

            snap = await q.GetSnapshotAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ItemDisplay] 서버 정렬 실패(색인 등). 타입 필터 유지한 채 로컬 정렬로 폴백: {e.Message}");
            snap = await baseQ.GetSnapshotAsync(); // ❗ 타입 필터 유지
        }

        // 3) 스냅샷 → 로컬 DTO (여기서도 타입 필터 2차 보정)
        var rows = new List<Row>();
        foreach (var doc in snap.Documents)
        {
            // 내가 올린 글 제외
            string sellerUid = SafeStr(doc, "sellerUid");
            if (!string.IsNullOrEmpty(uid) && sellerUid == uid) continue;

            string type = SafeStr(doc, "type");
            int itemId  = SafeInt(doc, "itemId");
            int price   = SafeInt(doc, "priceGold");
            System.DateTime created = SafeTime(doc, "createdAt");

            // 🔒 로컬 타입 필터 보정(서버 필터 실패 대비)
            if (typeFilter == ItemTypeFilter.Consume   && type != "Consume")   continue;
            if (typeFilter == ItemTypeFilter.Equipment && type != "Equipment") continue;

            rows.Add(new Row(type, itemId, price, created));
        }

        // 4) 로컬 정렬 폴백(또는 문서 일부에 정렬 필드 결여 시)
        if (sortKey == SortedDropdown.SortOption.Price)
            rows = isAscending ? rows.OrderBy(x => x.price).ToList() : rows.OrderByDescending(x => x.price).ToList();
        else
            rows = isAscending ? rows.OrderBy(x => x.created).ToList() : rows.OrderByDescending(x => x.created).ToList();

        // 5) UI 생성
        foreach (var r in rows)
        {
            if (r.type == "Consume")
            {
                var def = ItemCatalog.GetConsume(r.itemId);
                if (def == null) continue;

                var go = Instantiate(itemSlotPrefab, contentParent);
                var p  = go.GetComponent<Product>();
                if (p == null) continue;

                p.SetConsumeItemData(def);
                SetPrice(go.transform, r.price);
            }
            else if (r.type == "Equipment")
            {
                var def = ItemCatalog.GetEquip(r.itemId);
                if (def == null) continue;

                var go = Instantiate(itemSlotPrefab, contentParent);
                var p  = go.GetComponent<Product>();
                if (p == null) continue;

                p.SetSlotImageByJob(def.jobCategory);
                p.SetEquipItemData(def);
                SetPrice(go.transform, r.price);
            }
        }
    }

    // ── 로컬 DTO
    private struct Row
    {
        public string type;
        public int itemId;
        public int price;
        public System.DateTime created;

        public Row(string type, int itemId, int price, System.DateTime created)
        {
            this.type = type;
            this.itemId = itemId;
            this.price = price;
            this.created = created;
        }
    }

    // ── 안전 파서
    private string        SafeStr(DocumentSnapshot d, string f) { try { return d.GetValue<string>(f); } catch { return null; } }
    private int           SafeInt(DocumentSnapshot d, string f, int def = 0)
    {
        try { return d.GetValue<int>(f); }
        catch
        {
            try { var s = d.GetValue<string>(f); if (int.TryParse(s, out var v)) return v; } catch { }
            return def;
        }
    }
    private System.DateTime SafeTime(DocumentSnapshot d, string f) { try { return d.GetValue<Timestamp>(f).ToDateTime(); } catch { return System.DateTime.MinValue; } }

    // ===========================================================
    // 🔸 공통 UI 메서드
    // ===========================================================
    /// <summary> [역할] 판매 모드에서는 코인 이미지만 비활성화 </summary>
    private void HideCoinOnly(Transform t)
    {
        var c1 = t.Find("Img_Coin"); if (c1) c1.gameObject.SetActive(false);
        var c2 = t.Find("Img_coin"); if (c2) c2.gameObject.SetActive(false);
    }

    /// <summary> [역할] 구매 모드에서 가격 표시 </summary>
    private void SetPrice(Transform t, int price)
    {
        var txt = t.Find("Txt_Price")?.GetComponent<TMP_Text>();
        if (txt) txt.text = $"{price}";
    }

    /// <summary> [역할] 판매 모드에서 보유 수량 표시 </summary>
    private void SetCount(Transform t, int count)
    {
        HideCoinOnly(t);

        var txtPrice = t.Find("Txt_Price")?.GetComponent<TMP_Text>();
        if (txtPrice)
        {
            txtPrice.gameObject.SetActive(true);
            txtPrice.text = $"수량: {count}";
        }

        var txtCount = t.Find("Txt_Count");
        if (txtCount) txtCount.gameObject.SetActive(false);
    }
}
