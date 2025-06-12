using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Employment : MonoBehaviour
{
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private Transform gridMainTab;
    [SerializeField] private ListUpManager listUpManager;

    [Header("Button Image")]
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;

    private List<Job> randomHeros;

    private Button currentSelected;

    void Start()
    {
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

            capturedButton.onClick.AddListener(() =>
            {
                if (currentSelected == capturedButton)
                    return;

                if (currentSelected != null)
                    ResetButtonImage();

                currentSelected = capturedButton;
                capturedImage.sprite = changedImage.selectedImage;
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
