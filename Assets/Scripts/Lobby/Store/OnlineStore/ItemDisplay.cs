using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Firestore;

/// <summary>
/// [역할] 온라인 상점 중앙부에 현재 보유한 아이템을 버튼 형태로 출력하는 스크립트
/// </summary>

public class ItemDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;     // 아이템 슬롯 부모
    [SerializeField] private GameObject itemSlotPrefab;   // Product 프리팹 (공용)
    [Header("Mode")]
    public bool isSellMode = false;                       // true=판매, false=구매

    private InventoryRuntime inv;

    void Start()
    {
        inv = InventoryRuntime.Instance;
        RefreshItemList();
    }

    /// <summary>
    /// [역할] 구매/판매 모드에 따라 아이템 목록 출력
    /// </summary>
    public void RefreshItemList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (isSellMode)
            DisplayMyItems();    // 내 보유 아이템 (판매용)
        else
            DisplayOnlineItems(); // 온라인 아이템 (구매용)
    }

    // ===============================================================
    // 🔹 판매 탭 (내 아이템)
    // ===============================================================
    private void DisplayMyItems()
    {
        if (inv == null)
        {
            Debug.LogWarning("[ItemDisplay] InventoryRuntime이 없습니다.");
            return;
        }

        // 소비 아이템
        foreach (var owned in inv.GetOwnedConsumeItems())
        {
            if (owned.count <= 0) continue;
            var go = Instantiate(itemSlotPrefab, contentParent);
            var product = go.GetComponent<Product>();
            if (product == null) continue;

            product.SetConsumeItemData(owned.itemData);
            HideCoinOnly(go.transform);
            SetCount(go.transform, owned.count);
        }

        // 장비 아이템
        foreach (var owned in inv.ownedEquipItem)
        {
            if (owned.itemData == null) continue;
            var go = Instantiate(itemSlotPrefab, contentParent);
            var product = go.GetComponent<Product>();
            if (product == null) continue;

            product.SetSlotImageByJob(owned.itemData.jobCategory);
            product.SetEquipItemData(owned.itemData);
            HideCoinOnly(go.transform);
            SetCount(go.transform, 1);
        }
    }

    // ===============================================================
    // 🔹 구매 탭 (온라인 마켓)
    // ===============================================================
    private async void DisplayOnlineItems()
    {
        var db = FirebaseFirestore.DefaultInstance;
        var snapshot = await db.Collection("marketListings").GetSnapshotAsync();

        foreach (var doc in snapshot.Documents)
        {
            string type = doc.GetValue<string>("type");
            int itemId = int.Parse(doc.GetValue<string>("itemId"));
            int price = doc.GetValue<int>("priceGold");

            if (type == "Consume")
            {
                var def = ItemCatalog.GetConsume(itemId);
                if (def == null) continue;

                var go = Instantiate(itemSlotPrefab, contentParent);
                var product = go.GetComponent<Product>();
                if (product == null) continue;

                product.SetConsumeItemData(def);
                SetPrice(go.transform, price);
            }
            else if (type == "Equipment")
            {
                var def = ItemCatalog.GetEquip(itemId);
                if (def == null) continue;

                var go = Instantiate(itemSlotPrefab, contentParent);
                var product = go.GetComponent<Product>();
                if (product == null) continue;

                product.SetSlotImageByJob(def.jobCategory);
                product.SetEquipItemData(def);
                SetPrice(go.transform, price);
            }
        }
    }

    // ===============================================================
    // 🔸 공통 UI 제어 메서드
    // ===============================================================
    /// <summary>
    /// [역할] 판매 모드에서는 코인 이미지만 비활성화
    /// </summary>
    private void HideCoinOnly(Transform t)
    {
        var coin = t.Find("Img_Coin");
        if (coin) coin.gameObject.SetActive(false);
    }

    /// <summary>
    /// [역할] 구매 모드에서 가격 표시
    /// </summary>
    private void SetPrice(Transform t, int price)
    {
        var txt = t.Find("Txt_Price")?.GetComponent<TMP_Text>();
        if (txt) txt.text = $"{price}";
    }

    /// <summary>
    /// [역할] 판매 모드에서 보유 수량 표시 (Txt_Price 오브젝트에 표시)
    /// </summary>
    private void SetCount(Transform t, int count)
    {
        // 판매 모드에서는 가격 대신 수량을 표시해야 함
        var txtPrice = t.Find("Txt_Price")?.GetComponent<TMP_Text>();
        var txtCount = t.Find("Txt_Count")?.GetComponent<TMP_Text>();

        // 코인 이미지 숨김
        var coin = t.Find("Img_coin");
        if (coin) coin.gameObject.SetActive(false);

        if (txtPrice != null)
        {
            txtPrice.gameObject.SetActive(true);
            txtPrice.text = $"수량: {count}";
        }

        // Txt_Count는 별도 표시 안 함 (중복 방지)
        if (txtCount != null)
            txtCount.gameObject.SetActive(false);
    }
}
