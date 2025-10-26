using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

///<summary>
/// 상품 오브젝트
/// </summary>
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

    void Awake()
    {
        spriteDict = jobSpritePairs.ToDictionary(p => p.category, p => p.sprite);
        selectedSpriteDict = selectedSpritePairs.ToDictionary(p => p.category, p => p.sprite);
        slotImage = GetComponent<Image>();
    }

    // 버튼 직업 분류 및 기본 이미지 저장
    public void SetSlotImageByJob(JobCategory category)
    {
        if (spriteDict.TryGetValue(category, out Sprite sprite))
        {
            slotImage.sprite = sprite;
            defaultSprite = sprite;
            currentCategory = category;
        }
    }

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

            // 신규 시그니처: 공통 포맷으로 전달
            ItemInfoPanel.instance.ShowItemInfo(
            item.name_item,
            item.description,
            item.price,
            item.icon,
            item.effects
        );

            FindObjectOfType<StoreManager>()?.UpdateApplyButtonState();
        });
    }

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

            FindObjectOfType<StoreManager>()?.UpdateApplyButtonState();
        });

    }

    public void ResetToDefaultImage()
    {
        slotImage.sprite = defaultSprite != null ? defaultSprite : defaultGeneralImage;
    }

}
