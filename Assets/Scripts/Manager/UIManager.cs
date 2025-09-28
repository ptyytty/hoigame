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

    [Header("Info Panel")]
    [SerializeField] private Image heroImage;
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroLevel;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private HealthBarUI heroHpBar;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;

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
        battleManager.OnSkillCommitted       += HandleSkillCommitted; // 이미 썼다면 유지
    }
    }

    // 애니메이션 등 진행 중 아웃라인 비활성화
    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.OnTargetingStateChanged -= HandleTargetingState;
            battleManager.OnSkillCommitted       -= HandleSkillCommitted;
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
                int damage = skill.effects?.OfType<DamageEffect>().Sum(s => s.damage) ?? 0;
                if (slot.skillName) slot.skillName.text = skill.skillName;
                if (slot.skillDamage) slot.skillDamage.text = $"피해: {ReturnText.ReturnDamage(damage)}";
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

        heroName.text = $"{hero.name_job}";
        heroHp.text = $"{hero.hp}";
        heroLevel.text = $"Lv.{hero.level}";
        heroDef.text = $"방어: {hero.def}";
        heroRes.text = $"저항: {hero.res}";
        heroSpd.text = $"민첩: {hero.spd}";
        heroHit.text = $"명중: {hero.hit}";

        Combatant c = Combatant.FindByHero(hero);
        if (heroHpBar) heroHpBar.Bind(c);

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
        CloseSkillPanelAndReset();
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
