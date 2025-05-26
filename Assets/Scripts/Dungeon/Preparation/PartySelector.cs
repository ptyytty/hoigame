using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartySelector : MonoBehaviour
{

    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private HeroListUp.ChangedImage changedImage;

    [Header("Party Slots")]
    [SerializeField] private List<GameObject> partySlots;

    [Header("Position")]
    [SerializeField] private Image leftFront;
    [SerializeField] private Image rightFront;
    [SerializeField] private Image leftBack;
    [SerializeField] private Image rightBack;

    private List<Button> slotButtons = new();
    private List<GameObject> heroImages = new();
    private List<Image> slotImages = new();
    private Job[] assignedHeroes = new Job[4];

    // 선택된 슬롯 번호
    private int? selectedSlotIndex = null;
    // 선택된 영웅
    private Job selectedHero = null;

    public Button currentSlot;

    void OnEnable()
    {
        // ✅ 이벤트 구독 (등록)
        heroListUp.OnHeroSelected += UpdatePartySlot;
        heroListUp.OnHeroSelected += OnHeroSelectedFromList;
        SetupSlots();
    }

    void OnDisable()
    {
        // 🧹 이벤트 해제 (필수)
        heroListUp.OnHeroSelected -= UpdatePartySlot;
    }

    // Party 슬롯 설정
    void SetupSlots()
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            int index = i;
            Button btn = partySlots[i].GetComponent<Button>();
            Image slotImage = partySlots[i].GetComponent<Image>();
            Transform heroImage = partySlots[i].transform.Find("HeroImage");

            slotButtons.Add(btn);
            heroImages.Add(heroImage.gameObject);
            slotImages.Add(slotImage);



            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                // Debug.Log(capturedSlot);
                // if (currentSlot == capturedSlot)
                //     return;

                // if (currentSlot != null)
                // {
                //     Image prevImage = currentSlot.GetComponent<Image>();
                //     prevImage.sprite = changedImage.defaultImage;
                // }
                // slotImage.sprite = changedImage.selectedImage;
                // currentSlot = capturedSlot;
                OnSlotClicked(index);
            });
        }
    }

    void OnSlotClicked(int index)
    {
        selectedSlotIndex = index;

        Button capturedButton = slotButtons[index];

        if (selectedHero != null)
        {
            AssignHeroToSlot(index, selectedHero);
            ResetSelection(selectedSlotIndex.Value);
        }
        else if (currentSlot == capturedButton)
        {
            return;
        }
        else if (currentSlot != null)
        {
            Image prevImage = currentSlot.GetComponent<Image>();
            prevImage.sprite = changedImage.defaultImage;
        }
        slotImages[index].sprite = changedImage.selectedImage;
        currentSlot = capturedButton;
    }

    void OnHeroSelectedFromList(Job hero)
    {
        selectedHero = hero;

        if (selectedSlotIndex != null)
        {
            AssignHeroToSlot(selectedSlotIndex.Value, hero);
            ResetSelection(selectedSlotIndex.Value);
        }
    }

    void AssignHeroToSlot(int index, Job hero)
    {
        assignedHeroes[index] = hero;
        heroImages[index].SetActive(true);
        Debug.Log($"{assignedHeroes[index].name_job} 확인");
    }

    void ResetSelection(int index)
    {
        slotImages[index].sprite = changedImage.defaultImage;
        heroListUp.ResetButtonImage();
        ResetPartySlotInteractable();

        selectedHero = null;
        selectedSlotIndex = null;
    }

    // 파티 비활성화 이미지
    void UpdatePartySlot(Job hero)
    {
        Loc loc = (Loc)hero.loc;

        if (loc == Loc.Front)
        {
            slotButtons[0].interactable = true;
            slotButtons[1].interactable = true;
            slotButtons[2].interactable = false;
            slotButtons[3].interactable = false;
        }
        else if (loc == Loc.Back)
        {
            slotButtons[0].interactable = false;
            slotButtons[1].interactable = false;
            slotButtons[2].interactable = true;
            slotButtons[3].interactable = true;
        }
        else
        {
            slotButtons[0].interactable = true;
            slotButtons[1].interactable = true;
            slotButtons[2].interactable = true;
            slotButtons[3].interactable = true;
        }
    }

    void ResetPartySlotInteractable()
    {
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            if (assignedHeroes[i] == null)
                slotButtons[i].interactable = false;
            else
                slotButtons[i].interactable = true;
        }
    }
}
