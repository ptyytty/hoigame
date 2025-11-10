using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;

public class LobbyManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelGoods;
    [SerializeField] private GameObject panelStore;
    [SerializeField] private GameObject panelManagement;
    [SerializeField] private GameObject panelFriend;
    [SerializeField] private GameObject panelDungeonPreparation;
    [SerializeField] private GameObject panelMailbox;
    [SerializeField] private GameObject panelNickname;

    [Header("Main Lobby")]
    [SerializeField] private GameObject btnMailbox;
    [SerializeField] private TextMeshProUGUI nicknameText;

    [Header("Close Mailbox")]
    [SerializeField] private Button btnCloseMailbox;

    [Header("Undo Button")]
    [SerializeField] private GameObject undoBtn;

    [Header("Dungeon Preparation")]
    [SerializeField] private GameObject panelHeroList;
    [SerializeField] private GameObject panelItemList;

    [Header("Scripts")]
    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private PartySelector partySelector;
    [SerializeField] private ListUpManager listUpManager;
    [SerializeField] private Employment employment;

    // ========================= [NEW] 로비 3D 표시 제어 =========================
    [Header("Lobby 3D View (선택 연결)")]
    [Tooltip("메인 로비에서만 켜질 3D 오브젝트들의 부모(실오브젝트 방식 사용 시 연결)")]
    [SerializeField] private GameObject lobby3DRoot;          // 실오브젝트 방식

    [Tooltip("메인 로비 3D만 비추는 카메라(있다면). 없으면 비워둬도 됨")]
    [SerializeField] private Camera lobby3DCamera;             // 실오브젝트/프리뷰 공통

    // =======================================================================

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        undoBtn.GetComponent<Button>().onClick.AddListener(() => OnClickUndo());

        // 📮 우편함 열기/닫기 시에도 3D 감춤/표시
        btnMailbox.GetComponent<Button>().onClick.AddListener(() =>
        {
            panelMailbox.SetActive(true);
            SetLobby3DVisible(false);   // [NEW] 메인 로비 전용이므로 패널 열면 감춤
        });
        btnCloseMailbox.onClick.AddListener(() =>
        {
            panelMailbox.SetActive(false);
            // 우편함을 닫았을 때 진짜 메인 로비 화면인지 확인 후 표시
            TryShowLobby3DIfOnMain();   // [NEW]
        });

        // 메인 로비 진입 초기 상태: 3D 보이기
        SetLobby3DVisible(true);        // [NEW]

        StartCoroutine(LoadNickname());
    }

    /// <summary>
    /// [역할] Firestore에서 내 프로필 닉네임을 불러와서 로비에 표시
    /// </summary>
    IEnumerator LoadNickname()
    {
        yield return new WaitForSeconds(0.5f); // Firebase 초기화 대기용 (필요시 조정)

        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("❌ 로그인된 유저가 없습니다.");
            yield break;
        }

        string uid = auth.CurrentUser.UserId;
        DocumentReference docRef = db.Collection("profiles").Document(uid);

        var getTask = docRef.GetSnapshotAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        if (getTask.Exception != null)
        {
            Debug.LogError($"❌ Firestore 닉네임 로드 실패: {getTask.Exception}");
            yield break;
        }

        DocumentSnapshot snapshot = getTask.Result;
        if (snapshot.Exists && snapshot.ContainsField("nickname"))
        {
            string nickname = snapshot.GetValue<string>("nickname");
            nicknameText.text = nickname;
            Debug.Log($"✅ 닉네임 로드 완료: {nickname}");
        }
        else
        {
            nicknameText.text = "닉네임 없음";
            Debug.Log("⚠ 닉네임 필드가 존재하지 않음");
        }
    }

    public void OnClickDungeonList()
    {
        undoBtn.SetActive(true);
        panelDungeonPreparation.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);
        panelGoods.SetActive(false);

        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);

        SetLobby3DVisible(false); // [NEW] 메인 로비가 아니므로 숨김
    }

    public void OnClickManagement()
    {
        panelManagement.SetActive(true);
        listUpManager.PricePanelState(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);

        btnMailbox.SetActive(false);
        listUpManager.ApplyPanelState(false);
        panelNickname.SetActive(false);

        SetLobby3DVisible(false); // [NEW]
    }

    public void OnclickShowStore()
    {
        panelStore.SetActive(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);

        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);

        SetLobby3DVisible(false); // [NEW]
    }

    public void OnClickFriend()
    {
        panelFriend.SetActive(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);

        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);

        SetLobby3DVisible(false); // [NEW]
    }

    public void OnClickUndo()
    {
        if (panelStore.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelStore.SetActive(false);
            undoBtn.SetActive(false);

            if (Product.CurrentSelected != null)
            {
                Product.CurrentSelected.ResetToDefaultImage();
            }

            if (ItemInfoPanel.instance != null)
            {
                ItemInfoPanel.instance.HideAll(); // 역할: 로컬/온라인 두 패널 모두 비활성화
            }

            TryShowLobby3DIfOnMain();
        }
        else if (panelManagement.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelManagement.SetActive(false);
            listUpManager.RecoveryPanelState(false);
            undoBtn.SetActive(false);

            listUpManager.ResetButtonImage();
            employment.ResetButtonImage();
            listUpManager.ResetHeroListState();

            TryShowLobby3DIfOnMain(); // [NEW]
        }
        else if (panelFriend.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelFriend.SetActive(false);
            undoBtn.SetActive(false);

            TryShowLobby3DIfOnMain(); // [NEW]
        }
        else if (panelDungeonPreparation.activeSelf)
        {
            // Preparation 상태 초기화
            heroListUp.ResetHeroListState();
            partySelector.ResetAssignParty();
            panelHeroList.SetActive(true);

            // 로비 Active
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelDungeonPreparation.SetActive(false);
            panelItemList.SetActive(false);
            undoBtn.SetActive(false);

            TryShowLobby3DIfOnMain(); // [NEW]
        }
    }

    public void OnClickListToggle()
    {
        if (panelHeroList.activeSelf)
        {
            panelHeroList.SetActive(false);
            panelItemList.SetActive(true);
        }
        else
        {
            panelHeroList.SetActive(true);
            panelItemList.SetActive(false);
        }
        Debug.Log("클릭");
    }

    // ========================= [NEW] 공통 유틸 =========================

    /// <summary>
    /// [역할] 메인 로비 전용 3D를 보이게/숨기게 한다.
    /// - 실오브젝트 방식: lobby3DRoot 활성/비활성
    /// - 프리뷰 방식: lobby3DView(RawImage 등) 활성/비활성
    /// - 전용 카메라가 있을 경우 enable 토글
    /// </summary>
    private void SetLobby3DVisible(bool visible)
    {
        if (lobby3DRoot) lobby3DRoot.SetActive(visible);
    }

    /// <summary>
    /// [역할] 현재 화면이 '메인 로비' 상태라면 3D를 다시 표시한다.
    /// 메인 로비 조건: 메뉴/재화/우편버튼/닉네임 패널이 보이고, 다른 풀스크린 패널이 모두 닫힘.
    /// </summary>
    private void TryShowLobby3DIfOnMain()
    {
        bool isMain =
            panelMenu.activeSelf &&
            panelGoods.activeSelf &&
            btnMailbox.activeSelf &&
            panelNickname.activeSelf &&
            !panelStore.activeSelf &&
            !panelManagement.activeSelf &&
            !panelFriend.activeSelf &&
            !panelDungeonPreparation.activeSelf &&
            !panelMailbox.activeSelf;

        SetLobby3DVisible(isMain);
    }
}
