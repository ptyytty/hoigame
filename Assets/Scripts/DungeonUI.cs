using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class DungeonUI : MonoBehaviour
{
    [SerializeField]
    public RectTransform skillPanel; // 현재 패널
    [SerializeField]
    public Button skillButton;   // 패널을 여는 버튼
    [SerializeField]
    private TMP_Text skillTarget;
    [SerializeField]
    private RectTransform enemyPanel;
    [SerializeField]
    private RectTransform partyPanel;


    //private Vector3 originalPosition;

    private float moveOffset = 240f; // 이동 거리
    private float duration = 1.0f;   // 애니메이션 지속 시간
    private Vector2 hiddenPos;  //현재 위치
    private Vector2 visiblePos; // 클릭 시 위치
    private Vector2 enemyHiddenPos;
    private Vector2 enemyVisiblePos;
    private bool isOpen = false;
    public bool isEnemyPanelOpen = false;
    public bool isPartyPanelOpen = false;

    // 현재 열린 패널을 추적하는 static 변수
    private static DungeonUI currentOpenDrawer = null;

    void Start()
    {
        hiddenPos = skillPanel.anchoredPosition;
        visiblePos = hiddenPos + new Vector2(0, moveOffset);

        enemyHiddenPos = enemyPanel.anchoredPosition;
        enemyVisiblePos = enemyHiddenPos + new Vector2(120f, 0);

        skillButton.onClick.AddListener(ToggleUI);

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
        skillPanel.DOAnchorPos(visiblePos, duration).SetEase(Ease.OutBack).SetUpdate(true);
        isOpen = true;
        currentOpenDrawer = this; // 현재 열린 패널 저장

        //스킬 대상 확인
        if(skillTarget.text.ToString() == "대상: 적"){
            Debug.Log("적 입니다");
            OpenEnemyPanel();
        }else if(skillTarget.text.ToString() == "대상: 아군"){
            OpenPartyPanel();
            Debug.Log("아군 대상입니다");
        }else{
            OpenPartyPanel();
            Debug.Log("자신 대상입니다");
        }
    }

    void CloseDrawer()
    {
        skillPanel.DOAnchorPos(hiddenPos, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        isOpen = false;

        // 만약 현재 열린 패널이 본인이라면 초기화
        if (currentOpenDrawer == this)
        {
            currentOpenDrawer = null;
        }
        if(isEnemyPanelOpen){
            CloseEnemyPanel();
        }
        if(isPartyPanelOpen){
            ClosePartyPanel();
        }
    }

    void OpenEnemyPanel()
    {
        enemyPanel.DOAnchorPos(enemyVisiblePos, 0.7f).SetEase(Ease.InOutSine).SetUpdate(true);
        isEnemyPanelOpen = true;
        //Enemypanel이 열릴 때 카메라 이동
        BattleCamera.instance.SwitchToEnemy();
    }

    void CloseEnemyPanel()
    {
        enemyPanel.DOAnchorPos(enemyHiddenPos, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        isEnemyPanelOpen = false;
        BattleCamera.instance.SwitchToDefault();
    }

    void OpenPartyPanel()
    {
        partyPanel.DOAnchorPos(enemyVisiblePos, 0.7f).SetEase(Ease.InOutSine).SetUpdate(true);
        isPartyPanelOpen = true;
        //Enemypanel이 열릴 때 카메라 이동
        BattleCamera.instance.SwitchToParty();
    }

    void ClosePartyPanel()
    {
        partyPanel.DOAnchorPos(enemyHiddenPos, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        isPartyPanelOpen = false;
        BattleCamera.instance.SwitchToDefault();
    }
}
