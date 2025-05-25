using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HeroListUp : MonoBehaviour
{
    [Header("List Panel")]
    [SerializeField] private Transform contentParent;

    [Header("Party List")]
    [SerializeField] private Image leftFront;
    [SerializeField] private Image rightFront;
    [SerializeField] private Image leftBack;
    [SerializeField] private Image rightBack;

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

    [Header("Button Images")]
    [SerializeField] private ChangedImage changedImage;

    [CreateAssetMenu(fileName = "UIAssets", menuName = "Game/Preparation Asset Collection")]
    public class ChangedImage : ScriptableObject
    {
        public Sprite defaultImage;
        public Sprite selectedImage;
        public Sprite deactivationImage;
    }

    private Button currentSelect;

    void Start()
    {
        LoadHeroList();

    }

    void LoadHeroList()
    {
        foreach (var hero in DBManager.instance.jobData.jobs)
        {
            Button heroButton = Instantiate(heroButtonPrefab, contentParent);
            TMP_Text heroName = heroButton.GetComponentInChildren<TMP_Text>();
            Image buttonImage = heroButton.GetComponent<Image>();

            heroName.text = hero.name_job;

            // 버튼이 클릭되었을 때 어떤 Sprite를 조작할지를 보존하기 위해 로컬 변수로 캡처
            Button capturedButton = heroButton;
            Image capturedImage = buttonImage;

            // 초기 이미지 설정
            capturedImage.sprite = changedImage.defaultImage;

            capturedButton.onClick.AddListener(() =>
            {
                // 이미 선택된 버튼이면 무시
                if (currentSelect == capturedButton)
                    return;

                // 기존 선택된 버튼이 있으면 이미지 복원
                if (currentSelect != null)
                {
                    Image prevImage = currentSelect.GetComponent<Image>();
                    prevImage.sprite = changedImage.defaultImage;
                }

                // 새로운 선택
                capturedImage.sprite = changedImage.selectedImage;
                currentSelect = capturedButton;

                // 여기서 PartySlot 등 UI 연동하면 됨
                UpdatePartySlot((Loc)hero.loc);
                ShowHeroInfo(hero);
            });
        }
    }

    void UpdatePartySlot(Loc loc)
    {
        if (loc == (Loc)Loc.Front)
        {
            leftFront.sprite = changedImage.defaultImage;
            rightFront.sprite = changedImage.defaultImage;

            leftBack.sprite = changedImage.deactivationImage;
            rightBack.sprite = changedImage.deactivationImage;
        }
        else if (loc == (Loc)Loc.Back)
        {
            leftBack.sprite = changedImage.defaultImage;
            rightBack.sprite = changedImage.defaultImage;

            leftFront.sprite = changedImage.deactivationImage;
            rightFront.sprite = changedImage.deactivationImage;
        }
        else
        {
            leftFront.sprite = changedImage.defaultImage;
            rightFront.sprite = changedImage.defaultImage;

            leftBack.sprite = changedImage.defaultImage;
            rightBack.sprite = changedImage.defaultImage;
        }
    }

    void ShowHeroInfo(Job hero)
    {
        heroName.text = $"{hero.name_job}";
        heroHp.text = $"{hero.hp}";
        heroDef.text = $"{hero.def}";
        heroRes.text = $"{hero.res}";
        heroSpd.text = $"{hero.spd}";
        heroHit.text = $"{hero.hit}";
    }


}
