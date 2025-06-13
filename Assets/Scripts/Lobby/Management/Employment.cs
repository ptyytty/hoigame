using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Employment : MonoBehaviour
{
    [SerializeField] private ListUpManager listUpManager;

    [Header("Set Prefab")]
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private Transform gridMainTab;
    [SerializeField] private GameObject heroPricePrefab;
    [SerializeField] private Transform gridPricePanel;

    [Header("Extra Panel")]
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Button employButton;

    [Header("Extra Assets")]
    [SerializeField] private GoodsImage goodsImage;
    [SerializeField] private TestMoney testMoney;       // 임시 데이터
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;

    private List<Job> randomHeros;

    private Button currentSelected;

    void Start()
    {
        Button employ = employButton.GetComponent<Button>();
        DisplayRandomHero();
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
                    if (testMoney.redSoul < 3)
                    {
                        price.color = Color.red;
                        employButton.interactable = false;
                    }
                    else
                    {
                        employButton.interactable = true;
                    }
                    break;

                case JobCategory.Ranged:
                    image.sprite = goodsImage.rangeImage;
                    if (testMoney.blueSoul < 3)
                    {
                        price.color = Color.red;
                        employButton.interactable = false;
                    }
                    else
                    {
                        employButton.interactable = true;
                    }
                    break;

                case JobCategory.Special:
                    image.sprite = goodsImage.specialImage;
                    if (testMoney.purpleSoul < 3)
                    {
                        price.color = Color.red;
                        employButton.interactable = false;
                    }
                    else
                    {
                        employButton.interactable = true;
                    }
                    break;

                case JobCategory.Healer:
                    image.sprite = goodsImage.healerImage;
                    if (testMoney.greenSoul < 3)
                    {
                        price.color = Color.red;
                        employButton.interactable = false;
                    }
                    else
                    {
                        employButton.interactable = true;
                    }
                    break;
            }

            capturedButton.onClick.AddListener(() =>
            {
                if (currentSelected == capturedButton)
                    return;

                if (currentSelected != null)
                    ResetButtonImage();

                if (testMoney.redSoul < 3) employButton.interactable = false;

                currentSelected = capturedButton;
                capturedImage.sprite = changedImage.selectedImage;

                employButton.gameObject.SetActive(true);
                pricePanel.SetActive(true);
                infoPanel.SetActive(true);
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

    void ControlEmployButton(Job job)
    {
        
    }
}
