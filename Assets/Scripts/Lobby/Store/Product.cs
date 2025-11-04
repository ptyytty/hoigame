using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

///<summary>
/// 상품 오브젝트 (온라인/로컬 공용)
/// - [구매 모드] listingId, 온라인 가격/수량을 바인딩하고, 클릭 시 정보패널과 StoreManager를 갱신
/// - [판매 모드] 기존 로컬 보유 아이템 출력
///</summary>
public class Product : MonoBehaviour
{
    [Header("Product Info")]
    [SerializeField] private TMP_Text productName;
    [SerializeField] private TMP_Text productPrice;
    [SerializeField] private Image productImage;
    [SerializeField] private Image coinImageObject;
    [SerializeField] private Sprite coinImage;
    [SerializeField] private Sprite defaultGeneralImage;
    [SerializeField] private Sprite selectGeneralImage;
    [SerializeField] private List<JobSpritePair> jobSpritePairs;
    [SerializeField] private List<JobSpritePair> selectedSpritePairs;

    private static Product currentSelectedProduct;
    public static Product CurrentSelected => currentSelectedProduct;

    public bool IsConsume => boundConsume != null;
    public bool IsEquip => boundEquip != null;
    public int Price => boundPrice;
    public ConsumeItem BoundConsume => boundConsume;
    public EquipItem BoundEquip => boundEquip;

    private ConsumeItem boundConsume;
    private EquipItem boundEquip;
    private int boundPrice;

    private Dictionary<JobCategory, Sprite> spriteDict;
    private Dictionary<JobCategory, Sprite> selectedSpriteDict;
    private Image slotImage;
    private JobCategory currentCategory;
    private Sprite defaultSprite;

    // ── 온라인 구매용 바인딩 값들
    private string boundListingId;     // listing 문서 ID
    private int onlinePrice;           // 서버 가격(표시/검증)
    private int onlineQty;             // 서버 수량(남은 수량)

    public static event System.Action<Product> OnAnyProductClicked;

    void Awake()
    {
        spriteDict = jobSpritePairs.ToDictionary(p => p.category, p => p.sprite);
        selectedSpriteDict = selectedSpritePairs.ToDictionary(p => p.category, p => p.sprite);
        slotImage = GetComponent<Image>();
    }

    /// <summary> [역할] 슬롯 윤곽(직군별 이미지) 설정 </summary>
    public void SetSlotImageByJob(JobCategory category)
    {
        if (spriteDict.TryGetValue(category, out Sprite sprite))
        {
            slotImage.sprite = sprite;
            defaultSprite = sprite;
            currentCategory = category;
        }
    }

    /// <summary> [역할] 소비형 데이터 바인딩 + 클릭 시 정보패널 갱신 </summary>
    public void SetConsumeItemData(ConsumeItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;
        coinImageObject.sprite = coinImage;

        boundConsume = item;
        boundEquip = null;
        boundPrice = item.price;

        // 소비형은 공통 일반 프레임 사용
        defaultSprite = defaultGeneralImage;

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (currentSelectedProduct != null && currentSelectedProduct != this)
                currentSelectedProduct.ResetToDefaultImage();

            currentSelectedProduct = this;
            slotImage.sprite = selectGeneralImage;

            ItemInfoPanel.instance.ShowItemInfo(
                item.name_item,
                item.description,
                item.price,
                item.icon,
                item.effects
            );
            OnAnyProductClicked?.Invoke(this);

            FindObjectOfType<StoreManager>()?.UpdateApplyButtonState();
        });
    }

    /// <summary> [역할] 장비형 데이터 바인딩 + 클릭 시 정보패널 갱신 </summary>
    public void SetEquipItemData(EquipItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;
        coinImageObject.sprite = coinImage;

        boundConsume = null;
        boundEquip = item;
        boundPrice = item.price;

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (currentSelectedProduct != null && currentSelectedProduct != this)
                currentSelectedProduct.ResetToDefaultImage();

            currentSelectedProduct = this;

            if (selectedSpriteDict.TryGetValue(currentCategory, out Sprite selectedSprite))
                slotImage.sprite = selectedSprite;

            ItemInfoPanel.instance.ShowItemInfo(
                item.name_item,
                item.description,
                item.price,
                item.icon,
                item.effects
            );
            OnAnyProductClicked?.Invoke(this);

            FindObjectOfType<StoreManager>()?.UpdateApplyButtonState();
        });

    }

    /// <summary> [역할] 선택 해제 시 원래 슬롯 이미지 복원 </summary>
    public void ResetToDefaultImage()
    {
        slotImage.sprite = defaultSprite != null ? defaultSprite : defaultGeneralImage;
    }

    // ============== 온라인 구매 바인딩/표시 ===============

    /// <summary> [역할] 온라인 listingId 보관 </summary>
    public void BindListingId(string listingId) => boundListingId = listingId;
    /// <summary> [역할] 온라인 가격 보관 </summary>
    public void SetOnlinePrice(int price) => onlinePrice = price;
    /// <summary> [역할] 온라인 수량 보관 + 배지 갱신 </summary>
    public void SetOnlineQty(int qty)
    {
        onlineQty = Mathf.Max(0, qty);
        RefreshQtyBadge();
    }

    /// <summary> [역할] 현재 셀의 listingId 조회 </summary>
    public string GetListingId() => boundListingId;
    /// <summary> [역할] 현재 셀의 온라인 표시가격 조회 </summary>
    public int GetOnlinePrice() => onlinePrice;
    /// <summary> [역할] 현재 셀의 온라인 남은 수량 조회 </summary>
    public int GetOnlineQty() => onlineQty;

    /// <summary>
    /// [역할] 구매 성공 시 로컬 슬롯 수량 감소(즉시 UI 반영)
    ///  - 반환값: 감소 후 남은 수량
    /// </summary>
    public int DecreaseOnlineQty(int amount)
    {
        onlineQty = Mathf.Max(0, onlineQty - Mathf.Max(1, amount));
        RefreshQtyBadge();
        return onlineQty;
    }

    /// <summary>
    /// [역할] 슬롯 하위 "Txt_Count"를 찾아 xN 형태로 수량 뱃지 표기
    /// </summary>
    private void RefreshQtyBadge()
    {
        var tCount = transform.Find("Txt_Count")?.GetComponent<TMP_Text>();
        if (tCount != null)
        {
            tCount.gameObject.SetActive(true);
            tCount.text = $"수량: {onlineQty}";
        }
    }

    /// <summary>
    /// [역할] 이 상품을 현재 선택으로 강제 지정하고 슬롯 비주얼을 '선택 상태'로 바꾼다.
    ///  - 판매 모드에서 탭 전환 직후 선택이 풀리는 상황을 방지
    /// </summary>
    public void ForceSelectAsCurrent()
    {
        // 이전 선택 해제
        if (currentSelectedProduct != null && currentSelectedProduct != this)
            currentSelectedProduct.ResetToDefaultImage();

        currentSelectedProduct = this;

        // 타입에 따라 선택 프레임 적용
        if (IsEquip && selectedSpriteDict != null && selectedSpriteDict.TryGetValue(currentCategory, out Sprite sel))
            slotImage.sprite = sel;
        else
            slotImage.sprite = selectGeneralImage;
    }

}
