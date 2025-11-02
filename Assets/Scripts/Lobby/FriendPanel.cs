using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;

public class FriendPanel : MonoBehaviour
{
    [Header("입력/버튼")]
    [SerializeField] private TMP_InputField inputNickname;  // 닉네임 입력창
    [SerializeField] private Button btnConfirm;             // 확인(신청) 버튼

    [Header("탭 토글(동작용)")]
    [SerializeField] private Toggle toggleFriendList;       // 친구 목록 탭
    [SerializeField] private Toggle toggleRequestList;      // 신청 목록 탭

    // ─────────────────────────────────────────
    // ListUI와 동일한 비주얼 제어용 구조체
    // (배경 이미지와 TMP 라벨을 토글 상태에 맞춰 바꿔줌)
    // ─────────────────────────────────────────
    [Serializable]
    public class ToggleVisualPair
    {
        public Toggle toggle;           // 제어 대상 토글
        public GameObject background;   // 선택 시 켜질 배경
        public TMP_Text label;          // 선택 시 스타일이 바뀔 TMP 라벨
        [HideInInspector] public Material baseMaterial;  // 라벨용 인스턴스 머티리얼
        [HideInInspector] public bool materialReady;     // 중복 복제 방지 플래그
    }

    [Header("탭 토글(비주얼용) - 인스펙터에서 2개 순서대로 연결 (0=친구목록, 1=신청목록)")]
    [SerializeField] private List<ToggleVisualPair> tabToggles = new();  // 두 개만 사용

    [Header("프리팹 / 부모")]
    [SerializeField] private GameObject prefabFriendRow;    // Panel_Friend 프리팹 (자식: Btn_Delete, Txt_Name)
    [SerializeField] private GameObject prefabRequestRow;   // Panel_Request 프리팹 (자식: Btn_Delete, Btn_Accept, Txt_Name)
    [SerializeField] private Transform contentParent;       // 스크롤 콘텐츠 부모(공용)

    [Header("토스트(선택)")]
    [SerializeField] private CanvasGroup toastGroup;        // 토스트용 CanvasGroup
    [SerializeField] private TMP_Text toastText;            // 토스트 문구

    // Firestore 경로 약속:
    // nicknames/{lowerNickname}: { uid, createdAt }
    // profiles/{uid}: { nickname, email, createdAt }
    // mailboxes/{uid}/friendRequests/{fromUid}: { fromUid, fromNickname, status("pending"), createdAt }
    // friends/{uid}/list/{friendUid}: { friendUid, friendNickname, createdAt }

    FirebaseAuth auth;
    FirebaseFirestore db;

    readonly List<GameObject> _pooled = new(); // 간단한 재사용/정리용

    // ─────────────────────────────────────────
    // ListUI와 동일한 컬러/두께 파라미터
    // ─────────────────────────────────────────
    private readonly Color defaultTextColor      = new Color(185f/255f,185f/255f,185f/255f,1f);
    private readonly Color selectedTextColor     = new Color(1f,1f,1f,1f);
    private readonly Color selectedOutlineColor  = new Color(164f/255f,109f/255f,9f/255f,1f);
    private const float selectedOutlineWidth     = 0.18f;

    void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        db   = FirebaseFirestore.DefaultInstance;

        // (역할) 신청 버튼 클릭 시 요청 전송
        if (btnConfirm) btnConfirm.onClick.AddListener(OnClickSendRequest);

        // (역할) 토글 변화 시 목록 갱신 + 비주얼 갱신
        if (toggleFriendList)
        {
            toggleFriendList.onValueChanged.AddListener(isOn =>
            {
                if (isOn) _ = RefreshFriendList();   // 데이터 갱신
                UpdateAllToggleVisuals();            // 비주얼 갱신
            });
        }
        if (toggleRequestList)
        {
            toggleRequestList.onValueChanged.AddListener(isOn =>
            {
                if (isOn) _ = RefreshRequestList();  // 데이터 갱신
                UpdateAllToggleVisuals();            // 비주얼 갱신
            });
        }

