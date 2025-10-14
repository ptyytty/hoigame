using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 던전 UI 애니메이션 제어
/// 스킬 텍스트 삽입
/// </summary>

[Serializable]
public class UIinfo
{
    public Button onButton;         // UI 여는 버튼
    public RectTransform panelPos;
    public float duration;          // 이동 속도
    public Vector2 hiddenPos;       // 숨겨지는 위치
    public Vector2 visiblePos;      // 보이는 위치
    [HideInInspector] public bool isOpen;
}

[Serializable]
public class SkillInfo
{
    public Image skillImage;
    public TMP_Text skillName;
    public TMP_Text skillDamage;
    public TMP_Text skillTarget;
    public TMP_Text skillRange;
    public TMP_Text description;
}

public class UIManager : MonoBehaviour
{
    [Header("Script")]
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private BattleManager battleManager;

    [Header("Skill Panels")]
    [SerializeField] private List<UIinfo> uiList = new List<UIinfo>();
    [SerializeField] private List<SkillInfo> skillInfos = new List<SkillInfo>();

    [Header("Battle Panels")]
    [SerializeField] private UIinfo infoPanel;
    [SerializeField] private UIinfo invenPanel;

    [Header("Info Panel - Hero")]
    [SerializeField] private Image heroImage;
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroLevel;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private HealthBarUI heroHpBar;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;

    [Header("Info Panel - Item & Effects")]
    [SerializeField] private GameObject itemRow;     // 아이템 줄 전체(없으면 꺼짐)
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private Transform buffRoot;    // 버프 칩 부모(초록)
    [SerializeField] private Transform debuffRoot;  // 디버프 칩 부모(빨강)
    [SerializeField] private GameObject effectTagPrefab; // 텍스트 하나 달린 간단 칩 프리팹

    // 텍스트/색상 지정
    private static readonly Color BuffTextColor = new Color(0.25f, 0.85f, 0.35f); // 초록
    private static readonly Color DebuffTextColor = new Color(0.95f, 0.30f, 0.30f); // 빨강

    private Job _currentHeroRef; // 현재 패널에 표시 중인 영웅 저장

    private UIinfo currentOpen;
    private bool _closingPanels;   // 재진입 방지 가드

    // Outline Toggle
    private readonly HashSet<Button> _wired = new();
    private int _selectedSkillIndex = -1;

    private readonly Dictionary<Button, int> _btnIndex = new();
    private List<Skill> _currentSkills = new();

    void Awake()
    {
        // 시작 시 전부 닫힌 위치로 세팅(트윈 없이 배치)
        foreach (var ui in uiList)
        {
            if (ui?.panelPos == null) continue;
            ui.panelPos.anchoredPosition = ui.hiddenPos;
            ui.isOpen = false;
        }

        // 위치 초기화
        if (invenPanel?.panelPos) invenPanel.panelPos.anchoredPosition = invenPanel.hiddenPos;
        if (infoPanel?.panelPos) infoPanel.panelPos.anchoredPosition = infoPanel.hiddenPos;

        if (invenPanel != null) invenPanel.isOpen = false;
        if (infoPanel != null) infoPanel.isOpen = false;

        currentOpen = null;
    }

