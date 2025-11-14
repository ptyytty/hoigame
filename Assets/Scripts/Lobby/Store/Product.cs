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

    [Header("Slot Frames")]
    [SerializeField] private Sprite defaultGeneralImage;   // 기본 프레임(소비형 공통)
    [SerializeField] private Sprite selectGeneralImage;    // 선택 프레임(소비형 공통)
    [SerializeField] private List<JobSpritePair> jobSpritePairs;       // 장비 직군별 기본 프레임
    [SerializeField] private List<JobSpritePair> selectedSpritePairs;  // 장비 직군별 선택 프레임

    private static Product currentSelectedProduct;
    public static Product CurrentSelected => currentSelectedProduct;

    public bool IsConsume => boundConsume != null;
    public bool IsEquip   => boundEquip   != null;
    public int  Price     => boundPrice;
    public ConsumeItem BoundConsume => boundConsume;
    public EquipItem   BoundEquip   => boundEquip;

    private ConsumeItem boundConsume;
    private EquipItem   boundEquip;
    private int         boundPrice;

    private Dictionary<JobCategory, Sprite> spriteDict;
    private Dictionary<JobCategory, Sprite> selectedSpriteDict;
    private Image slotImage;
    private JobCategory currentCategory;
    private Sprite defaultSprite;

    // ── 온라인 구매용 바인딩 값들
    private string boundListingId;   // listing 문서 ID
    private int    onlinePrice;      // 서버 가격(표시/검증)
    private int    onlineQty;        // 서버 남은 수량

    /// <summary>
    /// 역할: 다른 스크립트가 상품을 클릭했음을 구독할 수 있도록 제공
    ///  - StoreManager가 이 이벤트를 받아 정보 패널/버튼을 갱신
    /// </summary>
    public static event Action<Product> OnAnyProductClicked;

    void Awake()
    {
        // 역할: 직군별 프레임 딕셔너리 구성
        spriteDict        = (jobSpritePairs ?? new List<JobSpritePair>()).ToDictionary(p => p.category, p => p.sprite);
        selectedSpriteDict= (selectedSpritePairs ?? new List<JobSpritePair>()).ToDictionary(p => p.category, p => p.sprite);
        slotImage         = GetComponent<Image>();
    }

    /// <summary>역할: 직군에 맞는 기본 슬롯 프레임 지정(장비 전용)</summary>
    public void SetSlotImageByJob(JobCategory category)
    {
        if (spriteDict != null && spriteDict.TryGetValue(category, out Sprite sprite))
        {
            slotImage.sprite = sprite;
            defaultSprite    = sprite;
            currentCategory  = category;
        }
        else
        {
            slotImage.sprite = defaultGeneralImage;
            defaultSprite    = defaultGeneralImage;
            currentCategory  = category;
        }
    }

    /// <summary>역할: 소비 아이템 데이터 바인딩</summary>
    public void SetConsumeItemData(ConsumeItem item)
    {
        productName.text      = item.name_item;
        productPrice.text     = $"{item.price}";
        productImage.sprite   = item.icon;
        coinImageObject.sprite= coinImage;

        boundConsume = item;
        boundEquip   = null;
        boundPrice   = item.price;

        // 소비형은 공통 일반 프레임 사용
        defaultSprite = defaultGeneralImage;
        if (slotImage) slotImage.sprite = defaultSprite;

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClickSelect); // 역할: 공통 클릭 처리
    }

    /// <summary>역할: 장비 아이템 데이터 바인딩</summary>
    public void SetEquipItemData(EquipItem item)
    {
        productName.text      = item.name_item;
        productPrice.text     = $"{item.price}";
        productImage.sprite   = item.icon;
        coinImageObject.sprite= coinImage;

        boundConsume = null;
        boundEquip   = item;
        boundPrice   = item.price;

        // 장비는 직군별 기본 프레임을 사용(사전 지정 필요)
        if (slotImage && defaultSprite != null)
            slotImage.sprite = defaultSprite;

        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClickSelect); // 역할: 공통 클릭 처리
    }

    /// <summary>
    /// 역할: 상품 클릭 시 공통 처리
    ///  1) 이전 선택 해제
    ///  2) 현재를 선택 상태 비주얼로 전환
    ///  3) 선택 이벤트 발행 → StoreManager가 로컬/온라인 패널에 맞춰 정보 출력
    /// </summary>
    private void OnClickSelect()
    {
        if (currentSelectedProduct != null && currentSelectedProduct != this)
            currentSelectedProduct.ResetToDefaultImage();

        currentSelectedProduct = this;

        // 타입에 따라 선택 프레임 적용
        if (IsEquip && selectedSpriteDict != null && selectedSpriteDict.TryGetValue(currentCategory, out Sprite sel))
            slotImage.sprite = sel;
        else
            slotImage.sprite = selectGeneralImage;

        // ❗중요: 여기서 ItemInfoPanel을 직접 건드리지 않음
        // StoreManager가 OnAnyProductClicked를 받아 로컬/온라인 분기하여 패널을 채운다.
        OnAnyProductClicked?.Invoke(this);

        // 구매 버튼/골드 등 갱신
        var sm = FindObjectOfType<StoreManager>();
        if (sm != null) sm.UpdateApplyButtonState();
    }

    /// <summary>역할: 선택 해제 시 기본 프레임으로 복원</summary>
    public void ResetToDefaultImage()
    {
        slotImage.sprite = defaultSprite != null ? defaultSprite : defaultGeneralImage;
    }

    // ============== 온라인 구매 바인딩/표시 ===============

    /// <summary>역할: 온라인 listingId 보관</summary>
    public void BindListingId(string listingId) => boundListingId = listingId;

    /// <summary>역할: 온라인 가격 보관</summary>
    public void SetOnlinePrice(int price) => onlinePrice = price;

    /// <summary>역할: 온라인 수량 보관 + 배지 갱신</summary>
    public void SetOnlineQty(int qty)
    {
        onlineQty = Mathf.Max(0, qty);
        RefreshQtyBadge();
    }

    /// <summary>역할: 현재 셀의 listingId 조회</summary>
    public string GetListingId() => boundListingId;

    /// <summary>역할: 현재 셀의 온라인 표시가격 조회</summary>
    public int GetOnlinePrice() => onlinePrice;

    /// <summary>역할: 현재 셀의 온라인 남은 수량 조회</summary>
    public int GetOnlineQty() => onlineQty;

    /// <summary>
    /// 역할: 구매 성공 시 로컬 슬롯 수량 감소(즉시 UI 반영)
    ///  - 반환값: 감소 후 남은 수량
    /// </summary>
    public int DecreaseOnlineQty(int amount)
    {
        onlineQty = Mathf.Max(0, onlineQty - Mathf.Max(1, amount));
        RefreshQtyBadge();
        return onlineQty;
    }

    /// <summary>역할: 슬롯 하위 "Txt_Count"를 찾아 "수량: N" 표기</summary>
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
    /// 역할: 이 상품을 강제로 선택 상태로 만들고 비주얼을 즉시 반영
    ///  - 판매 모드에서 탭 전환 직후 선택이 풀리는 상황 방지용
    /// </summary>
    public void ForceSelectAsCurrent()
    {
        if (currentSelectedProduct != null && currentSelectedProduct != this)
            currentSelectedProduct.ResetToDefaultImage();

        currentSelectedProduct = this;

        if (IsEquip && selectedSpriteDict != null && selectedSpriteDict.TryGetValue(currentCategory, out Sprite sel))
            slotImage.sprite = sel;
        else
            slotImage.sprite = selectGeneralImage;
    }
}
