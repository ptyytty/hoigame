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

        boundConsume = item;
        boundEquip = null;
        boundPrice = item.price;

        defaultSprite = defaultGeneralImage;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (currentSelectedProduct != null && currentSelectedProduct != this)
            {
                currentSelectedProduct.ResetToDefaultImage();
            }

            currentSelectedProduct = this;

            slotImage.sprite = selectGeneralImage;

            ItemInfoPanel.instance.ShowItemInfo(item.name_item,
                                                item.description,
                                                item.buffTypes,
                                                null,
                                                item.value,
                                                item.price,
                                                item.icon);
        });
    }

    public void SetEquipItemData(EquipItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;

        boundConsume = null;
        boundEquip = item;
        boundPrice = item.price;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (currentSelectedProduct != null && currentSelectedProduct != this)
            {
                currentSelectedProduct.ResetToDefaultImage();
            }

            currentSelectedProduct = this;

            if (selectedSpriteDict.TryGetValue(currentCategory, out Sprite selectedSprite))
            {
                slotImage.sprite = selectedSprite;
            }

            ItemInfoPanel.instance.ShowItemInfo(item.name_item,
                                                item.description,
                                                null,
                                                item.effectText,
                                                item.value,
                                                item.price,
                                                item.icon);
        });

    }

    public void ResetToDefaultImage()
    {
        if (currentSelectedProduct.defaultSprite != null)
        {
            slotImage.sprite = defaultSprite;
            return;
        }

        currentSelectedProduct.slotImage.sprite = defaultGeneralImage;
    }

}
