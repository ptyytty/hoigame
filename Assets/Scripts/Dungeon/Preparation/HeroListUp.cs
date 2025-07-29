using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class HeroListUp : ListUIBase<Job>
{
    [Header("Interact Panels")]
    [SerializeField] private PartySelector partySelector;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject heroListPanel;

    [Header("Hero Info Panel")]
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;


    [Header("Prefab")]
    [SerializeField] private Button heroButtonPrefab;

    [SerializeField] private ScrollRect scrollRect;

    [Header("Created Assets")]
    [SerializeField] private TestHero testHero;

    // delegate 정의 (형식 선언)
    public delegate void HeroSelectedHandler(Job selectedHero);
    // event 선언
    public event HeroSelectedHandler OnHeroSelected;

    void Start()
    {
        LoadList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void LoadList()
    {
        foreach (var hero in testHero.jobs)
            CreateButton(hero);

        currentSelect = null;
    }

    public void SetHeroButtonInteractableByLoc(int requiredLoc)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool canUse = dataList[i].loc == requiredLoc || dataList[i].loc == (int)Loc.None;
            buttons[i].interactable = canUse;
            Debug.Log(i);
        }
    }

    public void ResetHeroListState()
    {
        ResetSelectedButton(); // 선택된 버튼 이미지 복구
        SetAllButtonsInteractable(true); // 버튼 상호작용 복구
    }

    public void RefreshHeroList()
    {
        ClearList();
        LoadList();
    }

    public void ShowHeroInfo(Job hero)
    {
        heroName.text = $"{hero.name_job}";
        heroHp.text = $"{hero.hp}";
        heroDef.text = $"{hero.def}";
        heroRes.text = $"{hero.res}";
        heroSpd.text = $"{hero.spd}";
        heroHit.text = $"{hero.hit}";
    }

    protected override string GetLabel(Job hero)
    {
        return hero.name_job;
    }

    protected override void OnSelected(Job hero)
    {
        Debug.Log("선택됨!");
        OnHeroSelected?.Invoke(hero);

        ShowHeroInfo(hero);
    }

    // 이하 PartySelector 호출용 Public 메소드
    public void ResetButton()
    {
        ResetSelectedButton();
    }

    public void SetInteractable(bool state)
    {
        SetAllButtonsInteractable(state);
    }
}
