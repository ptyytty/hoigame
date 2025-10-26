using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// 상점 UI 제어
public class StoreManager : MonoBehaviour
{
    [System.Serializable]
    public class ToggleImagepair    // 토글 버튼 정보
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
        public Text labelText;
        public Color selectedTextColor = new Color(238f / 255f, 190f / 255f, 20f / 255f, 1f);
        public Color defaultTextcolor = new Color(1f, 1f, 1f, 1f);
    }

    [Header("Toggle Images")]
    public List<ToggleImagepair> itemTypeToggleImagePairs;  // 아이템 종류 토글
    public List<ToggleImagepair> storeTypeToggleImagePairs; // 상점 토글
    public List<ToggleImagepair> changeBuyOrSellToggle;     // 구매 판매 토글

    [Header("Panels")]
    [SerializeField] private GameObject localStore, onlineStore;
    [SerializeField] private GameObject itemToggleGroup;
    [SerializeField] private GameObject onlineBackground;

    [Header("Toggle Group")]
    [SerializeField] GameObject onlineToggleGroup, onlineSell, onlineBuy;

    private Toggle lastSelectedItemType = null;
    private Toggle lastSelectedStoreType = null;
    private Toggle lastSelectedOnlineStoreMode = null;

    [Header("Button")]
    [SerializeField] Button applyBtn;

    void Start()
    {
        // 아이템 타입 토글 (전체, 장비, 소모)
        for (int i = 0; i < itemTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            itemTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(itemTypeToggleImagePairs[index].toggle, itemTypeToggleImagePairs, ref lastSelectedItemType);
                }
            });
        }

        // 상점 타입 토글 (로컬, 온라인)
        for (int i = 0; i < storeTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            // 클릭할 때, 토글의 isOn이 true가 될 때 전부 호출
            storeTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(storeTypeToggleImagePairs[index].toggle, storeTypeToggleImagePairs, ref lastSelectedStoreType);
                    ShowPannelByIndex(index);
                }
            });
        }

        // 구매/판매 전환 토글
        for (int i = 0; i < changeBuyOrSellToggle.Count; i++)
        {
            int index = i;

            changeBuyOrSellToggle[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(changeBuyOrSellToggle[index].toggle, changeBuyOrSellToggle, ref lastSelectedOnlineStoreMode);
                }
            });
        }

        // 기본값 초기화
        if (itemTypeToggleImagePairs.Count > 0)
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
        }

        if (storeTypeToggleImagePairs.Count > 0)
        {
            storeTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;
        }

        if (changeBuyOrSellToggle.Count > 0)
        {
            changeBuyOrSellToggle[0].toggle.isOn = true;
            lastSelectedOnlineStoreMode = changeBuyOrSellToggle[0].toggle;
        }

        UpdateToggle(itemTypeToggleImagePairs);
        UpdateToggle(storeTypeToggleImagePairs);
        UpdateToggle(changeBuyOrSellToggle);

        // 기본은 비활성화
        applyBtn.onClick.AddListener(OnClickApply);

        // ✅ 항상 싱글턴 기준으로 inventory 보정
        var inv = InventoryRuntime.Instance;
        if (inv != null)
        {
            InventoryRuntime.Instance.OnCurrencyChanged += UpdateApplyButtonState; // [역할] 재화 변동 시 버튼 재평가
        }

        UpdateApplyButtonState();
    }

    private void OnDestroy()
    {
        var inv = InventoryRuntime.Instance;
        if (inv != null) inv.OnCurrencyChanged -= UpdateApplyButtonState;
    }

    // 상점 타입 토글 전환에 따른 패널 변경
    public void ShowPannelByIndex(int index)
    {
        bool islocal = index == 0;
        bool isonline = index == 1;

        // 로컬 상점
        localStore.SetActive(islocal);
        if (islocal) // 로컬 상점 전환 시 전체 토글로 초기화 / 로비 갔다와도 초기화
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;

            storeTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;

            changeBuyOrSellToggle[0].toggle.isOn = true;
            lastSelectedOnlineStoreMode = changeBuyOrSellToggle[0].toggle;
        }

        // 온라인 상점
        onlineToggleGroup.SetActive(isonline);
        onlineStore.SetActive(isonline);
        onlineBackground.SetActive(isonline);
        itemToggleGroup.SetActive(isonline);
        ItemInfoPanel.instance.Hide();

        if (Product.CurrentSelected != null)
        {
            Product.CurrentSelected.ResetToDefaultImage();
        }

        // ✅ 아이템 정보창 닫기
        if (ItemInfoPanel.instance != null)
        {
            ItemInfoPanel.instance.Hide();
        }

        UpdateApplyButtonState();
    }


    // 토글 전환
    void OnToggleChanged(Toggle changedToggle, List<ToggleImagepair> toggleGroup, ref Toggle lastSelectedToggle)    //ref: lastSelectedToggle 참조 호출
    {
        if (changedToggle.isOn)
        {
            if (changedToggle != lastSelectedToggle)
            {
                lastSelectedToggle = changedToggle;
                UpdateToggle(toggleGroup);
            }
            else
            {
                changedToggle.isOn = true;
            }
        }
    }

    // 토글 버튼 이미지 변경
    void UpdateToggle(List<ToggleImagepair> toggleGroup)
    {
        foreach (var pair in toggleGroup)
        {
            bool isOn = pair.toggle.isOn;

            pair.image.sprite = pair.toggle.isOn ? pair.selectedSprite : pair.defaultSprite;

            if (pair.labelText != null) // labelText 여부에 따른 텍스트 색상 변경
            {
                Color targetColor = isOn ? pair.selectedTextColor : pair.defaultTextcolor;
                targetColor.a = 1f;
                pair.labelText.color = targetColor;
            }
        }
    }

    /// <summary>
    /// Apply 버튼 클릭 시 선택된 상품을 구매 확정
    /// </summary>
    private void OnClickApply()
    {
        var selected = Product.CurrentSelected;
        if (selected == null)
        {
            applyBtn.interactable = false;
            Debug.Log("[Store] 구매할 상품이 선택되지 않았습니다.");
            return;
        }

        var inv = InventoryRuntime.Instance;
        if (inv == null)
        {
            Debug.LogError("[Store] InventoryRuntime 인스턴스를 찾을 수 없습니다!");
            return;
        }

        int price = selected.Price;

        // ⚠️ 최종 가드: TrySpendGold가 false면 절대 진행하지 않음
        if (!inv.TrySpendGold(price))
        {
            Debug.Log("[Store] 골드 부족으로 구매 불가.");
            UpdateApplyButtonState(); // 남은 골드 기준으로 즉시 버튼 상태 반영
            return;
        }

        // 🛒 아이템 지급
        if (selected.IsConsume && selected.BoundConsume != null)
        {
            inv.AddConsumeItem(selected.BoundConsume, 1);
            Debug.Log($"[Store] {selected.BoundConsume.name_item}을(를) 1개 구매했습니다.");

            // 소비 아이템은 보유량 갱신된 정보 다시 표시
            ItemInfoPanel.instance.ShowItemInfo(
                selected.BoundConsume.name_item,
                selected.BoundConsume.description,
                selected.Price,
                selected.BoundConsume.icon,
                selected.BoundConsume.effects
            );
        }
        else if (selected.IsEquip && selected.BoundEquip != null)
        {
            inv.AddEquipItem(selected.BoundEquip);
            Debug.Log($"[Store] {selected.BoundEquip.name_item} 장비를 구매했습니다.");

            // 장비는 한 번만 구매 가능 → 버튼 비활성화 & 초기화
            var btn = selected.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            selected.ResetToDefaultImage();
            ItemInfoPanel.instance.Hide();

            // 선택 상태 해제
            typeof(Product)
                .GetField("currentSelectedProduct", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);
        }
        else
        {
            Debug.LogWarning("[Store] 선택된 상품이 유효하지 않습니다.");
            return;
        }

        // 저장
        if (PlayerProgressService.Instance != null)
            _ = PlayerProgressService.Instance.SaveAsync();

        // ⭐ 구매 후 버튼 상태 재평가(골드 변동 반영)
        UpdateApplyButtonState();
    }

    public void UpdateApplyButtonState()
    {
        var inv = InventoryRuntime.Instance;
        if (applyBtn == null || inv == null) return;

        var selected = Product.CurrentSelected;

        if (selected == null)
        {
            applyBtn.interactable = false;
            return;
        }

        int price = selected.Price;
        applyBtn.interactable = (inv.Gold >= price);
    }
}
