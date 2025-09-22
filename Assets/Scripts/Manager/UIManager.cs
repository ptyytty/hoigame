using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [Header("Skill Panels")]
    [SerializeField] private List<UIinfo> uiList = new List<UIinfo>();
    [SerializeField] private List<SkillInfo> skillInfos = new List<SkillInfo>();

    [Header("Battle Panels")]
    [SerializeField] private UIinfo invenPanel;

    private UIinfo currentOpen;
    private Skill currentSkill;
    private Job currentHero;
    private Action<Job, Skill, Job> onConfirmSkill;

    void Awake()
    {
        // 시작 시 전부 닫힌 위치로 세팅(트윈 없이 배치)
        foreach (var ui in uiList)
        {
            if (ui?.panelPos == null) continue;
            ui.panelPos.anchoredPosition = ui.hiddenPos;
            ui.isOpen = false;
        }
        currentOpen = null;
    }

    void Start()
    {
        for (int i = 0; i < uiList.Count; i++)
        {
            var ui = uiList[i];
            if (ui?.onButton == null) continue;

            ui.onButton.onClick.AddListener(() => ToggleUI(ui));
        }
    }

    //======= 외부 공개 API =======
    public void ToggleUI(UIinfo ui)
    {
        if (ui == null || ui.panelPos == null) return;

        // 1) 이미 열려 있는 같은 패널이면 닫기(토글)
        if (currentOpen == ui && ui.isOpen)
        {
            CloseDrawer(ui);
            return;
        }

        // 2) 다른 패널이 열려 있다면 먼저 닫고
        if (currentOpen != null && currentOpen != ui && currentOpen.isOpen)
            CloseDrawer(currentOpen);

        // 3) 대상 패널이 닫혀 있으면 열기
        if (!ui.isOpen)
            OpenDrawer(ui);
    }

    public void Open(UIinfo ui)
    {
        if (ui == null || ui.panelPos == null) return;

        if (currentOpen != null && currentOpen != ui && currentOpen.isOpen)
            CloseDrawer(currentOpen);

        if (!ui.isOpen)
            OpenDrawer(ui);
    }

    public void Close(UIinfo ui)
    {
        if (ui == null || ui.panelPos == null) return;
        if (ui.isOpen) CloseDrawer(ui);
    }

    public void CloseAll()
    {
        foreach (var ui in uiList) if (ui != null && ui.isOpen) CloseDrawer(ui);
        currentOpen = null;
    }

    //====== 전투용 통합 API ======
    // 영웅 정보 TMP_Text에 적용
    public void ShowSkills(Job hero, IList<Skill> skills)
    {
        var list = skills.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            var skill = list[i];

            int damage = skill.effects?
                        .OfType<DamageEffect>()
                        .Sum(s => s.damage) ?? 0;   // DamageEffect null이면 0

            skillInfos[i].skillName.text = skill.skillName;
            skillInfos[i].skillDamage.text = $"피해: {ReturnText.ReturnDamage(damage)}";
            skillInfos[i].skillTarget.text = $"대상: {ReturnText.ReturnTarget((int)skill.target)}";
            skillInfos[i].skillRange.text = $"범위: {ReturnText.ReturnArea((int)skill.area)}";
        }
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
        currentOpen = ui;          // 🔸 전역 하나만 열림 보장용
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
        if (currentOpen == ui) currentOpen = null;
    }
}
