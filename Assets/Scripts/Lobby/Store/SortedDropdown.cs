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
    private bool isFlipped = false;
    private Toggle lastSelected = null;

    void Start()
    {
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

        if (sortedItemDropdowns.Count > 0)
        {
            sortButtonText.text = sortedItemDropdowns[0].label.text;
            sortedItemDropdowns[0].toggle.isOn = true;
            lastSelected = sortedItemDropdowns[0].toggle;
            UpdateToggle(sortedItemDropdowns);
            isFlipped = false;
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

    public void OpenSortedDropDown()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        isOpen = true;
        sortedDropdown.SetActive(true);
    }

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

    void OnToggleChanged(Toggle sortedToggle, List<SortedItemDropdownToggle> sortedItemList, ref Toggle lastSelected)
    {
        if (sortedToggle.isOn)
        {
            if (sortedToggle != lastSelected)
            {
                lastSelected = sortedToggle;
                UpdateToggle(sortedItemList);
            }
            else
            {
                ChangeMark(sortedItemList);
            }
        }
    }

    void UpdateToggle(List<SortedItemDropdownToggle> sortedItemList)
    {
        isFlipped = false;
        foreach (var pair in sortedItemList)
        {
            if (pair.toggle.isOn)
                sortButtonText.text = pair.label.text;
            pair.background.color = pair.toggle.isOn ? pair.selectedColor : pair.defaultColor;
            pair.checkmark.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
            sortButtonImage.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
        }
    }

    void ChangeMark(List<SortedItemDropdownToggle> sortedItemList)
    {
        isFlipped = !isFlipped;

        foreach (var pair in sortedItemList)
        {
            pair.checkmark.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
            sortButtonImage.localRotation = Quaternion.Euler(0, 0, isFlipped ? 180f : 0f);
        }
    }

    public void ResetToggle()
    {
        if (lastSelected != null)
            lastSelected.isOn = false;

        sortedItemDropdowns[0].toggle.isOn = true;
        sortButtonText.text = sortedItemDropdowns[0].label.text;
        lastSelected = sortedItemDropdowns[0].toggle;
        UpdateToggle(sortedItemDropdowns);
        isFlipped = false;

    }
}
