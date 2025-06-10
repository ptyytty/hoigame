using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListUpManager : MonoBehaviour
{
    [System.Serializable]
    public class HeroButtonFrame
    {
        public Button framePrefab;
        public Image frameBackground;
        public Image frameHeroImage;
        public TMP_Text frameHeroName;
        public TMP_Text frameHeroJob;
        public TMP_Text frameHeroLevel;
    }

    [Header("Set Button")]
    [SerializeField] private HeroButtonFrame frame;
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

    public void GetOwnedHeroList()
    {
        foreach (Job job in HeroManager.instance.GetAllJobs())
        {
            Button heroButton = Instantiate(frame.framePrefab, grid);
            Image buttonBackground = frame.frameBackground.GetComponent<Image>();
            Image heroImage = frame.frameHeroImage.GetComponent<Image>();
            TMP_Text heroName = frame.frameHeroName.GetComponent<TMP_Text>();
            TMP_Text heroJob = frame.frameHeroJob.GetComponent<TMP_Text>();
            TMP_Text heroLevel = frame.frameHeroLevel.GetComponent<TMP_Text>();

            heroJob.text = job.name_job;

            Button capturedButton = heroButton;
            Image capturedImage = buttonBackground;

            capturedImage.sprite = changedImage.defaultImage;

            capturedButton.onClick.AddListener(() =>
            {
                if (currentSelected == capturedButton)
                    return;

                // 기존 선택된 버튼이 있으면 이미지 복원
                if (currentSelected != null)
                {

                }

                // 새로운 선택
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
