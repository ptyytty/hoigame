using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// [모바일 빌드/배포용] Firestore 권한/규칙 진단용 프로브 모음
/// - 버튼에 연결해서 호출하면, 어떤 경로/연산이 규칙에 막히는지 콘솔에 명확히 남깁니다.
/// - 모든 메서드는 실행 전 로그인/초기화 상태를 확인합니다.
/// </summary>
public class FirestoreDebugPorbes : MonoBehaviour
{
    [Header("공통 테스트 파라미터")]
    [Tooltip("marketListings 생성 시 사용할 itemId")]
    public string testItemId = "potion_small";

    [Tooltip("marketListings 생성 시 사용할 type (규칙은 'Consume' 또는 'Equipment'만 허용)")]
    public string testItemType = "Consume";

    [Tooltip("marketListings 생성/수정 시 사용할 가격")]
    public int testPriceGold = 100;

    [Tooltip("marketListings 생성/수정 시 사용할 수량")]
    public int testQty = 1;

    [Space(10)]
    [Header("친구 요청 테스트 파라미터")]
    [Tooltip("요청을 받을 대상의 UID (내 UID와 달라야 함)")]
    public string toUid = "<TARGET_UID>";

    [Tooltip("nicknames/{toNicknameLower} 문서 키 (반드시 소문자)")]
    public string toNicknameLower = "targetnick";

    [Space(10)]
    [Header("우편 읽음 처리 테스트 파라미터")]
    [Tooltip("내 UID (보통은 CurrentUser.uid 사용)")]
    public string mailboxOwnerUid = "<MY_UID>";
    [Tooltip("mailbox/{uid}/mails/{mailId} 의 mailId")]
    public string testMailId = "<MAIL_ID>";
    [Tooltip("isRead 로 설정할 값")]
    public bool mailIsReadValue = true;

    // 마지막에 생성된 marketListings 문서 참조 (업데이트 테스트에 사용)
    private DocumentReference _lastCreatedListing;

    // ------------- 내부 공통 유틸 -------------

    /// <summary>
    /// [역할] 현재 로그인/초기화 상태가 규칙 요구사항을 만족하는지 검사
    /// - Auth 인스턴스 및 CurrentUser(UID) 존재 여부 확인
    /// - 실패 시 false 반환 및 자세한 로그 출력
    /// </summary>
    private bool EnsureAuthReady()
    {
        var auth = FirebaseAuth.DefaultInstance;
        if (auth == null)
        {
            Debug.LogError("[AUTH] FirebaseAuth.DefaultInstance == null. Firebase 초기화가 아직 안 되었거나 App이 준비되지 않았습니다.");
            return false;
        }

        var user = auth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("[AUTH] 로그인된 사용자가 없습니다. (request.auth == null → 규칙 전부 거절) 로그인 완료 후 실행하세요.");
            return false;
        }

