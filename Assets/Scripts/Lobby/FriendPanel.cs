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

    [Header("탭 토글")]
    [SerializeField] private Toggle toggleFriendList;       // 친구 목록 탭
    [SerializeField] private Toggle toggleRequestList;      // 신청 목록 탭

    [Header("프리팹 / 부모")]
    [SerializeField] private GameObject prefabFriendRow;    // Panel_Friend 프리팹 (자식: Btn_Delete, Txt_Name)
    [SerializeField] private GameObject prefabRequestRow;   // Panel_Request 프리팹 (자식: Btn_Delete, Btn_Accept, Txt_Name)
    [SerializeField] private Transform contentParent;       // 스크롤 콘텐츠 부모(공용)

    [Header("토스트(선택)")]
    [SerializeField] private CanvasGroup toastGroup;        // 토스트용 CanvasGroup
    [SerializeField] private TMP_Text toastText;            // 토스트 문구

    // ─────────────────────────────────────────────────────────────────────────────
    // Firestore 경로 약속
    // nicknames/{lowerNickname}: { uid, createdAt }
    // profiles/{uid}: { nickname, email, createdAt }
    // mailboxes/{uid}/friendRequests/{fromUid}: { fromUid, fromNickname, status("pending"), createdAt }
    // friends/{uid}/list/{friendUid}: { friendUid, friendNickname, createdAt }
    // ─────────────────────────────────────────────────────────────────────────────
    FirebaseAuth auth;
    FirebaseFirestore db;

    readonly List<GameObject> _pooled = new(); // 간단한 재사용/정리용

    void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        db   = FirebaseFirestore.DefaultInstance;

        if (btnConfirm) btnConfirm.onClick.AddListener(OnClickSendRequest);
        if (toggleFriendList)  toggleFriendList.onValueChanged.AddListener(isOn => { if (isOn) _ = RefreshFriendList(); });
        if (toggleRequestList) toggleRequestList.onValueChanged.AddListener(isOn => { if (isOn) _ = RefreshRequestList(); });
    }

    void OnEnable()
    {
        // 기본 탭: 친구 목록
        if (toggleFriendList)  toggleFriendList.isOn = true;
        if (toggleRequestList) toggleRequestList.isOn = false;
        _ = RefreshFriendList();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // UI 토스트 표시: 문자열 표시 후 자동 페이드아웃
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 신청 버튼: 닉네임 → UID 조회 후 상대 메일박스에 요청 문서 생성
    // ─────────────────────────────────────────────────────────────────────────────
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

            // 자기 자신 금지
            // 닉네임 문서에서 uid를 찾는다.
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

            // 상대 메일박스에 요청 생성(덮어쓰기 방지: set merge)
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

            // 입력창 비우기
            if (inputNickname) inputNickname.text = string.Empty;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowToast("요청 중 오류가 발생했습니다.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 목록 갱신: friends/{me}/list/* 로우를 읽고 프리팹 인스턴스화
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 신청 목록 갱신: mailboxes/{me}/friendRequests where status==pending
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 로우 생성: 삭제 버튼 연결
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 신청 로우 생성: 수락/삭제 버튼 연결
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 삭제: 양쪽 friends 문서 제거
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 신청 수락: 양쪽 friends 추가 후 요청 삭제
    // ─────────────────────────────────────────────────────────────────────────────
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

            // A(나) 목록에 B 추가
            var myFriends = db.Collection("friends").Document(me.UserId).Collection("list").Document(fromUid);
            batch.Set(myFriends, new Dictionary<string, object> {
                { "friendUid", fromUid },
                { "friendNickname", fromNickname },
                { "createdAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);

            // B 목록에 A 추가
            var hisFriends = db.Collection("friends").Document(fromUid).Collection("list").Document(me.UserId);
            batch.Set(hisFriends, new Dictionary<string, object> {
                { "friendUid", me.UserId },
                { "friendNickname", myNickname },
                { "createdAt", FieldValue.ServerTimestamp }
            }, SetOptions.MergeAll);

            // 요청 삭제
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 친구 신청 거절: 요청 문서만 삭제
    // ─────────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────────
    // 유틸: 도큐먼트에서 안전하게 문자열 꺼내기
    // ─────────────────────────────────────────────────────────────────────────────
    static string SafeGet(DocumentSnapshot doc, string key)
    {
        try { return doc.GetValue<string>(key); }
        catch { return string.Empty; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 유틸: 현재 스크롤 콘텐츠 정리
    // ─────────────────────────────────────────────────────────────────────────────
    void ClearRows()
    {
        for (int i = 0; i < _pooled.Count; i++)
        {
            if (_pooled[i]) Destroy(_pooled[i]);
        }
        _pooled.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // 유틸: 자식에서 이름으로 Button/TMP_Text 찾기(프리팹 구조 고정 사용)
    // ─────────────────────────────────────────────────────────────────────────────
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
