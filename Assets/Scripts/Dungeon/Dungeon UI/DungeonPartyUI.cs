using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


/// <summary>
/// 던전/전투 화면 중앙 2x2 파티 슬롯 전용 UI 매니저
/// - PartyBridge.Instance.ActiveParty 를 읽어 4칸에 표시
/// - 슬롯 클릭 시 UIManager.ShowHeroInfo(hero) 호출(좌측 정보/하단 장비 출력은 기존 UIManager가 처리)
/// - 전투 중 영웅 교체 기능은 없음(표시/선택 전용)
/// </summary>
public class DungeonPartyUI : MonoBehaviour
{
    [Serializable]
    public class SlotRefs
    {
        public Button button; // 클릭 영역(필수)
        public Image portrait; // 영웅 초상/아이콘(선택)
        public Sprite selectedSprite;

        [NonSerialized] public Sprite _originalSprite;
        [NonSerialized] public bool _hasCachedOriginal;
        [NonSerialized] public bool _selected;
    }

    [Header("Slots (2x2, index: 0~3)")]
    [Tooltip("왼쪽-앞(0), 오른쪽-앞(1), 왼쪽-뒤(2), 오른쪽-뒤(3) 순서 권장")]
    [SerializeField] private List<SlotRefs> slots = new List<SlotRefs>(4);

    [Header("Options")]
    [SerializeField] private bool autoPopulateFromPartyBridge = true; // OnEnable 시 자동 바인딩

    [Header("좌측: 영웅 정보 패널(던전 전용)")]
    [SerializeField] private Image infoHeroImage;
    [SerializeField] private TMP_Text infoName;
    [SerializeField] private TMP_Text infoLevel;
    [SerializeField] private TMP_Text infoHp;         // "현재/최대" 텍스트
    [SerializeField] private HealthBarUI infoHpBar;   // 너가 쓰는 HP Bar UI 스크립트
    [SerializeField] private TMP_Text infoDef;
    [SerializeField] private TMP_Text infoRes;
    [SerializeField] private TMP_Text infoSpd;
    [SerializeField] private TMP_Text infoHit;

    [Header("하단: 장비/아이템 표시(간단 버전)")]
    [SerializeField] private GameObject equipRow;     // 전체 줄(없으면 비활성)
    [SerializeField] private Image equipIcon;
    [SerializeField] private TMP_Text equipName;

    [Header("선택 표시 옵션")]
    [SerializeField] private bool autoSelectFirstOnEnable = false;

    // 내부 상태
    private readonly Job[] _heroes = new Job[4];
    private int _currentIndex = -1;


    private void OnEnable()
    {
        WireButtonsOnce();
        TryPopulateFromPartyBridge();
        ResetSelectionToNone();
    }


    private void OnDisable()
    {
        ResetSelectionToNone();
    }

    // ▶ 외부에서 직접 파티 주입 가능하도록 공개 메서드 추가
    public void ApplyHeroes(Job[] heroes)
    {
        for (int i = 0; i < _heroes.Length; i++)
            _heroes[i] = (heroes != null && i < heroes.Length) ? heroes[i] : null;

        ApplyHeroesToSlots();
        ResetSelectionToNone();
    }


    /// <summary>
    /// PartyBridge에서 현재 파티를 읽어 UI에 반영
    /// </summary>
    private void TryPopulateFromPartyBridge()
    {
        if (!autoPopulateFromPartyBridge) return;

        var party = PartyBridge.Instance?.ActiveParty; // 0~3
        for (int i = 0; i < 4; i++)
            _heroes[i] = (party != null && i < party.Count) ? party[i] : null;

        ApplyHeroesToSlots();
    }


    /// <summary>
    /// 외부에서 파티를 직접 세팅하고 싶을 때 사용(최대 4명 반영)
    /// </summary>
    private void ApplyHeroesToSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var hero = (i < _heroes.Length) ? _heroes[i] : null;

            if (hero == null)
            {
                if (s.portrait) s.portrait.enabled = false;
                continue;
            }

