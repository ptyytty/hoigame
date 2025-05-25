using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Product : MonoBehaviour
{
    [SerializeField] private TMP_Text productName;
    [SerializeField] private TMP_Text productPrice;
    [SerializeField] private Image productImage;
    [SerializeField] private List<JobSpritePair> jobSpritePairs;

    private Dictionary<JobCategory, Sprite> spriteDict;
    private Image slotImage;
    private string effect;

    void Awake()
    {
        spriteDict = jobSpritePairs.ToDictionary(p => p.category, p => p.sprite);
        slotImage = GetComponent<Image>();
    }

    public void SetSlotImageByJob(JobCategory category)
    {
        if (spriteDict.TryGetValue(category, out Sprite sprite))
        {
            slotImage.sprite = sprite;
        }
    }

    public void SetConsumeItemData(ConsumeItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            ItemInfoPanel.instance.ShowItemInfo(item.name_item, item.description, item.buffTypes, null, item.value, item.price, item.icon);
        });
    }

    public void SetEquipItemData(EquipItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() =>
        {
            ItemInfoPanel.instance.ShowItemInfo(item.name_item, item.description, null, item.effectText, item.value, item.price, item.icon);
        });

    }
}
