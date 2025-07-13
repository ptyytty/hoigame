using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Employment : MonoBehaviour
{
    [SerializeField] private ListUpManager listUpManager;
    [SerializeField] private HeroListUp heroListUp;

    [Header("Set Prefab")]
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private Transform gridMainTab;
    [SerializeField] private GameObject heroPricePrefab;
    [SerializeField] private Transform gridPricePanel;

    [Header("Extra Panel")]
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button employButton;

    [Header("Extra Assets")]
    [SerializeField] private GoodsImage goodsImage;
    [SerializeField] private TestMoney testMoney;       // 임시 데이터
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;
    [SerializeField] private TestHero testHero;

    private List<Job> randomHeros;
    private Job selectedHero;
    private int heroPrice = 3;
    private Button currentSelected;

    void Start()
    {
        Button employ = employButton.GetComponent<Button>();
        DisplayRandomHero();

        employ.onClick.AddListener(() =>
        {
            testHero.jobs.Add(selectedHero);
            testMoney.PayHeroPrice(selectedHero.jobCategory, heroPrice);
            currentSelected.interactable = false;
            listUpManager.EmployPanelState(false);
            employButton.gameObject.SetActive(false);
            currentSelected = null;

            listUpManager.RefreshHeroList();
            heroListUp.RefreshHeroList();
        });
    }

    List<Job> ShowEmployableHero(int level) // 매개 변수 = 슬롯 확장 단계
    {
        List<Job> copy = HeroManager.instance.GetAllJobs();
        List<Job> result = new List<Job>();

        for (int i = 0; i < copy.Count && i < level; i++)
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
        }

        return result;
    }

    void DisplayRandomHero()
    {
        randomHeros = ShowEmployableHero(2);

        foreach (Job job in randomHeros)
        {
            Button heroButton = Instantiate(heroButtonPrefab, gridMainTab);

            Image buttonBackground = heroButton.GetComponent<Image>();
            Image heroImage = heroButton.GetComponentInChildren<Image>();
            TMP_Text heroName = heroButton.transform.Find("Text_Name").GetComponent<TMP_Text>();
            TMP_Text heroJob = heroButton.transform.Find("Text_Job").GetComponent<TMP_Text>();
            TMP_Text heroLevel = heroButton.transform.Find("Text_Level").GetComponent<TMP_Text>();

            buttonBackground.sprite = changedImage.defaultImage;
            heroJob.text = job.name_job;

            Button capturedButton = heroButton;
            Image capturedImage = buttonBackground;

            GameObject pricePanel = Instantiate(heroPricePrefab, gridPricePanel);
            Image image = pricePanel.transform.Find("image").GetComponent<Image>();
            TMP_Text price = pricePanel.transform.Find("price").GetComponent<TMP_Text>();

            switch (job.jobCategory)
            {
                case JobCategory.Warrior:
                    image.sprite = goodsImage.warriorImage;
                    break;

                case JobCategory.Ranged:
                    image.sprite = goodsImage.rangeImage;
                    break;

                case JobCategory.Special:
                    image.sprite = goodsImage.specialImage;
                    break;

                case JobCategory.Healer:
                    image.sprite = goodsImage.healerImage;
                    break;
            }

            if (!testMoney.HasEnoughSoul(job.jobCategory, 3)) price.color = Color.red;

            capturedButton.onClick.AddListener(() =>
            {
                if (currentSelected == capturedButton)
                    return;

                if (currentSelected != null)
                    ResetButtonImage();

                if (testMoney.HasEnoughSoul(job.jobCategory, 3)) employButton.interactable = true;
                else employButton.interactable = false;

                currentSelected = capturedButton;
                capturedImage.sprite = changedImage.selectedImage;
                selectedHero = job;

                employButton.gameObject.SetActive(true);
                pricePanel.SetActive(true);
                listUpManager.EmployPanelState(true);
                listUpManager.ShowHeroInfo(job);
            });
        }
    }

    public void ResetButtonImage()
    {
        if (currentSelected == null) return;
        Image prevImage = currentSelected.GetComponent<Image>();
        prevImage.sprite = changedImage.defaultImage;
        currentSelected = null;
    }
}
