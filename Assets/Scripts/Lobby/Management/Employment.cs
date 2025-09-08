using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Employment : ListUIBase<Job>
{
    [Header("Refs")]
    [SerializeField] private ListUpManager listUpManager;
    [SerializeField] private HeroListUp heroListUp;

    [Header("Set Prefab")]
    [SerializeField] private GameObject heroPricePrefab;
    [SerializeField] private Transform gridPricePanel;

    [Header("Extra Panel")]
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text employText;

    [Header("Extra Assets")]
    [SerializeField] private GoodsImage currencyImage;
    [SerializeField] private TestMoney testMoney;       // 임시 데이터
    [SerializeField] private TestHero testHero;
    [SerializeField] private TestHero DBHero;

    private List<Job> randomHeros;
    private Job selectedHero;
    private int heroPrice = 3;

    void Start()
    {
        LoadList();

        confirmButton.onClick.AddListener(() =>
        {

        });
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        confirmButton.onClick.AddListener(EmployHero);
        employText.text = "고용";
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(EmployHero);
    }

    List<Job> ShowEmployableHero(int level) // 매개 변수 = 슬롯 확장 단계
    {
        List<Job> copy = new List<Job>();

        foreach (var job in DBHero.jobs)
            copy.Add(job);

        List<Job> result = new List<Job>();

        for (int i = 0; i < copy.Count && i < level; i++)
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
        }

        return result;
    }

    protected override void LoadList()
    {
        randomHeros = ShowEmployableHero(2);
        foreach (var hero in randomHeros)
        {
            GameObject pricePanel = Instantiate(heroPricePrefab, gridPricePanel);
            Image image = pricePanel.transform.Find("image").GetComponent<Image>();
            TMP_Text price = pricePanel.transform.Find("price").GetComponent<TMP_Text>();

            switch (hero.jobCategory)
            {
                case JobCategory.Warrior:
                    image.sprite = currencyImage.warriorImage;
                    break;

                case JobCategory.Ranged:
                    image.sprite = currencyImage.rangeImage;
                    break;

                case JobCategory.Special:
                    image.sprite = currencyImage.specialImage;
                    break;

                case JobCategory.Healer:
                    image.sprite = currencyImage.healerImage;
                    break;
            }

            if (!testMoney.HasEnoughSoul(hero.jobCategory, 3)) price.color = Color.red;

            CreateButton(hero);
        }
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

    protected override void OnSelected(Job hero)
    {
        if (testMoney.HasEnoughSoul(hero.jobCategory, 3)) confirmButton.interactable = true;
        else confirmButton.interactable = false;

        selectedHero = hero;
        confirmButton.gameObject.SetActive(true);
        pricePanel.SetActive(true);
        listUpManager.EmployPanelState(true);
        ShowHeroInfo(hero);
    }

    void EmployHero()
    {
        testHero.jobs.Add(selectedHero);
        testMoney.PayHeroPrice(selectedHero.jobCategory, heroPrice);
        currentSelect.interactable = false;
        listUpManager.EmployPanelState(false);
        confirmButton.gameObject.SetActive(false);
        currentSelect = null;

        listUpManager.RefreshList();
        heroListUp.RefreshHeroList();
    }

    public void ResetButtonImage()
    {
        base.ResetSelectedButton();
    }
}
