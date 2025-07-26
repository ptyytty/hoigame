using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public abstract class ListUIBase<TData> : MonoBehaviour
{
    [SerializeField] protected Button buttonPrefab;
    [SerializeField] protected Transform contentParent;
    [SerializeField] protected HeroButtonObject.ChangedImage changedImage;

    protected Button currentSelect;
    protected List<Button> buttons = new();
    protected List<TData> dataList = new();

    // UI 편의성 제어
    protected virtual void OnEnable()
    {
        var clickHandler = FindObjectOfType<UIClickResetHandler>();
        if (clickHandler != null)
            clickHandler.RegisterResetCallback(ResetSelectedButton);
    }

    // 버튼 생성
    protected void CreateButton(TData data)
    {
        Button button = Instantiate(buttonPrefab, contentParent);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.text = GetLabel(data);

        var capturedButton = button;
        var capturedData = data;

        button.GetComponent<Image>().sprite = changedImage.defaultImage;

        button.onClick.AddListener(() =>
        {
            if (currentSelect == capturedButton)
                return;

            if (currentSelect != null)
                ResetSelectedButton();

            currentSelect = capturedButton;
            capturedButton.GetComponent<Image>().sprite = changedImage.selectedImage;

            OnSelected(capturedData);
        });

        buttons.Add(button);
        dataList.Add(data);
    }

    // 리스트 초기화
    protected void ClearList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        buttons.Clear();
        dataList.Clear();
        currentSelect = null;
    }

    // 버튼 선택 이미지 => 기본 이미지
    protected void ResetSelectedButton()
    {
        if (currentSelect == null) return;
        currentSelect.GetComponent<Image>().sprite = changedImage.defaultImage;
        currentSelect = null;
    }

    // 버튼 상호작용 제어
    protected void SetAllButtonsInteractable(bool state)
    {
        foreach (var btn in buttons)
            btn.interactable = state;
    }

    // 버튼에 표시할 텍스트 반환
    protected abstract string GetLabel(TData data);
    // 버튼 클릭 시 실행 로직
    protected abstract void OnSelected(TData data);
    // 리스트 생성
    protected abstract void LoadList();
}