        // (역할) TMP 라벨용 머티리얼 인스턴스 준비
        PrepareMaterialsIfNeeded();
    }

    void OnEnable()
    {
        // 기본 탭: 친구 목록
        if (toggleFriendList)  toggleFriendList.isOn = true;
        if (toggleRequestList) toggleRequestList.isOn = false;

        // 즉시 비주얼/데이터 동기화
        UpdateAllToggleVisuals();
        _ = RefreshFriendList();
    }

    // ─────────────────────────────────────────
    // (역할) 탭 라벨의 TMP 머티리얼을 인스턴스화
    //  - 공유 머티리얼 오염 방지 (ListUI와 동일 패턴)
    // ─────────────────────────────────────────
    void PrepareMaterialsIfNeeded()
    {
        foreach (var pair in tabToggles)
        {
            if (pair == null || pair.label == null || pair.materialReady) continue;
            pair.baseMaterial = new Material(pair.label.fontMaterial);
            pair.label.fontMaterial = pair.baseMaterial;
            pair.materialReady = true;
        }
    }

    // ─────────────────────────────────────────
    // (역할) 모든 탭 토글의 배경/라벨 스타일을 토글 상태에 맞게 갱신
    //  - ListUI.UpdateToggle과 동일한 파라미터 사용
    // ─────────────────────────────────────────
    void UpdateAllToggleVisuals()
    {
        foreach (var pair in tabToggles)
        {
            if (pair == null || pair.toggle == null || pair.label == null) continue;

            bool isOn = pair.toggle.isOn;

            // 배경 on/off
            if (pair.background) pair.background.SetActive(isOn);

            // 라벨 컬러
            pair.label.color = isOn ? selectedTextColor : defaultTextColor;

            // TMP 머티리얼 파라미터 (Outline/Underlay)
            var mat = pair.label.fontMaterial;
            if (isOn)
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, selectedOutlineWidth);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, selectedOutlineColor);
                // Underlay(그림자)
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1.5f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1.5f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.5f));
                pair.label.alpha = 1f;
            }
            else
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0f));
            }
        }
    }

    // ─────────────────────────────────────────
    // (역할) 토스트 표시: 문자열 표시 후 자동 페이드아웃
    // ─────────────────────────────────────────
    async void ShowToast(string message, float show = 1.2f, float fade = 0.25f)
    {
        if (!toastGroup || !toastText) { Debug.Log(message); return; }
        toastText.text = message;
        toastGroup.alpha = 1f;
        toastGroup.gameObject.SetActive(true);
        await Task.Delay(Mathf.RoundToInt(show * 1000));
        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            toastGroup.alpha = Mathf.Lerp(1f, 0f, t / fade);
            await Task.Yield();
        }
        toastGroup.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────
    // (역할) 친구 신청 버튼: 닉네임 → UID 조회 후 상대 메일박스에 요청 문서 생성
    // ─────────────────────────────────────────
    async void OnClickSendRequest()
    {
        string raw = inputNickname ? inputNickname.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(raw))
        {
            ShowToast("닉네임을 입력하세요.");
            return;
        }

        string lower = raw.ToLowerInvariant();

        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            var nickDoc = await db.Collection("nicknames").Document(lower).GetSnapshotAsync();
            if (!nickDoc.Exists)
            {
                ShowToast("사용자를 찾을 수 없습니다.");
                return;
            }

            string targetUid = nickDoc.GetValue<string>("uid");
            if (string.IsNullOrEmpty(targetUid) || targetUid == me.UserId)
            {
                ShowToast(targetUid == me.UserId ? "자기 자신에게는 신청할 수 없습니다." : "사용자를 찾을 수 없습니다.");
                return;
            }

            // 내 닉네임 읽기 (표시용)
            var myProfile = await db.Collection("profiles").Document(me.UserId).GetSnapshotAsync();
            string myNickname = myProfile.Exists && myProfile.TryGetValue("nickname", out string nn) ? nn : "unknown";

            // 상대 메일박스에 요청 생성
            var reqRef = db.Collection("mailboxes").Document(targetUid)
                           .Collection("friendRequests").Document(me.UserId);

            var payload = new Dictionary<string, object>
            {
                { "fromUid", me.UserId },
                { "fromNickname", myNickname },
                { "status", "pending" },
                { "createdAt", FieldValue.ServerTimestamp }
            };

            await reqRef.SetAsync(payload, SetOptions.MergeAll);
            ShowToast("친구 신청 되었습니다.");

            if (inputNickname) inputNickname.text = string.Empty;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("요청 중 오류가 발생했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 친구 목록 갱신: friends/{me}/list/* 로우 출력
    // ─────────────────────────────────────────
    public async Task RefreshFriendList()
    {
        ClearRows();
        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            var snap = await db.Collection("friends").Document(me.UserId)
                               .Collection("list")
                               .OrderByDescending("createdAt")
                               .GetSnapshotAsync();

            foreach (var doc in snap.Documents)
            {
                string friendUid = SafeGet(doc, "friendUid");
                string friendNickname = SafeGet(doc, "friendNickname");
                CreateFriendRow(friendUid, friendNickname);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("목록을 불러오지 못했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 신청 목록 갱신: mailboxes/{me}/friendRequests where status==pending
    // ─────────────────────────────────────────
    public async Task RefreshRequestList()
    {
        ClearRows();
        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            var q = db.Collection("mailboxes").Document(me.UserId)
                      .Collection("friendRequests")
                      .WhereEqualTo("status", "pending")
                      .OrderByDescending("createdAt");

            var snap = await q.GetSnapshotAsync();

            foreach (var doc in snap.Documents)
            {
                string fromUid = SafeGet(doc, "fromUid");
                string fromNickname = SafeGet(doc, "fromNickname");
                CreateRequestRow(fromUid, fromNickname);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("목록을 불러오지 못했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 친구 로우 생성: 삭제 버튼 연결
    // ─────────────────────────────────────────
    void CreateFriendRow(string friendUid, string nickname)
    {
        if (!prefabFriendRow || !contentParent) return;
        var go = Instantiate(prefabFriendRow, contentParent, false);
        _pooled.Add(go);

        var txt = FindTMP(go.transform, "Txt_Name");
        if (txt) txt.text = nickname;

        var btnDelete = FindButton(go.transform, "Btn_Delete");
        if (btnDelete) btnDelete.onClick.AddListener(() => _ = DeleteFriend(friendUid));
    }

    // ─────────────────────────────────────────
    // (역할) 신청 로우 생성: 수락/삭제 버튼 연결
    // ─────────────────────────────────────────
    void CreateRequestRow(string fromUid, string nickname)
    {
        if (!prefabRequestRow || !contentParent) return;
        var go = Instantiate(prefabRequestRow, contentParent, false);
        _pooled.Add(go);

        var txt = FindTMP(go.transform, "Txt_Name");
        if (txt) txt.text = nickname;

        var btnAccept = FindButton(go.transform, "Btn_Accept");
        if (btnAccept) btnAccept.onClick.AddListener(() => _ = AcceptRequest(fromUid, nickname));

        var btnDelete = FindButton(go.transform, "Btn_Delete");
        if (btnDelete) btnDelete.onClick.AddListener(() => _ = DeclineRequest(fromUid));
    }

    // ─────────────────────────────────────────
    // (역할) 친구 삭제: 양쪽 friends 문서 제거
    // ─────────────────────────────────────────
    async Task DeleteFriend(string friendUid)
    {
        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            var batch = db.StartBatch();

            var a = db.Collection("friends").Document(me.UserId).Collection("list").Document(friendUid);
            var b = db.Collection("friends").Document(friendUid).Collection("list").Document(me.UserId);

            batch.Delete(a);
            batch.Delete(b);

            await batch.CommitAsync();
            ShowToast("친구를 삭제했습니다.");
            await RefreshFriendList();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("삭제에 실패했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 친구 신청 수락: 양쪽 friends 추가 후 요청 삭제
    // ─────────────────────────────────────────
    async Task AcceptRequest(string fromUid, string fromNickname)
    {
        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            // 내 닉네임 조회(상대 목록에 저장용)
            var myProfile = await db.Collection("profiles").Document(me.UserId).GetSnapshotAsync();
            string myNickname = myProfile.Exists && myProfile.TryGetValue("nickname", out string nn) ? nn : "unknown";

            var batch = db.StartBatch();

            var myFriends = db.Collection("friends").Document(me.UserId).Collection("list").Document(fromUid);
            batch.Set(myFriends, new Dictionary<string, object> {
                { "friendUid", fromUid },
                { "friendNickname", fromNickname },
                { "createdAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);

            var hisFriends = db.Collection("friends").Document(fromUid).Collection("list").Document(me.UserId);
            batch.Set(hisFriends, new Dictionary<string, object> {
                { "friendUid", me.UserId },
                { "friendNickname", myNickname },
                { "createdAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);

            var req = db.Collection("mailboxes").Document(me.UserId)
                        .Collection("friendRequests").Document(fromUid);
            batch.Delete(req);

            await batch.CommitAsync();
            ShowToast("친구로 추가되었습니다.");
            await RefreshRequestList();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("수락에 실패했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 친구 신청 거절: 요청 문서만 삭제
    // ─────────────────────────────────────────
    async Task DeclineRequest(string fromUid)
    {
        try
        {
            var me = auth.CurrentUser;
            if (me == null) { ShowToast("로그인이 필요합니다."); return; }

            var req = db.Collection("mailboxes").Document(me.UserId)
                        .Collection("friendRequests").Document(fromUid);
            await req.DeleteAsync();

            ShowToast("신청을 삭제했습니다.");
            await RefreshRequestList();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("삭제에 실패했습니다.");
        }
    }

    // ─────────────────────────────────────────
    // (역할) 도큐먼트에서 안전하게 문자열 꺼내기
    // ─────────────────────────────────────────
    static string SafeGet(DocumentSnapshot doc, string key)
    {
        try { return doc.GetValue<string>(key); }
        catch { return string.Empty; }
    }

    // ─────────────────────────────────────────
    // (역할) 현재 스크롤 콘텐츠 정리
    // ─────────────────────────────────────────
    void ClearRows()
    {
        for (int i = 0; i < _pooled.Count; i++)
        {
            if (_pooled[i]) Destroy(_pooled[i]);
        }
        _pooled.Clear();
    }

    // ─────────────────────────────────────────
    // (역할) 자식에서 이름으로 Button/TMP_Text 찾기(프리팹 구조 고정 사용)
    // ─────────────────────────────────────────
    static Button FindButton(Transform root, string childName)
    {
        var t = root.Find(childName);
        return t ? t.GetComponent<Button>() : null;
    }

    static TMP_Text FindTMP(Transform root, string childName)
    {
        var t = root.Find(childName);
        return t ? t.GetComponent<TMP_Text>() : null;
    }
}
