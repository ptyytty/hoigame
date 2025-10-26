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
    [SerializeField] private TMP_Text itemCountText;
    [SerializeField] private Image itemIcon;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        panel.SetActive(false);
    }

    /// <summary>
    /// 아이템 정보 표시 (공통 포맷)
    /// </summary>
    /// <param name="name">아이템 이름</param>
    /// <param name="description">설명 문구</param>
    /// <param name="price">가격</param>
    /// <param name="icon">아이콘 스프라이트</param>
    /// <param name="effects">효과 리스트(ItemEffectSpec)</param>
    public void ShowItemInfo(string name, string description, int price, Sprite icon, List<ItemEffectSpec> effects)
    {
        panel.SetActive(true);
        itemNameText.text = name;
        itemDescription.text = description;
        itemPriceText.text = price.ToString();
        itemIcon.sprite = icon;

        // 효과 요약
        itemEffect.text = (effects == null || effects.Count == 0)
            ? ""
            : effects.BuildEffectSummary();

        int count = 0;
        if (InventoryRuntime.Instance != null)
        {
            // InventoryRuntime에서 같은 이름의 아이템 탐색
            var consume = ItemCatalog.GetConsumeByName(name); // 없으면 아래 fallback
            if (consume != null)
                count = InventoryRuntime.Instance.GetConsumeItemCount(consume);
        }

        itemCountText.text = $"현재 보유량: {count}";
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
