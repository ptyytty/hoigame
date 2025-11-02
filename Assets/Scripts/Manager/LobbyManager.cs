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
    [SerializeField] private GameObject btnFriend;
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

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        btnMailbox.GetComponent<Button>().onClick.AddListener(() => panelMailbox.SetActive(true));
        btnCloseMailbox.onClick.AddListener(() => panelMailbox.SetActive(false));

        StartCoroutine(LoadNickname());
    }

    /// <summary>
    /// Firestore에서 내 프로필 닉네임을 불러와서 로비에 표시하는 메서드
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
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);
    }

    public void OnClickManagement()
    {
        panelManagement.SetActive(true);
        listUpManager.PricePanelState(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
        listUpManager.ApplyPanelState(false);
        panelNickname.SetActive(false);
    }

    public void OnclickShowStore()
    {
        panelStore.SetActive(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);
    }

    public void OnClickFriend()
    {
        panelFriend.SetActive(true);
        undoBtn.SetActive(true);

        panelMailbox.SetActive(false);
        panelMenu.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
        panelNickname.SetActive(false);
    }

    public void OnClickUndo()
    {

        if (panelStore.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelStore.SetActive(false);
            undoBtn.SetActive(false);

            if (Product.CurrentSelected != null)
            {
                Product.CurrentSelected.ResetToDefaultImage();
            }

            // ✅ 아이템 정보창 닫기
            if (ItemInfoPanel.instance != null)
            {
                ItemInfoPanel.instance.Hide();
            }
        }
        else if (panelManagement.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelManagement.SetActive(false);
            listUpManager.RecoveryPanelState(false);
            undoBtn.SetActive(false);

            listUpManager.ResetButtonImage();
            employment.ResetButtonImage();
            listUpManager.ResetHeroListState();
        }
        else if (panelFriend.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelFriend.SetActive(false);
            undoBtn.SetActive(false);
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
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);
            panelNickname.SetActive(true);

            panelDungeonPreparation.SetActive(false);
            panelItemList.SetActive(false);
            undoBtn.SetActive(false);
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
}
