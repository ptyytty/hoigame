using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;


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
    [SerializeField] private TMP_Text equipEffect;

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

            if (s?.portrait == null)
                continue;

            if (hero == null)
            {
                // 빈 슬롯
                s.portrait.sprite = null;
                s.portrait.enabled = false;
                continue;
            }

            // 영웅 초상 스프라이트 세팅
            var sprite = ResolvePortrait(hero); // 아래 헬퍼 참고
            s.portrait.sprite = sprite;
            s.portrait.enabled = (sprite != null);

            // 필요 시 한 번만 캐시(현재는 사용 안 함)
            if (!s._hasCachedOriginal)
            {
                s._originalSprite = s.portrait.sprite;
                s._hasCachedOriginal = true;
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

        // 이전 슬롯: 비주얼 리셋(이미지는 건드리지 않음)
        if (_currentIndex >= 0 && _currentIndex < slots.Count)
        {
            var prev = slots[_currentIndex];
            if (prev != null)
            {
                // portrait.sprite를 원본으로 되돌리는 로직은 제거
                prev._selected = false;
            }
        }

        _currentIndex = index;

        // 현재 슬롯: 선택 플래그만 갱신, portrait는 유지
        if (_currentIndex >= 0 && _currentIndex < slots.Count)
        {
            var cur = slots[_currentIndex];
            if (cur != null && cur.portrait)
            {
                // 선택 상태 표시가 필요하면 버튼 배경/외곽선에서 처리 권장
                // cur.button.image.sprite = cur.selectedSprite; 등으로 분리 가능(원하면 추가 안내해줄게요)
                cur.portrait.enabled = (cur.portrait.sprite != null); // 클릭해도 계속 보이도록 안전 복구
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

            // s._hasCachedOriginal가 있어도 portrait.sprite는 건드리지 않음
            s._selected = false;
        }
    }

    private void ResetSelectionToNone()
    {
        ResetSelectionVisualsOnly();
        _currentIndex = -1;
        if (infoHpBar) infoHpBar.Unbind();
        ClearHeroInfoPanel();
        ClearEquipRow();
    }

    /// <summary>
    /// [역할] 영웅 구조가 달라도 portrait/face/icon 순으로 안전하게 스프라이트를 찾아 반환
    /// </summary>
    private Sprite ResolvePortrait(Job hero)
    {
        if (hero == null) return null;

        // 가장 흔한 필드
        if (hero.portrait != null) return hero.portrait;

        // 프로젝트마다 다른 네이밍 대비
        // 존재하지 않는다면 주석 처리해도 무방
        // if (hero.face != null) return hero.face;
        // if (hero.icon != null) return hero.icon;

        return null;
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

        // ★ 변경: Combatant 우선 바인딩
        var c = Combatant.FindByHero(hero);
        if (c != null && infoHpBar && infoHpBar.TryBind(c))
        {
            // 바인딩되면 HealthBarUI가 OnHpChanged로 계속 동기화함
            if (infoHp) infoHp.text = $"{c.currentHp}/{Mathf.Max(1, c.maxHp)}";
        }
        else
        {
            // Combatant가 없으면 Job 데이터로 1회 세팅
            int hp = hero?.hp ?? 0;
            int hpMax = Mathf.Max(1, hero?.maxHp ?? 1);
            if (infoHp) infoHp.text = $"{hp}/{hpMax}";
            if (infoHpBar) infoHpBar.Set(hp, hpMax);  // 기존 로직
        }

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
            infoHpBar.Unbind();
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

        // [역할] 아이템 효과 출력(EquipInfoBox 동일 규칙)
        if (equipEffect) equipEffect.text = hasItem ? BuildEquipEffectsText(item) : "";
    }

    private void ClearEquipRow()
    {
        if (equipRow) equipRow.SetActive(true);
        if (equipIcon) { equipIcon.enabled = false; equipIcon.sprite = null; }
        if (equipName) { equipName.enabled = false; equipName.text = ""; }
        if (equipEffect) equipEffect.text = "";
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


    // =========== 아이템 효과 유틸 ================
    /// <summary>
    /// [역할] 장비의 효과 목록을 읽어 EquipInfoBox와 동일한 형식의 문자열로 변환
    /// - 프로젝트별 아이템/효과 타입 차이를 고려해 리플렉션으로 안전 접근
    /// </summary>
    private string BuildEquipEffectsText(object equipped)
    {
        if (equipped == null) return "";

        // effects(List<...>) 찾기
        var effectsField = equipped.GetType().GetField("effects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var effectsProp = equipped.GetType().GetProperty("effects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var effectsObj = effectsField != null ? effectsField.GetValue(equipped) :
                           effectsProp != null ? effectsProp.GetValue(equipped, null) : null;

        if (effectsObj is System.Collections.IEnumerable == false || effectsObj == null)
            return "";

        var sb = new System.Text.StringBuilder();

        foreach (var eff in (System.Collections.IEnumerable)effectsObj)
        {
            // 지속효과만 노출
            bool persistent = GetBool(eff, "persistent");
            if (!persistent) continue;

            // op: AbilityMod / Special
            string opName = GetEnumName(eff, "op");
            if (opName == "AbilityMod")
            {
                // stat: BuffType, value: int
                string statLabel = MapStatToLabel(GetEnumName(eff, "stat"));
                int value = GetInt(eff, "value");
                string sign = value >= 0 ? "+" : "";
                sb.AppendLine($"{statLabel} {sign}{value}");
            }
            else if (opName == "Special")
            {
                string key = GetString(eff, "specialKey");
                sb.AppendLine(MapSpecialKey(key));
            }
            // 다른 op는 무시 (EquipInfoBox와 동일 정책)
        }

        return sb.ToString();
    }

    /// <summary> [역할] 리플렉션 유틸: bool 안전 추출 </summary>
    private static bool GetBool(object o, string name)
    {
        var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(bool)) return (bool)f.GetValue(o);
        var p = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(bool)) return (bool)p.GetValue(o, null);
        return false;
    }
    /// <summary> [역할] 리플렉션 유틸: int 안전 추출 </summary>
    private static int GetInt(object o, string name)
    {
        var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int)) return (int)f.GetValue(o);
        var p = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(int)) return (int)p.GetValue(o, null);
        return 0;
    }
    /// <summary> [역할] 리플렉션 유틸: string 안전 추출 </summary>
    private static string GetString(object o, string name)
    {
        var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(string)) return (string)f.GetValue(o);
        var p = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(string)) return (string)p.GetValue(o, null);
        return null;
    }
    /// <summary> [역할] 리플렉션 유틸: enum 값을 이름 문자열로 </summary>
    private static string GetEnumName(object o, string name)
    {
        var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType.IsEnum) return f.GetValue(o).ToString();
        var p = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType.IsEnum) return p.GetValue(o, null).ToString();
        return null;
    }

    /// <summary> [역할] EquipInfoBox와 동일: 특수키 한글 라벨 매핑 </summary>
    private static string MapSpecialKey(string key)
    {
        switch (key)
        {
            case "Immune_Stun": return "기절 면역";
            case "Immune_Bleed": return "출혈 면역";
            case "Immune_Burn": return "화상 면역";
            case "Immune_Faint": return "기절 면역";
            default: return string.IsNullOrEmpty(key) ? "특수 효과" : key;
        }
    }

    /// <summary> [역할] EquipInfoBox와 동일: BuffType → 한글 라벨 </summary>
    private static string MapStatToLabel(string enumName)
    {
        switch (enumName)
        {
            case "Defense": return "방어";
            case "Resistance": return "저항";
            case "Speed": return "민첩";
            case "Hit": return "명중";
            case "Damage": return "공격";
            case "Heal": return "회복량";
            default: return enumName ?? "효과";
        }
    }
}