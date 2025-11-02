using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 상품 정렬 드롭다운
public class SortedDropdown : MonoBehaviour
{
    // [역할] 외부에서 현재 정렬 상태를 구독/조회할 수 있도록 공개 API 제공
    public enum SortOption { Newest, Price } // 0: 최신순, 1: 가격순
    public System.Action<SortOption, bool> OnSortChanged; // (정렬기준, 오름차순 여부)

    // [역할] 현재 선택된 인덱스에 따라 정렬 기준을 계산
    public SortOption CurrentOption => (currentIndex == 0) ? SortOption.Newest : SortOption.Price;

    // [역할] 오름/내림 규칙
    //  - 최신순의 기본은 "내림차순(최신 → 오래된)" → isFlipped=false
    //  - 가격순의 기본은 "오름차순(낮은가격 → 높은가격)" → isFlipped=false
    //  - 따라서 IsAscending 계산식을 옵션에 따라 다르게 둔다
    public bool IsAscending => CurrentOption == SortOption.Newest ? isFlipped : !isFlipped;

    [System.Serializable]
    public class SortedItemDropdownToggle
    {
        public Toggle toggle;
        public Image background;
        public TMP_Text label;
        public RectTransform checkmark;
        public Color selectedColor = new Color(139f / 255f, 149f / 255, 179f / 255);
        public Color defaultColor = new Color(1f, 1f, 1f, 0f);
    }

    public List<SortedItemDropdownToggle> sortedItemDropdowns;

    [SerializeField] private GameObject sortedDropdown;
    [SerializeField] private GameObject sortButton;
    [SerializeField] private TMP_Text sortButtonText;
    [SerializeField] private RectTransform sortButtonImage;

    // 패널 오픈
    private bool isOpen = false;
    private bool isFlipped = false;   // [역할] 같은 버튼 재클릭 시 방향 전환(화살표 뒤집기)
    private Toggle lastSelected = null;
    private int currentIndex = 0;     // [역할] 0: 최신순, 1: 가격순 (인스펙터 순서 기준)

    void Start()
    {
        // 개별 토글 바인딩
        for (int i = 0; i < sortedItemDropdowns.Count; i++)
        {
            int index = i;
            sortedItemDropdowns[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(sortedItemDropdowns[index].toggle, sortedItemDropdowns, ref lastSelected);
                }
            });
        }

        // 기본값: 최신순(내림차순)
        if (sortedItemDropdowns.Count > 0)
        {
            currentIndex = 0; // 최신순
            sortButtonText.text = sortedItemDropdowns[0].label.text;
            sortedItemDropdowns[0].toggle.isOn = true;
            lastSelected = sortedItemDropdowns[0].toggle;
            UpdateToggle(sortedItemDropdowns);   // 기준 선택 시 isFlipped=false로 초기화
            isFlipped = false;                   // 최신순은 "내림차순"이 기본
            OnSortChanged?.Invoke(CurrentOption, IsAscending); // 초기 상태 브로드캐스트
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(sortedDropdown) && !IsPointerOverUI(sortButton))
            {
                ClosePanel();
            }
        }
    }

    /// <summary>
    /// [역할] 드롭다운 열기/닫기 토글
    /// </summary>
    public void OpenSortedDropDown()
    {
        if (isOpen) ClosePanel();
        else OpenPanel();
    }

    /// <summary>
    /// [역할] 패널 열기
    /// </summary>
    public void OpenPanel()
    {
        isOpen = true;
        sortedDropdown.SetActive(true);
    }

    /// <summary>
    /// [역할] 패널 닫기
    /// </summary>
    public void ClosePanel()
    {
        isOpen = false;
        sortedDropdown.SetActive(false);
    }

    bool IsPointerOverUI(GameObject target)
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

    /// <summary>
    /// [역할] 동일 그룹의 토글 중 복수 선택 방지 + 동일 토글 재클릭 시 방향 전환 이벤트로 연결
    /// </summary>
    void OnToggleChanged(Toggle sortedToggle, List<SortedItemDropdownToggle> sortedItemList, ref Toggle lastSelected)
    {
        if (sortedToggle.isOn)
        {
            if (sortedToggle != lastSelected)
            {
                lastSelected = sortedToggle;
                UpdateToggle(sortedItemList); // 기준 변경 시 방향 초기화
                OnSortChanged?.Invoke(CurrentOption, IsAscending);
            }
            else
            {
                ChangeMark(sortedItemList);   // 재클릭 시 방향 전환
            }
        }
    }

    /// <summary>
    /// [역할] 정렬 기준 변경 시 UI/상태 업데이트 (방향 초기화)
    /// </summary>
    void UpdateToggle(List<SortedItemDropdownToggle> sortedItemList)
    {
        isFlipped = false; // 새 기준을 고르면 화살표 방향은 기본값으로 리셋
        for (int i = 0; i < sortedItemList.Count; i++)
        {
            var pair = sortedItemList[i];

            if (pair.toggle.isOn)
            {
                sortButtonText.text = pair.label.text;
                currentIndex = i; // 0=최신순, 1=가격순
            }

            pair.background.color = pair.toggle.isOn ? pair.selectedColor : pair.defaultColor;
            pair.checkmark.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
            sortButtonImage.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
        }
    }

    /// <summary>
    /// [역할] 같은 기준에서 재클릭 시 방향 전환(역순 정렬)
    /// </summary>
    void ChangeMark(List<SortedItemDropdownToggle> sortedItemList)
    {
        isFlipped = !isFlipped;

        foreach (var pair in sortedItemList)
        {
            pair.checkmark.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
            sortButtonImage.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
        }

        OnSortChanged?.Invoke(CurrentOption, IsAscending);
    }

    /// <summary>
    /// [역할] 외부에서 드롭다운을 기본 상태(최신순, 기본 방향)로 리셋
    /// </summary>
    public void ResetToggle()
    {
        if (lastSelected != null)
            lastSelected.isOn = false;

        currentIndex = 0; // 최신순
        sortedItemDropdowns[0].toggle.isOn = true;
        sortButtonText.text = sortedItemDropdowns[0].label.text;
        lastSelected = sortedItemDropdowns[0].toggle;
        UpdateToggle(sortedItemDropdowns);
        isFlipped = false;

        OnSortChanged?.Invoke(CurrentOption, IsAscending);
    }
}
