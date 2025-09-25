using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// 아이템 정보창 관리
public class ItemInfoPanel : MonoBehaviour
{
    public static ItemInfoPanel instance;  // 싱글톤
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescription;
    [SerializeField] private TMP_Text itemEffect;
    [SerializeField] private TMP_Text itemPriceText;
    [SerializeField] private Image itemIcon;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        panel.SetActive(false);
    }

    public void ShowItemInfo(string name, string description, List<ConsumeBuffType> consumeEffect, string equipEffect, int value, int price, Sprite icon)
    {
        panel.SetActive(true);
        itemNameText.text = name;
        itemDescription.text = description;
        itemPriceText.text = price.ToString();
        itemIcon.sprite = icon;
        if (consumeEffect != null)
        {

            itemEffect.text = $"{string.Join(", ", consumeEffect.Select(bt => bt.ToKorean()))} +{value}";
        }
        if (equipEffect != null)
            itemEffect.text = $"{equipEffect} +{value}";
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
