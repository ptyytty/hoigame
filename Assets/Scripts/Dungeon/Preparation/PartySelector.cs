using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PartySelector : MonoBehaviour
{
    [Header("Hero List / Scripts")]
    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private HeroButtonObject.ChangedImage changedImage;
    [SerializeField] private ItemList itemList;

    [Header("Interact Panels")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject heroListPanel;
    [SerializeField] private GameObject itemListPanel;

    [Header("Party Slots")]
    [SerializeField] private List<GameObject> partySlots;

    [Header("Position")]
    [SerializeField] private Image leftFront;
    [SerializeField] private Image rightFront;
    [SerializeField] private Image leftBack;
    [SerializeField] private Image rightBack;

    [Header("Enter Dungeon Button")]
    [SerializeField] private Button enterDungeon;
    [SerializeField] private EnterDungeonButton enterDungeonButton;

    private List<Button> slotButtons = new();
    private List<GameObject> heroImages = new();
    private List<Image> slotImages = new();
    private Job[] assignedHeroes = new Job[4];
    public Job[] AssignedHeroes => assignedHeroes;

    // 선택된 슬롯 번호
    private int? selectedSlotIndex = null;
    // 선택된 영웅
    private Job selectedHero = null;
    // 선택된 장비
    private EquipItem equipItem = null;

    private Button currentSlot;

    void OnEnable()
    {
        SetupSlots();

        // ✅ 이벤트 구독 (등록)
        heroListUp.OnHeroSelected += UpdatePartySlot;
        heroListUp.OnHeroSelected += OnHeroSelectedFromList;

        itemList.OnEquipItemSelect += OnEquipItemSelectedFromList;

        ResetPartySlotInteractable();
    }

    void OnDisable()
    {
        // 🧹 이벤트 해제 (필수)
        heroListUp.OnHeroSelected -= UpdatePartySlot;
        heroListUp.OnHeroSelected -= OnHeroSelectedFromList;

        ItemList.instance.OnEquipItemSelect -= OnEquipItemSelectedFromList;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            if (!IsPointerOverUI(partyPanel) && !IsPointerOverUI(heroListPanel) && !IsPointerOverUI(itemListPanel))
            {
                for (int i = 0; i < assignedHeroes.Length; i++)
                {
                    if (assignedHeroes[i] != null)
                        slotImages[i].sprite = changedImage.defaultImage;
                }

                selectedSlotIndex = null;
                selectedHero = null;
            }
        }
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

    // Party 슬롯 설정
    void SetupSlots()
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            Debug.Log($"🧩 partySlots.Count = {partySlots.Count}");
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
                OnSlotClicked(index);
            });
        }
    }

    void OnSlotClicked(int index)
    {
        selectedSlotIndex = index;

        Button capturedButton = slotButtons[index];
        Loc slotLoc = (index <= 1) ? Loc.Front : Loc.Back;
        heroListUp.SetHeroButtonInteractableByLoc((int)slotLoc);
        

        // 영웅 리스트 클릭 후 슬롯 선택
        if (selectedHero != null)
        {
            AssignHeroToSlot(index, selectedHero);
            ResetSelection(selectedSlotIndex.Value);
            return;
        }
        else if (currentSlot == null)
        {
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        // 슬롯 우선 선택
        else if (currentSlot != null)
        {
            Debug.Log($"{index}, 영웅 있음 여부 {assignedHeroes[index] != null}");
            Image prevImage = currentSlot.GetComponent<Image>();
            prevImage.sprite = changedImage.defaultImage;
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        // 동일 슬롯 선택
        else if (currentSlot == capturedButton)
        {
            slotImages[index].sprite = changedImage.selectedImage;
            return;
        }

        // 아이템 리스트 클릭 후 슬롯 선택
        if (equipItem != null)
        {
            Debug.Log("아이템 함수");
            AssignItemToHero(selectedSlotIndex.Value, equipItem);
            equipItem = null;
            //itemList.SetInteractable(true);
            ResetSelection(selectedSlotIndex.Value);
            return;
        }
        else if (currentSlot == null)
        {
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        else if (currentSlot != null)
        {
            Image prevImage = currentSlot.GetComponent<Image>();
            prevImage.sprite = changedImage.defaultImage;
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        else if (currentSlot == capturedButton)
        {
            slotImages[index].sprite = changedImage.selectedImage;
            return;
        }

        if (assignedHeroes[index] != null)
        {
            Job hero = assignedHeroes[index];
            itemList.SetEquipItemButtonInteractableByJob(hero.jobCategory); // ✅ 장비 필터링
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

        CheckAssignedHeroState();
    }

    // 아이템 착용 과정
    void AssignItemToHero(int index, EquipItem item)
    {
        Job hero = assignedHeroes[index];
        if (hero == null)
        {
            Debug.LogWarning("❌ 해당 슬롯에 영웅이 없습니다.");
            return;
        }

        // ✅ 직업 카테고리 일치 검사
        if (hero.jobCategory != item.jobCategory)
        {
            Debug.LogWarning($"❌ {item.name_item}은(는) {item.jobCategory} 직업 전용입니다. ({hero.name_job}은 {hero.jobCategory})");
            return;
        }

        // ✅ 기존 장비 해제
        if (hero.equippedItem != null)
        {
            UnapplyItemStats(hero, hero.equippedItem);
            Debug.Log($"🔁 기존 장비 제거됨: {hero.equippedItem.name_item}");
        }

        // ✅ 새 장비 적용
        hero.equippedItem = item;
        ApplyItemStats(hero, item);
        Debug.Log($"✅ {hero.name_job}이 {item.name_item}을(를) 장비함");
    }

    // 아이템 착용
    void ApplyItemStats(Job hero, EquipItem item)
    {
        switch (item.buffType[0])
        {
            case EquipItemBuffType.Def:
                hero.def += item.value;
                break;
            case EquipItemBuffType.Spd:
                hero.spd += item.value;
                break;
            case EquipItemBuffType.Hit:
                hero.hit += item.value;
                break;
        }
    }
    // 아이템 해제
    void UnapplyItemStats(Job hero, EquipItem item)
    {
        switch (item.buffType[0])
        {
            case EquipItemBuffType.Def:
                hero.def -= item.value;
                break;
            case EquipItemBuffType.Spd:
                hero.spd -= item.value;
                break;
            case EquipItemBuffType.Hit:
                hero.hit -= item.value;
                break;
        }
    }

    void ResetSelection(int index)
    {
        slotImages[index].sprite = changedImage.defaultImage;
        heroListUp.ResetButton();
        heroListUp.SetInteractable(true); // ✅ 리스트 복구
        ResetPartySlotInteractable();

        selectedHero = null;
        selectedSlotIndex = null;

        itemList.ResetItemButton();
        itemList.SetInteractable(true);
    }

    public void ResetSelectorState()
    {
        selectedHero = null;
        selectedSlotIndex = null;
        equipItem = null;

        ResetPartySlotInteractable();
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

    public void ResetPartySlotInteractable()
    {
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            if (assignedHeroes[i] == null)
                slotButtons[i].interactable = false;
            else
                slotButtons[i].interactable = true;
        }
    }

    public void ResetAssignParty()
    {
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            assignedHeroes[i] = null;
            Debug.Log($"{assignedHeroes[i]} 제거");
            heroImages[i].SetActive(false);
        }

        ResetPartySlotInteractable();
    }

    void OnEquipItemSelectedFromList(EquipItem item)
    {
        equipItem = item;
        Debug.Log(equipItem.jobCategory);

        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            if (assignedHeroes[i] == null) continue;
            if (equipItem.jobCategory == assignedHeroes[i].jobCategory)
            {
                slotButtons[i].interactable = true;
                continue;
            }
            else if (equipItem.jobCategory != assignedHeroes[i].jobCategory)
            {
                slotButtons[i].interactable = false;
            }
        }

        if (selectedSlotIndex != null)
        {
            AssignItemToHero(selectedSlotIndex.Value, equipItem);
            equipItem = null;
            itemList.SetInteractable(true);
            ResetSelection(selectedSlotIndex.Value);
        }
    }

    void CheckAssignedHeroState()
    {
        bool allAssigned = true;

        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            if (assignedHeroes[i] == null)
            {
                allAssigned = false;
                break;
            }
        }

        SetEnterDungeonButton(allAssigned);
    }

    void SetEnterDungeonButton(bool assigned)
    {
        Image image = enterDungeon.GetComponent<Image>();

        image.sprite = assigned ? enterDungeonButton.entryImage : enterDungeonButton.noEntryImage;
        enterDungeon.interactable = assigned;
    }

    public void OnClickEnterDungeon()
    {
        SceneManager.LoadScene("Dungeon_Oratio");
    }

}