    void Start()
    {
        for (int i = 0; i < uiList.Count; i++)
        {
            var ui = uiList[i];
            if (ui?.onButton == null) continue;

            ui.onButton.onClick.AddListener(() =>
            {
                ToggleExclusive(ui);
            });
        }

        infoPanel.onButton.onClick.AddListener(() => ToggleExclusive(infoPanel));

        invenPanel.onButton.onClick.AddListener(() => ToggleExclusive(invenPanel));
    }

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.OnTargetingStateChanged += HandleTargetingState;
            battleManager.OnSkillCommitted += HandleSkillCommitted; // 이미 썼다면 유지
        }
    }

    // 애니메이션 등 진행 중 아웃라인 비활성화
    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnTargetingStateChanged -= HandleTargetingState;
            battleManager.OnSkillCommitted -= HandleSkillCommitted;
        }
    }

    //======= 외부 공개 API =======
    public void ToggleExclusive(UIinfo ui)
    {
        if (ui == null || ui.panelPos == null) return;

        // 이미 내가 열려있으면 -> 닫고 종료
        if (currentOpen == ui && ui.isOpen)
        {
            CloseDrawer(ui);
            currentOpen = null;
            return;
        }

        // 다른 거 열려 있으면 전부 닫기
        CloseAll();

        // 대상 열기
        OpenDrawer(ui);
        currentOpen = ui;
    }

    public void CloseAll()
    {
        // 스킬 패널 전부 닫기
        foreach (var u in uiList)
            if (u != null && u.isOpen) CloseDrawer(u);

        // 정보/인벤 패널 닫기
        if (infoPanel != null && infoPanel.isOpen) CloseDrawer(infoPanel);
        if (invenPanel != null && invenPanel.isOpen) CloseDrawer(invenPanel);

        currentOpen = null; // ✅ 포인터 초기화
    }

    // 아웃라인 초기화
    public void ClearSkillSelection()
    {
        _selectedSkillIndex = -1;
    }

    //====== 전투용 통합 API ======
    // 영웅 정보 TMP_Text에 적용
    // ShowSkills(...) 수정
    public void ShowSkills(Job hero, IList<Skill> skills)
    {
        _currentSkills = skills?.ToList() ?? new List<Skill>();
        _btnIndex.Clear();

        for (int i = 0; i < uiList.Count; i++)
        {
            var ui = uiList[i];
            var slot = (i < skillInfos.Count) ? skillInfos[i] : null;
            if (ui?.onButton == null || slot == null) continue;

            if (i < _currentSkills.Count && _currentSkills[i] != null)
            {
                var skill = _currentSkills[i];

                // ===== 텍스트 채우기 =====
                string label = "피해: ";
                string value = "0";

                if (skill?.effects != null)
                {
                    var heal = skill.effects.OfType<HealEffect>().FirstOrDefault();
                    if (heal != null)
                    {
                        label = "회복";
                        value = heal.percent ? $"{Mathf.FloorToInt(heal.rate * 100)}%" : ReturnText.ReturnDamage(Mathf.Max(0, heal.amount));
                    }
                    else
                    {
                        var dmg = skill.effects.OfType<DamageEffect>().FirstOrDefault();
                        if (dmg != null)
                        {
                            label = "피해";
                            value = ReturnText.ReturnDamage(Mathf.Max(0, dmg.damage));
                        }
                        else
                        {
                            var sgn = skill.effects.OfType<SignDamageEffect>().FirstOrDefault();
                            if (sgn != null)
                            {
                                label = "표식 피해";
                                value = ReturnText.ReturnDamage(Mathf.Max(0, sgn.damage));
                            }
                        }
                    }

                }
                if (slot.skillName) slot.skillName.text = skill.skillName;
                if (slot.skillDamage) slot.skillDamage.text = $"{label}: {value}";
                if (slot.skillTarget) slot.skillTarget.text = $"대상: {ReturnText.ReturnTarget((int)skill.target)}";
                if (slot.skillRange) slot.skillRange.text = $"범위: {ReturnText.ReturnArea((int)skill.area)}";

                // ===== 버튼 바인딩 =====
                if (!_wired.Contains(ui.onButton))
                {
                    _wired.Add(ui.onButton);
                    ui.onButton.onClick.AddListener(() =>
                    {
                        // 내가 몇 번째 버튼인지 역추적
                        int idx = _btnIndex.TryGetValue(ui.onButton, out var bi) ? bi : -1;
                        if (idx < 0 || idx >= _currentSkills.Count) return;

                        var selected = _currentSkills[idx];
                        battleManager?.OnSkillClickedFromUI(selected);   // ★ 대상 지정 모드 진입
                    });
                }
                _btnIndex[ui.onButton] = i;
            }
            else
            {
                // 빈 슬롯: 텍스트 클리어/버튼 비활성 등 필요 시 처리
            }
        }
    }

    public void ShowHeroInfo(Job hero)
    {
        if (hero == null) return;

        _currentHeroRef = hero;

        heroName.text = $"{hero.name_job}";
        heroHp.text = $"{hero.hp}";
        heroLevel.text = $"Lv.{hero.level}";
        Combatant c = Combatant.FindByHero(hero);
        if (c != null)
        {
            heroDef.text = $"방어: {c.GetCurrentDefense()}";
            heroRes.text = $"저항: {c.GetCurrentResistance()}";
            heroSpd.text = $"민첩: {c.GetCurrentSpeed()}";
            heroHit.text = $"명중: {c.GetCurrentHit()}";

            if (heroHpBar) heroHpBar.Bind(c);
        }
        else
        {
            // Combatant를 못 찾은 경우의 안전망(원본)
            heroDef.text = $"방어: {hero.def}";
            heroRes.text = $"저항: {hero.res}";
            heroSpd.text = $"민첩: {hero.spd}";
            heroHit.text = $"명중: {hero.hit}";
        }

        RefreshItem(hero);
        RefreshEffects(hero);
    }

    //=========== 적용 중인 효과 ============
    // ① 효과 UI 갱신
    private void RefreshEffects(Job hero)
    {
        if (hero == null) return;

        ClearChildren(buffRoot);
        ClearChildren(debuffRoot);

        // Job 쪽 딕셔너리(Combatant.TickStatuses가 바로 이걸 틱해요) 
        var buffs = hero.BuffsDict;   // Dictionary<BuffType,int>
        var debuffs = hero.DebuffsDict; // Dictionary<BuffType,int>

        // 버프
        if (buffs != null)
        {
            foreach (var kv in buffs)
            {
                if (kv.Value <= 0) continue;
                CreateEffectTag(buffRoot, kv.Key, kv.Value, isDebuff: false);
            }
        }

        // 디버프
        if (debuffs != null)
        {
            foreach (var kv in debuffs)
            {
                if (kv.Value <= 0) continue;
                CreateEffectTag(debuffRoot, kv.Key, kv.Value, isDebuff: true);
            }
        }
    }

    private void CreateEffectTag(Transform parent, BuffType type, int turns, bool isDebuff)
    {
        if (parent == null || effectTagPrefab == null) return;

        var go = Instantiate(effectTagPrefab, parent);

        // 텍스트: "중독(3턴)" 형태로
        var label = go.GetComponentInChildren<TMP_Text>();
        if (label)
        {
            label.text = $"{Localize(type)} ({turns}턴)";
            label.color = isDebuff ? DebuffTextColor : BuffTextColor;
        }

        // (선택) 배경색도 바꾸고 싶다면 프리팹의 Image를 잡아서 색상 지정
        var bg = go.GetComponent<Image>();
        if (bg) bg.color = isDebuff
            ? new Color(1f, 0.45f, 0.45f, 0.35f)
            : new Color(0.45f, 1f, 0.55f, 0.35f);
    }

    private static string Localize(BuffType type)
    {
        switch (type)
        {
            case BuffType.Poison: return "중독";
            case BuffType.Bleeding: return "출혈";
            case BuffType.Burn: return "화상";
            case BuffType.Faint: return "기절";
            case BuffType.Sign: return "표식";
            case BuffType.Taunt: return "도발";

            case BuffType.Defense: return "방어";
            case BuffType.Resistance: return "저항";
            case BuffType.Speed: return "민첩";
            case BuffType.Hit: return "명중";
            case BuffType.Damage: return "피해증가";
            case BuffType.Heal: return "회복증가";

            default: return type.ToString(); // 미정 의존성은 영문 유지
        }
    }

    private void ClearChildren(Transform t)
    {
        if (!t) return;
        for (int i = t.childCount - 1; i >= 0; --i)
            Destroy(t.GetChild(i).gameObject);
    }

    //============ 장비 중인 아이템 ===============
    // ② 아이템 UI 갱신(참조 경로는 프로젝트마다 다르므로 안전 기본값으로)
    private void RefreshItem(Job hero)
    {
        // 기본은 감춤
        if (itemRow) itemRow.SetActive(false);

        // ===== 나중에 여기를 '한 줄'만 바꾸면 자동 표시됨 =====
        // 예시: Combatant에서 꺼내기
        // var c = Combatant.FindByHero(hero);
        // if (c && c.TryGetEquippedItem(out var equip) && equip != null) {
        //     itemRow.SetActive(true);
        //     if (itemName) itemName.text = equip.displayName;
        //     if (itemIcon) itemIcon.sprite = equip.icon;
        // }

        // 또는 hero.equippedItem 같이 DTO에 있다면 그 경로로 바꿔서 사용
    }

    //===============================
    void OpenDrawer(UIinfo ui)
    {
        if (ui.isOpen) return;

        ui.panelPos.DOKill();
        ui.panelPos
            .DOAnchorPos(ui.visiblePos, ui.duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        ui.isOpen = true;
    }

    void CloseDrawer(UIinfo ui)
    {
        if (!ui.isOpen) return;

        ui.panelPos.DOKill();
        ui.panelPos
            .DOAnchorPos(ui.hiddenPos, ui.duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        ui.isOpen = false;
    }

    //========= 스킬 사용 이벤트 및 애니메이션 제어 ===========
    void HandleTargetingState(bool on)
    {
        if (!on)
            CloseSkillPanelAndReset(); // 🔒 타게팅이 꺼지면 스킬 패널도 닫음(초기화)
    }

    // 스킬 적용 완료 알림 시 이벤트 적용 메소드
    void HandleSkillCommitted()
    {
        // 패널 닫기 유지
        CloseSkillPanelAndReset();

        // ★ 현재 보고 있던 영웅 수치/효과 재표시
        if (_currentHeroRef != null)
        {
            ShowHeroInfo(_currentHeroRef);
        }
    }

    void CloseSkillPanelAndReset()
    {
        if (_closingPanels) return;     // 🔒 재진입 방지
        _closingPanels = true;

        // 스킬 패널이 여러 개면 모두 닫기, 하나면 그 하나만 닫기
        foreach (var ui in uiList)
        {
            if (ui != null && ui.isOpen)
                CloseDrawer(ui);   // ← 네가 쓰는 기존 닫기 루틴
        }

        _closingPanels = false;
    }
}