            if (s.portrait)
            {
                s.portrait.enabled = true;
                if (!s._hasCachedOriginal)
                {
                    s._originalSprite = s.portrait.sprite;
                    s._hasCachedOriginal = true;
                }
            }
        }
    }

    // --- 슬롯 클릭/선택 ------------------------------------------------------

    private void WireButtonsOnce()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i;
            var s = slots[i];
            if (s?.button == null) continue;

            // 중복리스너 방지
            s.button.onClick.RemoveAllListeners();
            s.button.onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    private void OnSlotClicked(int index)
    {
        if (index < 0 || index >= _heroes.Length) return;
        var hero = _heroes[index];
        if (hero == null) return;

        SetSelected(index);
        // 좌측/하단 패널 갱신(전투 X, 던전 전용)
        RefreshHeroInfoPanel(hero);
        RefreshEquipRow(hero);
    }

    private void SetSelected(int index)
    {
        if (_currentIndex == index) return;

        // 기존 선택 해제(원래 스프라이트 복구)
        if (_currentIndex >= 0 && _currentIndex < slots.Count)
        {
            var prev = slots[_currentIndex];
            if (prev != null)
            {
                if (prev._hasCachedOriginal && prev.portrait)
                    prev.portrait.sprite = prev._originalSprite;
                prev._selected = false;
            }
        }

        _currentIndex = index;

        // 새 선택: 선택 스프라이트로 교체
        if (_currentIndex >= 0 && _currentIndex < slots.Count)
        {
            var cur = slots[_currentIndex];
            if (cur != null && cur.portrait)
            {
                if (!cur._hasCachedOriginal)
                {
                    cur._originalSprite = cur.portrait.sprite;
                    cur._hasCachedOriginal = true;
                }
                if (cur.selectedSprite != null)
                    cur.portrait.sprite = cur.selectedSprite;

                cur._selected = true;
            }
        }
    }

    private void ResetSelectionVisualsOnly()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || s.portrait == null) continue;

            if (s._hasCachedOriginal)
                s.portrait.sprite = s._originalSprite;

            s._selected = false;
        }
    }

    private void ResetSelectionToNone()
    {
        ResetSelectionVisualsOnly();
        _currentIndex = -1;
        // 좌측/하단 패널 비움
        ClearHeroInfoPanel();
        ClearEquipRow();
    }

    private void SelectFirstValid()
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (_heroes[i] != null)
            {
                OnSlotClicked(i);
                return;
            }
        }
        ResetSelectionToNone();
    }

    // --- 좌측 정보 패널(던전 전용) ------------------------------------------

    private void RefreshHeroInfoPanel(Job hero)
    {
        // ★ 선택되었을 때는 보이게
        if (infoHeroImage) infoHeroImage.enabled = true;
        if (infoHpBar) infoHpBar.gameObject.SetActive(true);

        if (infoHeroImage) infoHeroImage.sprite = GetPortrait(hero);
        if (infoName) infoName.text = SafeName(hero);
        if (infoLevel) infoLevel.text = $"Lv.{GetLevel(hero)}";

        // HP
        int hp = GetHp(hero);
        int hpMax = GetHpMax(hero);
        if (infoHp) infoHp.text = $"{hp}/{hpMax}";
        if (infoHpBar) infoHpBar.Set(hp, hpMax);

        // 스탯(프로젝트의 Job/Combatant/Status 구조에 맞춰 아래 접근자만 매핑해주면 됨)
        if (infoDef) infoDef.text = $"방어: {GetDef(hero)}";
        if (infoRes) infoRes.text = $"저항: {GetRes(hero)}";
        if (infoSpd) infoSpd.text = $"민첩: {GetSpd(hero)}";
        if (infoHit) infoHit.text = $"명중: {GetHit(hero)}";

        // 버프/디버프, 상세 효과 등이 필요하면 여기서 추가로 채워줘
        // (현재 요구사항은 능력치 + 장비 노출이므로 최소구성만 반영)
    }

    private void ClearHeroInfoPanel()
    {
        // ★ 선택 전에는 완전히 숨김
        if (infoHeroImage)
        {
            infoHeroImage.enabled = false;   // 렌더 끔
            infoHeroImage.sprite = null;
        }

        if (infoHpBar)
        {
            infoHpBar.gameObject.SetActive(false); // 전체 비활성
            infoHpBar.Set(0, 1);                   // 내부 값은 기본값으로
        }

        if (infoName) infoName.text = "";
        if (infoLevel) infoLevel.text = "";
        if (infoHp) infoHp.text = "";  // HP 텍스트도 숨김 느낌을 주려면 빈 문자열 유지
        if (infoDef) infoDef.text = "";
        if (infoRes) infoRes.text = "";
        if (infoSpd) infoSpd.text = "";
        if (infoHit) infoHit.text = "";
    }

    // --- 하단 장비/아이템 ----------------------------------------------------

    private void RefreshEquipRow(Job hero)
    {
        if (equipRow) equipRow.SetActive(true);   // ★ 항상 켜둠

        var item = hero?.equippedItem;
        bool hasItem = item != null;

        if (equipIcon)
        {
            equipIcon.enabled = hasItem;          // ★ 내용만 토글
            if (hasItem) equipIcon.sprite = item.icon;
            else equipIcon.sprite = null;
        }

        if (equipName)
        {
            equipName.enabled = hasItem;          // ★ 내용만 토글
            equipName.text = hasItem
                ? (string.IsNullOrEmpty(item.name_item) ? "장비" : item.name_item)
                : ""; // 혹은 "장비 없음"
        }
    }

    private void ClearEquipRow()
    {
        if (equipRow) equipRow.SetActive(true);   // ★ 항상 켜둠

        if (equipIcon)
        {
            equipIcon.enabled = false;
            equipIcon.sprite = null;
        }
        if (equipName)
        {
            equipName.enabled = false;
            equipName.text = "";
        }
    }

    // --- 아래는 프로젝트 의존 Getter들을 한 곳에 모아둔 어댑터 ----------------
    // 네 Job/Combatant/Status 구조에 맞게만 매핑해주면, UI 로직은 그대로 재사용 가능

    // ---- 접근자 (Job 필드명은 네 프로젝트 기준) ----
    private static string SafeName(Job h) => h?.name_job ?? "Unknown";
    private static int GetLevel(Job h) => h?.level ?? 1;
    private static Sprite GetPortrait(Job h) => h?.portrait;
    private static int GetHp(Job h) => h?.hp ?? 0;
    private static int GetHpMax(Job h) => h?.maxHp ?? Math.Max(h?.hp ?? 0, 1);
    private static int GetDef(Job h) => h?.def ?? 0;
    private static int GetRes(Job h) => h?.res ?? 0;
    private static int GetSpd(Job h) => h?.spd ?? 0;
    private static int GetHit(Job h) => h?.hit ?? 0;

    private static Sprite GetEquippedItemSprite(Job hero)
    {
        // 예시: hero.equipMain?.icon
        return hero?.equippedItem?.icon;
    }

    private static string GetEquippedItemName(Job hero)
    {
        // 예시: hero.equipMain?.displayName
        return hero?.equippedItem?.name_item;
    }
}