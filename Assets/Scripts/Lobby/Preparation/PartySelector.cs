using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PartySelector : MonoBehaviour
{
    [Header("Hero List / Scripts")]
    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private ItemList itemList;
    [SerializeField] private Recovery recovery;

    [Header("Interact Panels")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject heroListPanel;
    [SerializeField] private GameObject itemListPanel;

    [Header("Party Slots")]
    [SerializeField] private List<GameObject> partySlots;

    [Header("Slot Sprites")]
    [SerializeField] private Sprite baseSlotImage;
    [SerializeField] private Sprite selectedImage;

    [Header("Position")]
    [SerializeField] private Image leftFront;
    [SerializeField] private Image rightFront;
    [SerializeField] private Image leftBack;
    [SerializeField] private Image rightBack;

    [Header("Enter Dungeon Button")]
    [SerializeField] private Button enterDungeon;
    [SerializeField] private EnterDungeonButton enterDungeonButton;
    [SerializeField] private DungeonInventory prepInventory;        // 던전 입장 시 인벤토리

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
        heroListUp.OnHeroSelected += OnHeroSelectedFromList; // 슬롯 가용만 책임
        itemList.OnEquipItemSelect += OnEquipItemSelectedFromList;

        ResetPartySlotInteractable();
    }

    void OnDisable()
    {
        // 🧹 이벤트 해제 (필수)
        heroListUp.OnHeroSelected -= OnHeroSelectedFromList;
        itemList.OnEquipItemSelect -= OnEquipItemSelectedFromList;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // ✅ 기존 Raycast 판정
            bool overPartyUI = IsPointerOverUI(partyPanel) || IsPointerOverUI(heroListPanel) || IsPointerOverUI(itemListPanel);
            // ✅ 보강: Raycast가 안 잡혀도 사각형 안이면 UI로 처리
            bool insideByRect = IsScreenPointInside(partyPanel)
                                || IsScreenPointInside(heroListPanel)
                                || IsScreenPointInside(itemListPanel);

            if (!overPartyUI && !insideByRect)
            {
                // 슬롯 이미지 전부 기본값으로 되돌리기
                for (int i = 0; i < Mathf.Min(slotImages.Count, assignedHeroes.Length); i++)
                    slotImages[i].sprite = GetSlotDefaultSprite(i);

                // 선택 상태 전원 해제
                selectedSlotIndex = null;
                selectedHero = null;
                equipItem = null;
                currentSlot = null;                            // ✅ 누락되면 이후 분기에서 NRE/오동작

                // 리스트 쪽도 확실히 초기화
                heroListUp?.ResetHeroListState();             // 기본 스프라이트+상호작용 복구
                                                              //itemList?.ResetItemListState?.Invoke();       // 구현돼있다면 호출 (없으면 생략)

                ResetPartySlotInteractable();                 // 슬롯 상호작용 규칙 재적용
                SelectionEvents.RaiseHeroSelected(null);
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

    // UI 이미지 설정
    Sprite GetSlotDefaultSprite(int index)
    {
        if (baseSlotImage) return baseSlotImage;
        return selectedImage;
    }

    // 현재 파티에 들어간 heroId 반환
    HashSet<int> BuildInPartySet()
    {
        var set = new HashSet<int>();
        for (int i = 0; i < assignedHeroes.Length; i++)
            if (assignedHeroes[i] != null) set.Add(assignedHeroes[i].id_job);
        return set;
    }

    // 리스트 버튼 파티 반영
    void RefreshHeroButtonsWithParty()
    {
        var instSet = BuildInPartyInstanceSet();
        heroListUp.SetButtonsForParty(instSet, assignedHeroes);
    }

    // === 선택된 영웅 기준으로 “배치 가능한 슬롯만” 활성화 ===
    void EnableSlotOptionsForHero(Job hero)
    {
        Loc loc = (Loc)hero.loc;
        int n = Mathf.Min(slotButtons.Count, assignedHeroes.Length);  // ✅ 안전 길이
        for (int i = 0; i < slotButtons.Count; i++)
        {
            bool isFrontSlot = i <= 1;
            bool canPlaceHere =
                (loc == Loc.Front && isFrontSlot) ||
                (loc == Loc.Back && !isFrontSlot) ||
                (loc != Loc.Front && loc != Loc.Back); // Loc.None/Any

            // ✅ 선택 상태에서는 '빈 슬롯' 제한을 해제한다 → 꽉 찬 슬롯도 클릭 가능(=교체 허용)
            slotButtons[i].interactable = canPlaceHere;
        }
    }

    // Party 슬롯 설정
    void SetupSlots()
    {
        slotButtons.Clear();
        heroImages.Clear();
        slotImages.Clear();

        // ✅ assignedHeroes 길이를 partySlots 수에 맞춤(안전)
        if (assignedHeroes == null || assignedHeroes.Length != partySlots.Count)
            assignedHeroes = new Job[partySlots.Count];

        for (int i = 0; i < partySlots.Count; i++)
        {
            int index = i;
            Button btn = partySlots[i].GetComponent<Button>();
            Image slotImage = partySlots[i].GetComponent<Image>();
            Transform heroImage = partySlots[i].transform.Find("HeroImage");

            slotButtons.Add(btn);
            heroImages.Add(heroImage.gameObject);
            slotImages.Add(slotImage);

            slotImage.sprite = GetSlotDefaultSprite(index);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSlotClicked(index));
        }

        for (int i = 0; i < assignedHeroes.Length; i++)
            SetHeroImageAt(i, assignedHeroes[i]);
    }

    void OnSlotClicked(int index)
    {
        selectedSlotIndex = index;

        Button capturedButton = slotButtons[index];
        var instSet = BuildInPartyInstanceSet();
        Loc slotLoc = (index <= 1) ? Loc.Front : Loc.Back;

        var idSet = BuildInPartyIdSet();
        heroListUp.SetHeroButtonInteractableByLoc((int)slotLoc, instSet, assignedHeroes);


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
            SelectionEvents.RaiseHeroSelected(assignedHeroes[index]);
        }
        // 슬롯 우선 선택
        else if (currentSlot != null)
        {
            Image prevImage = currentSlot.GetComponent<Image>();
            int prevIndex = slotButtons.IndexOf(currentSlot);
            prevImage.sprite = GetSlotDefaultSprite(prevIndex);
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
            SelectionEvents.RaiseHeroSelected(assignedHeroes[index]);
        }
        // 동일 슬롯 선택
        else if (currentSlot == capturedButton)
        {
            slotImages[index].sprite = selectedImage;
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
            SelectionEvents.RaiseHeroEquipChanged(assignedHeroes[index]);
            return;
        }
        else if (currentSlot == null)
        {
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        else if (currentSlot != null)
        {
            Image prevImage = currentSlot.GetComponent<Image>();
            int prevIndex = slotButtons.IndexOf(currentSlot);
            prevImage.sprite = GetSlotDefaultSprite(prevIndex);
            heroListUp.ShowHeroInfo(assignedHeroes[index]);
        }
        else if (currentSlot == capturedButton)
        {
            slotImages[index].sprite = selectedImage;
            return;
        }

        if (assignedHeroes[index] != null)
        {
            Job hero = assignedHeroes[index];
            itemList.SetEquipItemButtonInteractableByJob(hero.jobCategory); // ✅ 장비 필터링
            SelectionEvents.RaiseHeroSelected(assignedHeroes[index]);
        }

        slotImages[index].sprite = selectedImage;
        currentSlot = capturedButton;
    }

    void OnHeroSelectedFromList(Job hero)
    {
        selectedHero = hero;
        EnableSlotOptionsForHero(hero);

        if (selectedSlotIndex != null)
        {
            AssignHeroToSlot(selectedSlotIndex.Value, hero);
            ResetSelection(selectedSlotIndex.Value);
        }
    }

    void AssignHeroToSlot(int index, Job hero)
    {
        var replaced = assignedHeroes[index];
        assignedHeroes[index] = hero;

        SetHeroImageAt(index, hero);
        heroImages[index].SetActive(true);
        slotImages[index].sprite = GetSlotDefaultSprite(index);

        ResetPartySlotInteractable();

        // ✅ 즉시 해당 버튼 잠그기 (보조)
        heroListUp?.DisableButtonFor(hero);

        // ✅ 전체 재적용도 유지
        RefreshHeroButtonsWithParty();
        ResetPartySlotInteractable();
        CheckAssignedHeroState();
    }

    void SetHeroImageAt(int index, Job hero)
    {
        if (index < 0 || index >= heroImages.Count) return;
        var go = heroImages[index];
        if (go == null) return;

        var img = go.GetComponent<Image>();
        if (img == null) return;

        if (hero != null && hero.portrait != null)
        {
            img.sprite = hero.portrait;  // ← 영웅 초상화 반영
            go.SetActive(true);
        }
        else
        {
            img.sprite = null;           // ← 안전 초기화
            go.SetActive(false);
        }
    }

    // 아이템 착용 과정
    void AssignItemToHero(int index, EquipItem item)
    {
        Job hero = assignedHeroes[index];
        if (hero == null) { Debug.LogWarning("❌ 해당 슬롯에 영웅이 없습니다."); return; }

        // 직업 카테고리 체크
        if (hero.jobCategory != item.jobCategory)
        {
            Debug.LogWarning($"❌ {item.name_item}은(는) {item.jobCategory} 전용");
            return;
        }

        // 기존 장비 해제 → 표기 스탯 원복 + ID 초기화
        hero.equippedItem = null;
        hero.equippedItemId = 0;

        // 새 장비 적용 → 표기 스탯 증분 + ID 기록
        hero.equippedItem = item;
        hero.equippedItemId = item.id_item;

        Debug.Log($"✅ {hero.name_job} 장착: {item.name_item}");
    }

    void ResetSelection(int index)
    {
        slotImages[index].sprite = GetSlotDefaultSprite(index);

        heroListUp.ResetButton();
        heroListUp.SetInteractable(true); // ✅ 리스트 복구
        RefreshHeroButtonsWithParty();

        selectedHero = null;
        selectedSlotIndex = null;

        itemList.ResetItemButton();
        itemList.SetInteractable(true);

        ResetPartySlotInteractable();
    }

    public void ResetSelectorState()
    {
        selectedHero = null;
        selectedSlotIndex = null;
        equipItem = null;
        currentSlot = null; // ✅ 추가

        // 슬롯 비주얼도 전부 기본으로 복구
        for (int i = 0; i < Mathf.Min(slotImages.Count, assignedHeroes.Length); i++)
            slotImages[i].sprite = GetSlotDefaultSprite(i);

        // 리스트/아이템 쪽도 확실히 리셋
        heroListUp?.ResetHeroListState();

        ResetPartySlotInteractable();
        SelectionEvents.RaiseHeroSelected(null);
    }

    public void ResetSelectorStateAndInventory()
    {
        ResetSelectorState();

        if (prepInventory != null)
            prepInventory.ClearToInventory();
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

    // === 슬롯 기본 규칙 ===
    // - 비어 있으면 비활성
    // - 채워져 있으면 활성
    public void ResetPartySlotInteractable()
    {
        int n = Mathf.Min(slotButtons.Count, assignedHeroes.Length);  // ✅ 안전 길이
        for (int i = 0; i < n; i++)
        {
            if (slotButtons[i] == null) continue;                     // ✅ 널가드
            slotButtons[i].interactable = assignedHeroes[i] != null;  // 빈 슬롯=비활성, 채워진 슬롯=활성
        }
    }

    public void ResetAssignParty()
    {
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            assignedHeroes[i] = null;
            Debug.Log($"{assignedHeroes[i]} 제거");

            SetHeroImageAt(i, null);
            heroImages[i].SetActive(false);
        }

        if (prepInventory != null)
            prepInventory.ClearToInventory();

        // 전부 해제됐으니 리스트 전체 상호작용 다시 켜짐
        heroListUp.SetInteractable(true);
        RefreshHeroButtonsWithParty();      // (빈 파티라 모두 활성화 상태가 됨)
        ResetPartySlotInteractable();
    }


    // 장비 아이템 선택
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

    // 던전 진입 버튼 이미지 변경
    void SetEnterDungeonButton(bool assigned)
    {
        Image image = enterDungeon.GetComponent<Image>();

        image.sprite = assigned ? enterDungeonButton.entryImage : enterDungeonButton.noEntryImage;
        enterDungeon.interactable = assigned;
    }

    // 던전 입장
    public async void OnClickEnterDungeon()
    {
        // 안전망: 중복 검사
        var set = new HashSet<Job>();
        foreach (var h in AssignedHeroes)
        {
            if (h == null) { Debug.LogWarning("❌ 빈 슬롯 존재"); return; }
            if (!set.Add(h)) { Debug.LogWarning("❌ 중복 영웅 존재"); return; }
        }

        PartyBridge.Instance.dungeonLoadoutSnapshot = prepInventory.CreateSnapshot();       // 인벤토리 전달
        PartyBridge.Instance.SetParty(AssignedHeroes);                                      // 파티 전달

        recovery.ResetHealLocks();          // 의무실 잠금 해제

        SceneManager.LoadScene("Dungeon_Oratio");
    }


    // === 인스턴스 집합 ===
    HashSet<string> BuildInPartyInstanceSet()
    {
        var set = new HashSet<string>();
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            var h = assignedHeroes[i];
            if (h != null && !string.IsNullOrEmpty(h.instanceId))
                set.Add(h.instanceId);
        }
        return set;
    }

    HashSet<int> BuildInPartyIdSet()
    {
        var set = new HashSet<int>();
        for (int i = 0; i < assignedHeroes.Length; i++)
        {
            var h = assignedHeroes[i];
            if (h != null) set.Add(h.id_job);
        }
        return set;
    }

    // 외부에서 파티 상태를 재적용할 수 있도록 공개
    public HashSet<string> GetInPartyInstanceSet() => BuildInPartyInstanceSet();

    // ====== UI 유틸 =====
    bool IsScreenPointInside(GameObject target)
    {
        if (target == null) return false;
        var rt = target.transform as RectTransform;
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
    }
}
