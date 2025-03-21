using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    public RectTransform uiPanel; // 현재 패널
    [SerializeField]
    public Button toggleButton;   // 패널을 여는 버튼

    private float moveOffset = 240f; // 이동 거리
    private float duration = 1.0f;   // 애니메이션 지속 시간
    private Vector2 hiddenPos;  //현재 위치
    private Vector2 visiblePos; // 클릭 시 위치
    private bool isOpen = false;

    // 현재 열린 패널을 추적하는 static 변수
    private static UIManager currentOpenDrawer = null;

    void Start()
    {
        hiddenPos = uiPanel.anchoredPosition;
        visiblePos = hiddenPos + new Vector2(0, moveOffset);

        toggleButton.onClick.AddListener(ToggleUI);
    }

    void ToggleUI()
    {
        // 만약 다른 패널이 열려 있다면 닫기
        if (currentOpenDrawer != null && currentOpenDrawer != this)
        {
            currentOpenDrawer.CloseDrawer();
        }

        if (isOpen)
        {
            CloseDrawer();
        }
        else
        {
            OpenDrawer();
        }
    }

    void OpenDrawer()
    {
        //Time.timeScale 영향을 받지 않도록 설정
        uiPanel.DOAnchorPos(visiblePos, duration).SetEase(Ease.OutBack).SetUpdate(true);
        isOpen = true;
        currentOpenDrawer = this; // 현재 열린 패널 저장
    }

    void CloseDrawer()
    {
        uiPanel.DOAnchorPos(hiddenPos, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        isOpen = false;

        // 만약 현재 열린 패널이 본인이라면 초기화
        if (currentOpenDrawer == this)
        {
            currentOpenDrawer = null;
        }
    }

}
