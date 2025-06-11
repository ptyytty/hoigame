using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListUpManager : MonoBehaviour
{

    [Header("Set Button")]
    [SerializeField] private HeroButtonObject framePrefab;
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

    private Button currentSelected;

    void Start()
    {
        GetOwnedHeroList();
    }

    public void GetOwnedHeroList()
    {
        foreach (Job job in HeroManager.instance.GetAllJobs())
        {
            HeroButtonObject buttonObj = Instantiate(framePrefab, grid);  // frame은 HeroButtonObject 타입으로 바꿔야 함

            Button heroButton = buttonObj.button;
            buttonObj.heroJob.text = job.name_job;
            buttonObj.background.sprite = changedImage.defaultImage;

            Button capturedButton = buttonObj.button;
            Image capturedImage = buttonObj.background;

            capturedButton.onClick.AddListener(() =>
            {
                if (currentSelected == capturedButton)
                    return;

                if (currentSelected != null)
                    currentSelected.GetComponent<HeroButtonObject>().background.sprite = changedImage.defaultImage;

                capturedImage.sprite = changedImage.selectedImage;
                currentSelected = capturedButton;

                ShowHeroInfo(job);
            });
        }
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
