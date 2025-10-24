using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 역할: 던전 화면에서 "소비 아이템 사용" 워크플로 제어(힐 아이템 우선)
/// 1) 인벤토리 슬롯 클릭 → 아이템 정보 패널 갱신
/// 2) 영웅 슬롯 클릭 → 대상 선택, '사용' 버튼 활성화
/// 3) 사용 버튼 클릭 → 선택 영웅에게 회복 적용 + 인벤토리 1개 소모
/// </summary>
public class DungeonItemController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DungeonInventoryBinder inventoryBinder;  // 슬롯 클릭 이벤트를 받음
    [SerializeField] private DungeonInventory dungeonInventory;       // 실제 소모(RemoveItemAt)에 사용
    [SerializeField] private DungeonPartyUI partyUI;                  // 대상 영웅 선택 이벤트를 받음

    [Header("Item Info Panel")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text itemDetail;     // ex) +20 회복 같은 상세

    [Header("Actions")]
    [SerializeField] private Button useButton;

    // 내부 상태
    private int _selectedSlotIndex = -1;
    private ConsumeItem _selectedItem = null;
    private Job _selectedHero = null;

    void Awake()
    {
        // 안전 연결
        if (!dungeonInventory) dungeonInventory = FindObjectOfType<DungeonInventory>(true);

        // 버튼 리스너
        if (useButton)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnClickUse);
            useButton.gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        if (inventoryBinder != null)
            inventoryBinder.OnSlotClicked += OnInventorySlotClicked;

        if (partyUI != null)
            partyUI.OnHeroSelected += OnHeroSelected;
    }

    void OnDisable()
    {
        if (inventoryBinder != null)
            inventoryBinder.OnSlotClicked -= OnInventorySlotClicked;

        if (partyUI != null)
            partyUI.OnHeroSelected -= OnHeroSelected;
    }

    // 역할: 인벤토리 슬롯 클릭 시 선택 아이템을 기억하고 패널 갱신
    private void OnInventorySlotClicked(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;
        _selectedItem = TryGetConsumeItemAt(slotIndex);

        RefreshItemPanel(_selectedItem);
        RefreshUseButton();
    }

    // 역할: 영웅 슬롯 클릭 시 대상 영웅을 기억하고 버튼 상태 갱신
    private void OnHeroSelected(int index, Job hero)
    {
        _selectedHero = hero;
        RefreshItemPanel(_selectedItem);
        RefreshUseButton();
    }

    // 영웅 선택 초기화
    public void ClearHeroSelection()
    {
        _selectedHero = null;
        RefreshUseButton();      // 버튼 가시성 갱신
    }

    // 역할: '사용' 버튼 클릭 → 힐 아이템만 적용 후 1개 소모
    private void OnClickUse()
    {
        if (_selectedItem == null || _selectedHero == null || _selectedSlotIndex < 0) return;

        // 1) 힐 양 계산(여러 Heal spec이 있으면 모두 합산)
        int healTotal = ComputeTotalHeal(_selectedItem, _selectedHero);
        if (healTotal <= 0)
        {
            Debug.Log("[DungeonItemUse] 선택한 아이템은 회복 효과가 없습니다.");
            return;
        }

        // 2) 힐 적용 (던전/비전투 기준: Combatant 없이 Job.hp 직접 회복)
        int before = _selectedHero.hp;
        int after = Mathf.Clamp(_selectedHero.hp + healTotal, 0, Mathf.Max(1, _selectedHero.maxHp));

        _selectedHero.hp = Mathf.Clamp(_selectedHero.hp + healTotal, 0, Mathf.Max(1, _selectedHero.maxHp));
        Debug.Log($"[DungeonItemUse] {_selectedItem.name_item} 사용: +{healTotal} HP ({before}→{_selectedHero.hp})");

        if (partyUI)
        {
            partyUI.PlayHealAnimation(_selectedHero, before, _selectedHero.hp); // 프리뷰→커밋 애니메이션
        }

        StartCoroutine(CoCommitHeal(after));

        // 3) 인벤토리 1개 소모
        bool removed = dungeonInventory != null && dungeonInventory.RemoveItemAt(_selectedSlotIndex);
        if (!removed) Debug.LogWarning("[DungeonItemUse] 인벤토리 소모 실패");

        // 4) UI 즉시 갱신(영웅 패널/체력바 반영)
        //    DungeonPartyUI는 슬롯 클릭 시 갱신하므로, 현재 선택된 영웅을 다시 한번 강제로 반영하려면:
        //    partyUI.ShowHeroInfo(hero) 등의 메서드가 있다면 호출. 여기선 선택 이벤트만으로도 충분.
        //    (필요하면 DungeonPartyUI에 'ForceRefresh()' 같은 헬퍼를 추가)

        // 패널/버튼 상태 갱신
        RefreshItemPanel(TryGetConsumeItemAt(_selectedSlotIndex)); // 수량 표시용이라면 바인더에서 해줌
        RefreshUseButton();
    }

    // 회복 코루틴
    private IEnumerator CoCommitHeal(int after)
    {
        // HealthBarUI.PlayHealAnimation의 preview+commit 시간과 맞춰 대기
        yield return new WaitForSecondsRealtime(0.45f); // preview 0.25f + commit 0.2f 기본값

        // 이제 모델을 갱신(다른 UI들이 동기화해도 프리뷰는 이미 끝났음)
        if (_selectedHero != null)
            _selectedHero.hp = after;

        // (선택) HP 텍스트가 다른 패널에도 쓰인다면 여기서 한 번 더 가볍게 텍스트만 갱신
        // partyUI.ForceRefreshHpTextOnly(_selectedHero);  // 필요 시 구현
    }

    // --- 내부 유틸 ---

    /// <summary>역할: 슬롯 인덱스의 ConsumeItem을 조회(빈칸이면 null)</summary>
    private ConsumeItem TryGetConsumeItemAt(int slotIndex)
    {
        if (dungeonInventory == null) return null;
        var list = dungeonInventory.GetSlots();
        if (slotIndex < 0 || slotIndex >= list.Count) return null;
        var slot = list[slotIndex];
        return slot.IsEmpty ? null : slot.item;
    }

    /// <summary>역할: 아이템 정보 패널을 갱신</summary>
    private void RefreshItemPanel(ConsumeItem item)
    {
        if (itemIcon)
        {
            itemIcon.sprite = (item != null) ? item.icon : null;
            bool on = (item != null);
            itemIcon.enabled = on;
            itemIcon.gameObject.SetActive(on);
        }
        if (itemName)
        {
            if (item != null)
            {
                itemName.text = item.name_item;
                itemName.enabled = true;
                itemName.gameObject.SetActive(true);
            }
            else
            {
                itemName.text = "";
                itemName.enabled = false;
                itemName.gameObject.SetActive(false);
            }
        }

        if (itemDetail)
        {
            if (item == null || item.effects == null)
            {
                itemDetail.text = "";
                itemDetail.enabled = false;              // 🔽 비활성
                itemDetail.gameObject.SetActive(false);
            }
            else
            {
                int flat = 0;
                float pct = 0f;
                foreach (var e in item.effects)
                {
                    if (e.op != EffectOp.Heal) continue;
                    if (e.percent) pct += e.rate;
                    else flat += e.value;
                }
                string detail = "";
                if (flat > 0) detail += $"+{flat} 회복";
                if (pct > 0f) detail += ((detail.Length > 0) ? " / " : "") + $"{Mathf.RoundToInt(pct * 100)}% 회복";

                itemDetail.text = detail;
                bool show = !string.IsNullOrEmpty(detail);
                itemDetail.enabled = show;               // 🔼 활성
                itemDetail.gameObject.SetActive(show);
            }
        }
    }

    // 아이템 정보 패널 초기화
    public void ClearItemPanel()
    {
        if (itemIcon)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemName)
        {
            itemName.text = "";
            itemName.enabled = false;
            itemName.gameObject.SetActive(false);
        }

        if (itemDetail)
        {
            itemDetail.text = "";
            itemDetail.enabled = false;
            itemDetail.gameObject.SetActive(false);
        }

        if (useButton)
            useButton.gameObject.SetActive(false);

        // 사용 버튼 감춤(둘 다 선택돼야만 보이도록 기본은 감춤)
        if (useButton) useButton.gameObject.SetActive(false);
    }

    // 아이템 선택 초기화
    public void ClearItemSelection()
    {
        _selectedSlotIndex = -1;
        _selectedItem = null;
        RefreshItemPanel(null);  // 즉시 숨김/클리어
        RefreshUseButton();      // 버튼 가시성 갱신
    }

    // 선택 정보 초기화 및 UI 정리
    public void ClearAllSelectionsAndPanel()
    {
        _selectedSlotIndex = -1;
        _selectedItem = null;
        _selectedHero = null;
        ClearItemPanel();      // 뷰도 함께 정리
    }

    /// <summary>역할: 버튼 활성 조건(아이템 선택 + 영웅 선택 + 힐 효과 존재)</summary>
    private void RefreshUseButton()
    {
        if (!useButton) return;
        bool ok = (_selectedItem != null && _selectedHero != null && HasHeal(_selectedItem));
        useButton.gameObject.SetActive(ok);
    }

    /// <summary>역할: 아이템에 힐 효과가 있는가</summary>
    private bool HasHeal(ConsumeItem item)
    {
        if (item?.effects == null) return false;
        for (int i = 0; i < item.effects.Count; i++)
            if (item.effects[i].op == EffectOp.Heal) return true;
        return false;
    }

    /// <summary>역할: 총 힐량(퍼센트+고정 합산) 계산</summary>
    private int ComputeTotalHeal(ConsumeItem item, Job target)
    {
        if (item?.effects == null || target == null) return 0;

        int total = 0;
        foreach (var e in item.effects)
        {
            if (e.op != EffectOp.Heal) continue;

            if (e.percent)
            {
                int add = Mathf.Max(1, Mathf.FloorToInt(target.maxHp * e.rate));
                total += add;
            }
            else
            {
                total += Mathf.Max(0, e.value);
            }
        }
        return total;
    }
}
