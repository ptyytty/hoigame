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

    [Header("Prefab")]
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Created Assets")]
    [SerializeField] private TestHero testHero;

    // delegate 정의 (형식 선언)
    public delegate void HeroSelectedHandler(Job selectedHero);
    // event 선언
    public event HeroSelectedHandler OnHeroSelected;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshHeroList();
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

    protected override void SetLabel(Button button, Job hero)
    {
        TMP_Text nameText = button.transform.Find("Text_Name").GetComponent<TMP_Text>();
        TMP_Text jobText = button.transform.Find("Text_Job").GetComponent<TMP_Text>();
        TMP_Text levelText = button.transform.Find("Text_Level").GetComponent<TMP_Text>();
        
        nameText.text  = hero.name_job;
        jobText.text   = hero.name_job.ToString();
        levelText.text = $"Lv.{hero.id_job}";
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
