using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListUpManager : ListUIBase<Job>
{
    [Header("Main Tab State")]
    [SerializeField] Toggle employToggle;
    [SerializeField] Toggle growthToggle;
    [SerializeField] Toggle recoveryToggle;

    [Header("List Toggle")]
    [SerializeField] Toggle toggleSortByName;
    [SerializeField] Toggle toggleSortByJob;
    [SerializeField] Toggle toggleSortByLevel;

    [Header("Employ Object")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject employBtn;
    [SerializeField] private GameObject employPrice;

    [Header("Growth Object")]
    [SerializeField] private GameObject growthImage;

    [Header("Created Asset")]
    [SerializeField] private TestHero testHero;

    private enum SortType { Name, Job, Level }
    private SortType currentSortType = SortType.Name;

    public event Action<Job> OnOwnedHeroSelected;           // Growth 구독 이벤트
    public Job CurrentSelectedHero { get; private set; }     // 외부에서 선택 영웅 조회

    void Start()
    {
        toggleSortByName.onValueChanged.AddListener((isOn) => { if (isOn) ChangeSortType(SortType.Name); });
        toggleSortByJob.onValueChanged.AddListener((isOn) => { if (isOn) ChangeSortType(SortType.Job); });
        toggleSortByLevel.onValueChanged.AddListener((isOn) => { if (isOn) ChangeSortType(SortType.Level); });

        LoadList();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void LoadList()
    {
        List<Job> sortedList = new List<Job>(testHero.jobs);
        switch (currentSortType)
        {
            case SortType.Name:
                sortedList.Sort((a, b) => a.name_job.CompareTo(b.name_job));
                break;
            case SortType.Job:
                sortedList.Sort((a, b) => a.jobCategory.CompareTo(b.jobCategory));
                break;
            case SortType.Level:
                sortedList.Sort((a, b) => a.id_job.CompareTo(b.id_job));
                break;
        }

        foreach (var hero in sortedList)
            CreateButton(hero);
    }

    private void ChangeSortType(SortType sortType)
    {
        currentSortType = sortType;
        RefreshList();
    }

    protected override void OnSelected(Job hero)
    {
        CurrentSelectedHero = hero;

        if (growthToggle.isOn == true)
            OnOwnedHeroSelected?.Invoke(hero);
    }

    public void ResetButtonImage()
    {
        base.ResetSelectedButton();
        CurrentSelectedHero = null;
    }

    public void EmployPanelState(bool state)
    {
        infoPanel.SetActive(state);
        employBtn.SetActive(state);
    }

    public void PricePanelState(bool state)
    {
        employPrice.SetActive(state);
    }

    public void GrowthPanelState(bool state)
    {
        growthImage.SetActive(state);
    }

    public void RecoveryPanelState(bool state)
    {

    }

    protected override void SetLabel(Button button, Job hero)
    {
        TMP_Text nameText = button.transform.Find("Text_Name").GetComponent<TMP_Text>();
        TMP_Text jobText = button.transform.Find("Text_Job").GetComponent<TMP_Text>();
        TMP_Text levelText = button.transform.Find("Text_Level").GetComponent<TMP_Text>();

        nameText.text = hero.name_job;
        jobText.text = hero.name_job.ToString();
        levelText.text = $"Lv.{hero.level}";
    }

    public void RefreshList()
    {
        ClearList();
        LoadList();
    }
}
