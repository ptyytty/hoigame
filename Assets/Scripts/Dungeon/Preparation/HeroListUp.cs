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

    public Button currentSelect;

    // delegate 정의 (형식 선언)
    public delegate void HeroSelectedHandler(Job selectedHero);
    // event 선언
    public event HeroSelectedHandler OnHeroSelected;


    void Start()
    {
        LoadHeroList();

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(heroListPanel) && !IsPointerOverUI(partyPanel))
            {
                ResetButtonImage();
                partySelector.ResetPartySlotInteractable();
                currentSelect = null;
            }
        }
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
            Button capturedButton = heroButton; //영웅 버튼
            Image capturedImage = buttonImage;  //선택 버튼 이미지

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

                OnHeroSelected?.Invoke(hero);

                // 여기서 PartySlot 등 UI 연동하면 됨
                //UpdatePartySlot((Loc)hero.loc);   event로 변경
                ShowHeroInfo(hero);
            });
        }
    }

    public void ResetButtonImage()
    {
        if (currentSelect == null)
            return;
        Image prevImage = currentSelect.GetComponent<Image>();
        prevImage.sprite = changedImage.defaultImage;
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

    public bool IsPointerOverUI(GameObject target)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == target || result.gameObject.transform.IsChildOf(target.transform))
            {
                return true;
            }
        }
        return false;
    }
}
