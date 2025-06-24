using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListUpManager : MonoBehaviour
{
    [Header("Main Tab State")]
    [SerializeField] Toggle employToggle;
    [SerializeField] Toggle growthToggle;
    [SerializeField] Toggle recoveryToggle;

    [Header("Set Button")]
    [SerializeField] private Button framePrefab;
    [SerializeField] private Transform grid;

    [Header("Button Image")]
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;

    [Header("Hero Info Panel")]
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;

    [Header("Created Asset")]
    [SerializeField] private TestHero testHero;

    private Button currentSelected;
    private enum SortType {Name, Job, Level}

    void Start()
    {
        GetOwnedHeroList();
    }

    public void GetOwnedHeroList()
    {
        foreach (Job job in testHero.jobs)
        {
            Button heroButton = Instantiate(framePrefab, grid);

            Image buttonBackground = heroButton.GetComponent<Image>();
            Image heroImage = heroButton.GetComponentInChildren<Image>();
            TMP_Text heroName = heroButton.transform.Find("Text_Name").GetComponent<TMP_Text>();
            TMP_Text heroJob = heroButton.transform.Find("Text_Job").GetComponent<TMP_Text>();
            TMP_Text heroLevel = heroButton.transform.Find("Text_Level").GetComponent<TMP_Text>();

            buttonBackground.sprite = changedImage.defaultImage;
            heroJob.text = job.name_job;

            Button capturedButton = heroButton;
            Image capturedImage = buttonBackground;

            capturedButton.onClick.AddListener(() =>
            {
                if (employToggle.isOn == true) return;

                if (currentSelected == capturedButton)
                    return;

                if (currentSelected != null)
                    ResetButtonImage();

                if (growthToggle.isOn == true)
                    SelectHeroGrowth();

                currentSelected = capturedButton;
                capturedImage.sprite = changedImage.selectedImage;
            });
        }
    }

    void SelectHeroGrowth()
    {
        
    }

    public void RefreshHeroList()
    {
        foreach (Transform child in grid)
        {
            Destroy(child.gameObject);
        }

        GetOwnedHeroList();
    }

    public void ResetButtonImage()
    {
        if (currentSelected == null) return;
        Image prevImage = currentSelected.GetComponent<Image>();
        prevImage.sprite = changedImage.defaultImage;
        currentSelected = null;
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
}
