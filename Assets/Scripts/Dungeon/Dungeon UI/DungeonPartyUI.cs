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
    [SerializeField] private bool autoPopulateFromPartyBridge = true; // [역할] OnEnable 때 PartyBridge에서 자동 주입
    [SerializeField] private bool autoSelectFirstOnEnable = false;     // [역할] 활성화 시 첫 유효 슬롯 자동 선택

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

    [Header("전투 패널 연동")]
    [SerializeField] private UIManager battleUIManager;     // [역할] 슬롯 선택 시 전투용 패널(UIManager)도 동일 영웅으로 갱신/바인딩

    public event System.Action<int, Job> OnHeroSelected;    // 선택된 영웅 외부 알림
    public Job GetSelectedHero() => (_currentIndex >= 0) ? _heroes[_currentIndex] : null;   // 현재 선택 영웅 조회

    // 내부 상태
    private readonly Job[] _heroes = new Job[4];
    private int _currentIndex = -1;


    private void OnEnable()
    {
        WireButtonsOnce();
        TryPopulateFromPartyBridge();
        if (autoSelectFirstOnEnable) SelectFirstValid();
        else ResetSelectionToNone();
    }


    private void OnDisable()
    {
        ResetSelectionToNone();
    }

    // [역할] 외부에서 파티 배열을 넘겨와 슬롯/패널을 채움(최대 4명)
    public void ApplyHeroes(Job[] heroes)
    {
        for (int i = 0; i < _heroes.Length; i++)
            _heroes[i] = (heroes != null && i < heroes.Length) ? heroes[i] : null;

        ApplyHeroesToSlots();
        if (autoSelectFirstOnEnable) SelectFirstValid();
        else ResetSelectionToNone();
    }

    // [역할] PartyBridge에서 현재 파티를 읽어 UI에 반영
    private void TryPopulateFromPartyBridge()
    {
        if (!autoPopulateFromPartyBridge) return;

        var party = PartyBridge.Instance?.ActiveParty;
        for (int i = 0; i < 4; i++)
            _heroes[i] = (party != null && i < party.Count) ? party[i] : null;

        ApplyHeroesToSlots();
    }

    // [역할] 슬롯의 초상 활성/원본 스프라이트 캐시
    private void ApplyHeroesToSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            var hero = (i < _heroes.Length) ? _heroes[i] : null;

            if (hero == null)
            {
                if (s?.portrait) s.portrait.enabled = false;
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

    // [역할] 버튼 리스너 연결(중복 방지)
    private void WireButtonsOnce()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int idx = i;
            var s = slots[i];
            if (s?.button == null) continue;

            s.button.onClick.RemoveAllListeners();
            s.button.onClick.AddListener(() => OnSlotClicked(idx));
        }
    }

    // [역할] 슬롯 선택 → 던전 HUD 갱신(현재값만) + 전투용 UIManager에 동일 영웅 전달
    private void OnSlotClicked(int index)
    {
        if (index < 0 || index >= _heroes.Length) return;
        var hero = _heroes[index];
        if (hero == null) return;

        SetSelected(index);
        RefreshHeroInfoPanel(hero);  // 던전 HUD: Set(hp,max)만 호출(프리뷰 X) :contentReference[oaicite:3]{index=3}
        RefreshEquipRow(hero);

        // ★ 전투 패널(UIManager)도 같은 영웅으로 동기화(실시간 바인딩/프리뷰는 UIManager 쪽에서)
        if (battleUIManager) battleUIManager.ShowHeroInfo(hero);  // 내부에서 TryBind(c) 수행 :contentReference[oaicite:4]{index=4}

        OnHeroSelected?.Invoke(index, hero);
    }

    // 선택 슬롯 인덱스
    public int IndexOfHero(Job hero)
    {
        if (hero == null) return -1;
        if (_currentIndex >= 0 && _currentIndex < _heroes.Length && _heroes[_currentIndex] == hero)
            return _currentIndex;

        for (int i = 0; i < _heroes.Length; i++)
            if (_heroes[i] == hero)
                return i;

        return -1;
    }

    // 해당 영웅을 선택 슬롯으로 강제 선택
    public void SelectHero(Job hero)
    {
        int idx = IndexOfHero(hero);
        if (idx >= 0) OnSlotClicked(idx); // 내부에서 RefreshHeroInfoPanel 호출
    }

    // 선택/정보 UI 초기화 시키는 공개 메서드
    public void ResetSelectionAndPanel()
    {
        ResetSelectionToNone();
    }

    // [역할] 선택 비주얼 토글
    private void SetSelected(int index)
    {
        if (_currentIndex == index) return;

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

    // ---------- 던전 전용 “현재값만” 갱신 ----------
    private void RefreshHeroInfoPanel(Job hero)
    {
        if (infoHeroImage) infoHeroImage.enabled = true;
        if (infoHpBar) infoHpBar.gameObject.SetActive(true);

        if (infoHeroImage) infoHeroImage.sprite = hero?.portrait;
        if (infoName) infoName.text = hero?.name_job ?? "-";
        if (infoLevel) infoLevel.text = $"Lv.{(hero?.level ?? 1)}";

        int hp = hero?.hp ?? 0;
        int hpMax = Mathf.Max(1, hero?.maxHp ?? 1);
        if (infoHp) infoHp.text = $"{hp}/{hpMax}";
        if (infoHpBar) infoHpBar.Set(hp, hpMax); // ★ 프리뷰 없이 현재값만 적용 :contentReference[oaicite:5]{index=5}

        if (infoDef) infoDef.text = $"방어: {hero?.def ?? 0}";
        if (infoRes) infoRes.text = $"저항: {hero?.res ?? 0}";
        if (infoSpd) infoSpd.text = $"민첩: {hero?.spd ?? 0}";
        if (infoHit) infoHit.text = $"명중: {hero?.hit ?? 0}";
    }

    private void ClearHeroInfoPanel()
    {
        if (infoHeroImage)
        {
            infoHeroImage.enabled = false;
            infoHeroImage.sprite = null;
        }

        if (infoHpBar)
        {
            infoHpBar.gameObject.SetActive(false);
            infoHpBar.Set(0, 1);
        }

        if (infoName) infoName.text = "";
        if (infoLevel) infoLevel.text = "";
        if (infoHp) infoHp.text = "";
        if (infoDef) infoDef.text = "";
        if (infoRes) infoRes.text = "";
        if (infoSpd) infoSpd.text = "";
        if (infoHit) infoHit.text = "";
    }

    private void RefreshEquipRow(Job hero)
    {
        if (equipRow) equipRow.SetActive(true);

        var item = hero?.equippedItem;
        bool hasItem = item != null;

        if (equipIcon)
        {
            equipIcon.enabled = hasItem;
            equipIcon.sprite = hasItem ? item.icon : null;
        }

        if (equipName)
        {
            equipName.enabled = hasItem;
            equipName.text = hasItem ? (string.IsNullOrEmpty(item.name_item) ? "장비" : item.name_item) : "";
        }
    }

    private void ClearEquipRow()
    {
        if (equipRow) equipRow.SetActive(true);
        if (equipIcon) { equipIcon.enabled = false; equipIcon.sprite = null; }
        if (equipName) { equipName.enabled = false; equipName.text = ""; }
    }

    /// <summary>
    /// 역할: HP 바에 '이전→이후'로 회복 애니메이션을 재생하고, 패널을 즉시 최신값으로 동기화
    /// </summary>
    public void PlayHealAnimation(Job hero, int beforeHp, int afterHp, float previewDur = 0.25f, float commitDur = 0.2f)
    {
        if (hero == null) return;

        int max = Mathf.Max(1, hero.maxHp);

        if (infoHpBar)
        {
            infoHpBar.Set(beforeHp, max);
            infoHpBar.ShowPreviewToAnimated(afterHp, HealthBarUI.PreviewType.Heal, previewDur);
            StartCoroutine(CoCommitAfter(previewDur, commitDur));
        }

        // ⚠️ 여기서 RefreshHeroInfoPanel(hero)를 즉시 호출하면
        //    infoHpBar.Set(hp,max)가 다시 불려 프리뷰(초록)가 사라집니다.
        //    따라서 텍스트만 조용히 반영하고, 바는 HealthBarUI가 알아서 움직이게 둡니다.
        if (infoHp) infoHp.text = $"{afterHp}/{max}";
    }

    private System.Collections.IEnumerator CoCommitAfter(float delay, float commitDur)
    {
        // 프리뷰가 끝날 때까지 기다렸다가
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        // 이제 현재 바만 부드럽게 따라 올라감
        if (infoHpBar) infoHpBar.CommitPreview(commitDur);
    }
}