        Debug.Log($"[AUTH] OK. uid={user.UserId}, email={user.Email}");
        return true;
    }

    /// <summary>
    /// [역할] Firestore 호출 전후로 경로/연산/UID 로그 출력
    /// - 어떤 경로/연산이 막히는지 콘솔에서 바로 확인 가능
    /// </summary>
    private void LogFirestoreCall(string op, DocumentReference docRef)
    {
        var uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId ?? "NULL";
        Debug.Log($"[FS-CALL] op={op} path={docRef.Path} uid={uid}");
    }

    // ------------- marketListings 테스트 -------------

    /// <summary>
    /// [역할] 규칙 A(상점): marketListings 문서 생성(create) 캔어리 테스트
    /// - 허용 조건: 로그인 + 유효 필드 + 금지 필드 없음
    /// - 성공 시 _lastCreatedListing에 참조 저장 (업데이트 테스트에서 사용)
    /// </summary>
    public void Probe_CreateMarketListing_Button()
    {
        _ = Probe_CreateMarketListing();
    }

    private async Task Probe_CreateMarketListing()
    {
        if (!EnsureAuthReady()) return;

        var fs = FirebaseFirestore.DefaultInstance;

        // 규칙에 맞는 최소 페이로드 구성
        var data = new Dictionary<string, object> {
            { "sellerUid", FirebaseAuth.DefaultInstance.CurrentUser.UserId },  // 본인 명의
            { "itemId", string.IsNullOrWhiteSpace(testItemId) ? "potion_small" : testItemId },
            { "type", testItemType },                                          // "Consume" 또는 "Equipment"
            { "priceGold", testPriceGold },                                    // int >= 0
            { "qty", testQty },                                                // int > 0
            { "isActive", true },                                              // 판매 전(active)
            { "createdAt", FieldValue.ServerTimestamp }                        // timestamp
            // 금지: buyerUid, soldAt
        };

        var docRef = fs.Collection("marketListings").Document(); // auto-id
        LogFirestoreCall("create", docRef);

        try
        {
            await docRef.SetAsync(data);
            _lastCreatedListing = docRef;
            Debug.Log($"[FS-OK] marketListings create passed. id={docRef.Id}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FS-ERR] marketListings create failed: {e}");
        }
    }

    /// <summary>
    /// [역할] 규칙 A(상점): 마지막 생성 문서에 대해 priceGold 또는 qty만 update 허용 여부 테스트
    /// - UpdateAsync로 변경 키만 전송해야 규칙을 통과함
    /// </summary>
    public void Probe_UpdateMarketListing_Button(bool changePrice = true)
    {
        _ = Probe_UpdateMarketListing(changePrice);
    }

    private async Task Probe_UpdateMarketListing(bool changePrice)
    {
        if (!EnsureAuthReady()) return;

        if (_lastCreatedListing == null)
        {
            Debug.LogWarning("[FS] 업데이트할 문서가 없습니다. 먼저 'Probe_CreateMarketListing_Button'을 실행하세요.");
            return;
        }

        var payload = new Dictionary<string, object>();
        if (changePrice) payload["priceGold"] = Mathf.Max(0, testPriceGold + 1);
        else payload["qty"] = Mathf.Max(1, testQty + 1);

        LogFirestoreCall("update", _lastCreatedListing);

        try
        {
            await _lastCreatedListing.UpdateAsync(payload); // 허용된 키만 전송
            Debug.Log("[FS-OK] marketListings update passed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FS-ERR] marketListings update failed: {e}");
        }
    }

    // ------------- nicknames 읽기 테스트 -------------

    /// <summary>
    /// [역할] 규칙 B(닉네임): 로그인 필요(read). 로그인 전이면 PERMISSION_DENIED 발생.
    /// - toNicknameLower는 반드시 소문자 키여야 함.
    /// </summary>
    public void Probe_ReadNickname_Button()
    {
        _ = Probe_ReadNickname();
    }

    private async Task Probe_ReadNickname()
    {
        if (!EnsureAuthReady()) return;

        var lower = (toNicknameLower ?? "").Trim();
        if (string.IsNullOrEmpty(lower))
        {
            Debug.LogWarning("[FS] toNicknameLower가 비었습니다.");
            return;
        }

        var fs = FirebaseFirestore.DefaultInstance;
        var docRef = fs.Collection("nicknames").Document(lower);
        LogFirestoreCall("read", docRef);

        try
        {
            var snap = await docRef.GetSnapshotAsync();
            Debug.Log($"[FS-OK] nicknames read passed. exists={snap.Exists}");
            if (snap.Exists)
            {
                var dict = snap.ToDictionary();
                var uid = dict.ContainsKey("uid") ? dict["uid"] : "<no uid field>";
                Debug.Log($"[FS] nicknames/{lower}.uid = {uid}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FS-ERR] nicknames read failed: {e}");
        }
    }

    // ------------- friendRequests 생성 테스트 -------------

    /// <summary>
    /// [역할] 규칙 C(친구요청) create 캔어리:
    /// - 조건: toUid != 내 uid, status='pending', createdAt timestamp,
    ///         nicknames/{toNicknameLower}.uid == toUid 이어야 통과
    /// - 가장 자주 막히는 지점이므로 실패 시 콘솔의 op/path/uid와 함께 원인 추정 용이
    /// </summary>
    public void Probe_CreateFriendRequest_Button()
    {
        _ = Probe_CreateFriendRequest();
    }

    private async Task Probe_CreateFriendRequest()
    {
        if (!EnsureAuthReady()) return;

        var myUid = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        if (string.IsNullOrWhiteSpace(toUid) || toUid == "<TARGET_UID>")
        {
            Debug.LogWarning("[FS] toUid가 설정되지 않았습니다. 실제 상대 UID를 넣어주세요.");
            return;
        }
        if (toUid == myUid)
        {
            Debug.LogWarning("[FS] 자기 자신에게는 요청을 보낼 수 없습니다. toUid != myUid 이어야 합니다.");
            return;
        }
        var lower = (toNicknameLower ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(lower))
        {
            Debug.LogWarning("[FS] toNicknameLower가 비었습니다(소문자).");
            return;
        }

        var fs = FirebaseFirestore.DefaultInstance;
        var docRef = fs.Collection("friendRequests").Document(toUid)
                       .Collection("inbox").Document(); // auto-id

        var data = new Dictionary<string, object>
        {
            { "fromUid", myUid },
            { "toUid", toUid },
            { "status", "pending" },
            { "createdAt", FieldValue.ServerTimestamp },
            { "toNicknameLower", lower }
        };

        LogFirestoreCall("create", docRef);

        try
        {
            await docRef.SetAsync(data);
            Debug.Log("[FS-OK] friendRequests create passed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FS-ERR] friendRequests create failed: {e}");
        }
    }

    // ------------- mailbox isRead 업데이트 테스트 -------------

    /// <summary>
    /// [역할] 규칙 E(우편함): 본인만 읽기/수정 가능 + 수정은 isRead 키만 허용
    /// - 다른 키를 섞으면 규칙 위반으로 거절됨
    /// - create/delete는 클라이언트 금지(항상 거절)
    /// </summary>
    public void Probe_UpdateMailIsRead_Button()
    {
        _ = Probe_UpdateMailIsRead();
    }

    private async Task Probe_UpdateMailIsRead()
    {
        if (!EnsureAuthReady()) return;

        var me = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        var owner = string.IsNullOrWhiteSpace(mailboxOwnerUid) || mailboxOwnerUid == "<MY_UID>"
            ? me : mailboxOwnerUid;

        if (string.IsNullOrWhiteSpace(testMailId) || testMailId == "<MAIL_ID>")
        {
            Debug.LogWarning("[FS] testMailId가 설정되지 않았습니다.");
            return;
        }

        var fs = FirebaseFirestore.DefaultInstance;
        var docRef = fs.Collection("mailbox").Document(owner)
                       .Collection("mails").Document(testMailId);

        LogFirestoreCall("update", docRef);

        try
        {
            await docRef.UpdateAsync(new Dictionary<string, object> {
                { "isRead", mailIsReadValue }
            });
            Debug.Log("[FS-OK] mailbox update (isRead) passed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FS-ERR] mailbox update failed: {e}");
        }
    }

    // ------------- 편의: 에디터에서 바로 실행 버튼 -------------

#if UNITY_EDITOR
    [CustomEditor(typeof(FirestoreDebugPorbes))]
    private class FirestoreDebugProbesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var t = (FirestoreDebugPorbes)target;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("=== Firestore 캔어리 테스트 ===", EditorStyles.boldLabel);

            if (GUILayout.Button("1) marketListings 생성 (create)"))
                t.Probe_CreateMarketListing_Button();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("2) marketListings 업데이트 (priceGold만)"))
                    t.Probe_UpdateMarketListing_Button(true);
                if (GUILayout.Button("2) marketListings 업데이트 (qty만)"))
                    t.Probe_UpdateMarketListing_Button(false);
            }

            GUILayout.Space(6);
            if (GUILayout.Button("3) nicknames 읽기 (로그인 필요)"))
                t.Probe_ReadNickname_Button();

            if (GUILayout.Button("4) friendRequests 생성 (cross-check 포함)"))
                t.Probe_CreateFriendRequest_Button();

            GUILayout.Space(6);
            if (GUILayout.Button("5) mailbox isRead 업데이트 (해당 소유자만)"))
                t.Probe_UpdateMailIsRead_Button();
        }
    }
#endif
}
