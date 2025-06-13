using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class HeroListUp : MonoBehaviour
{
    [Header("List Panel")]
    [SerializeField] private Transform contentParent;
    [Header("Interact Panels")]
    [SerializeField] private PartySelector partySelector;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject heroListPanel;

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

    [Header("Created Assets")]
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;
    [SerializeField] private TestHero testHero;



    private Button currentSelect;
    private List<Button> heroButtons = new();
    private List<Job> heroDatas = new();

    // delegate 정의 (형식 선언)
    public delegate void HeroSelectedHandler(Job selectedHero);
    // event 선언
    public event HeroSelectedHandler OnHeroSelected;


    void Start()
    {
        LoadHeroList();

    }

    void OnEnable()
    {
        // 초기화 콜백 등록
        var clickHandler = FindObjectOfType<UIClickResetHandler>();
        if (clickHandler != null)
            clickHandler.RegisterResetCallback(ResetButtonImage);
    }


    void LoadHeroList()
    {
        foreach (var hero in testHero.jobs)
        {
            Button heroButton = Instantiate(heroButtonPrefab, contentParent);
            TMP_Text heroName = heroButton.GetComponentInChildren<TMP_Text>();
            Image buttonImage = heroButton.GetComponent<Image>();

            heroName.text = hero.name_job;

            // 버튼이 클릭되었을 때 어떤 Sprite를 조작할지를 보존하기 위해 로컬 변수로 캡처
            Button capturedButton = heroButton; //영웅 버튼
            Image capturedImage = buttonImage;  //선택 버튼 이미지

            heroButtons.Add(capturedButton);
            heroDatas.Add(hero);

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
                    ResetButtonImage();
                }

                // 새로운 선택
                capturedImage.sprite = changedImage.selectedImage;
                currentSelect = capturedButton;

                // PartySelector와 연결
                OnHeroSelected?.Invoke(hero);

                ShowHeroInfo(hero);
            });
        }
    }

    public void SetHeroButtonInteractableByLoc(int requiredLoc)
    {
        for (int i = 0; i < heroButtons.Count; i++)
        {
            bool canUse = heroDatas[i].loc == requiredLoc || heroDatas[i].loc == (int)Loc.None;
            heroButtons[i].interactable = canUse;
        }
    }

    public void SetAllHeroButtonsInteractable(bool state)
    {
        foreach (var btn in heroButtons)
        {
            btn.interactable = state;
        }
    }

    public void ResetHeroListState()
    {
        ResetButtonImage(); // 선택된 버튼 이미지 복구
        SetAllHeroButtonsInteractable(true); // 버튼 상호작용 복구
    }

    public void ResetButtonImage()
    {
        if (currentSelect == null) return;
        Image prevImage = currentSelect.GetComponent<Image>();
        prevImage.sprite = changedImage.defaultImage;
        currentSelect = null;
    }

    public void RefreshHeroList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        heroButtons.Clear();
        heroDatas.Clear();
        currentSelect = null;

        LoadHeroList();
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
