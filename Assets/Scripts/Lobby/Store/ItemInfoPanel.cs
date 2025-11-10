using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// 아이템 정보창 관리
public class ItemInfoPanel : MonoBehaviour
{
    public static ItemInfoPanel instance;

    [System.Serializable]
    public class PanelRefs
    {
        public GameObject panelRoot;
        public TMP_Text itemNameText;
        public TMP_Text itemDescription;
        public TMP_Text itemEffect;
        public TMP_Text itemPriceText;
        public TMP_Text itemCountText;
        public Image itemIcon;
    }

    [Header("Panels")]
    [SerializeField] private PanelRefs local;   // 로컬 상점 전용 패널
    [SerializeField] private PanelRefs online;  // 온라인 상점 전용 패널

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // 시작 시 모두 끄기
        SafeSetActive(local, false);
        SafeSetActive(online, false);
    }

    /// <summary>역할: 로컬 상점 패널에 정보 표시 + 활성화</summary>
    public void ShowLocalItemInfo(string name, string description, int price, Sprite icon, List<ItemEffectSpec> effects)
    {
        FillPanel(local, name, description, price, icon, effects);
        SafeSetActive(online, false);
        SafeSetActive(local, true);
    }

    /// <summary>역할: 온라인 상점 패널에 정보 표시 + 활성화</summary>
    public void ShowOnlineItemInfo(string name, string description, int price, Sprite icon, List<ItemEffectSpec> effects, int? onlineRemainQty = null)
    {
        FillPanel(online, name, description, price, icon, effects, onlineRemainQty);
        SafeSetActive(local, false);
        SafeSetActive(online, true);
    }

    /// <summary>역할: 두 패널 모두 비활성화</summary>
    public void HideAll()
    {
        SafeSetActive(local, false);
        SafeSetActive(online, false);
    }

    /// <summary>역할: 특정 패널에 공통 포맷으로 정보 채우기</summary>
    private void FillPanel(PanelRefs p, string name, string description, int price, Sprite icon, List<ItemEffectSpec> effects, int? overrideCount = null)
    {
        if (p == null || p.panelRoot == null) return;

        p.panelRoot.SetActive(true);
        if (p.itemNameText)      p.itemNameText.text = name;
        if (p.itemDescription)   p.itemDescription.text = description;
        if (p.itemPriceText)     p.itemPriceText.text = price.ToString();
        if (p.itemIcon)          p.itemIcon.sprite = icon;

        if (p.itemEffect)
            p.itemEffect.text = (effects == null || effects.Count == 0) ? "" : effects.BuildEffectSummary();

        // 보유/남은 수량 표시: 온라인에선 서버 남은 수량, 로컬은 내 인벤토리 보유량
        int countToShow = 0;
        if (overrideCount.HasValue)
        {
            countToShow = Mathf.Max(0, overrideCount.Value);
        }
        else
        {
            if (InventoryRuntime.Instance != null)
            {
                var consume = ItemCatalog.GetConsumeByName(name);
                if (consume != null)
                    countToShow = InventoryRuntime.Instance.GetConsumeItemCount(consume);
                else
                    countToShow = 0; // 장비는 1개 기준이므로 별도 표기 안 함
            }
        }

        if (p.itemCountText)
            p.itemCountText.text = $"보유량: {countToShow}";
    }

    private void SafeSetActive(PanelRefs p, bool on)
    {
        if (p != null && p.panelRoot != null)
            p.panelRoot.SetActive(on);
    }
}
