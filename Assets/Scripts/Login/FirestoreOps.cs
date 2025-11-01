using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// [모바일 빌드용] Firestore 허용 연산만 안전하게 호출하는 래퍼
/// - 규칙에 맞춘 payload/경로만 전송하도록 강제
/// - 콘솔 로그로 path/op/uid를 항상 표시해 디버깅에 도움
/// </summary>
public class FirestoreOps : MonoBehaviour
{
    private FirebaseFirestore FS => FirebaseFirestore.DefaultInstance;

    /// <summary>
    /// [역할] Firestore 호출 전에 로그인/준비 상태를 확인
    /// </summary>
    private bool CanCall(out string uid)
    {
        uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        if (!AuthBootstrap.Instance || !AuthBootstrap.Instance.IsFirebaseReady)
        { Debug.LogError("[FS] Firebase not ready."); return false; }
        if (string.IsNullOrEmpty(uid))
        { Debug.LogError("[FS] Not signed in (uid null)."); return false; }
        return true;
    }

    /// <summary>
    /// [역할] 콘솔에서 어떤 경로/연산이 수행되는지 로그
    /// </summary>
    private void LogCall(string op, DocumentReference doc, string uid)
    {
        Debug.Log($"[FS-CALL] op={op} path={doc.Path} uid={uid}");
    }

    // ---------------- A. marketListings ----------------

    /// <summary>
    /// [역할] 규칙 A에 맞는 판매글 생성 (허용 필드만 전송)
    /// </summary>
    public async Task CreateListing(string itemId, string type, int priceGold, int qty)
    {
        if (!CanCall(out var uid)) return;

        if (type != "Consume" && type != "Equipment")
        {
            Debug.LogError("[FS] type must be 'Consume' or 'Equipment' to pass rules.");
            return;
        }
        if (priceGold < 0 || qty <= 0)
        {
            Debug.LogError("[FS] priceGold >= 0 and qty > 0 required by rules.");
            return;
        }

        var doc = FS.Collection("marketListings").Document(); // auto-id
        LogCall("create", doc, uid);

        var data = new Dictionary<string, object> {
            { "sellerUid", uid },
            { "itemId", itemId },
            { "type", type },                // EXACT string
            { "priceGold", priceGold },      // int
            { "qty", qty },                  // int
            { "isActive", true },            // bool
            { "createdAt", FieldValue.ServerTimestamp }
            // 금지: buyerUid, soldAt
        };

        try { await doc.SetAsync(data); Debug.Log("[FS-OK] Listing created."); }
        catch (Exception e) { Debug.LogError($"[FS-ERR] Listing create failed: {e}"); }
    }

    /// <summary>
    /// [역할] 규칙 A: priceGold/qty만 update 허용. UpdateAsync로 변경키만 보냄.
    /// </summary>
    public async Task UpdateListingPrice(string listingId, int newPriceGold)
    {
        if (!CanCall(out var uid)) return;
        if (newPriceGold < 0) { Debug.LogError("[FS] newPriceGold must be >= 0."); return; }

        var doc = FS.Collection("marketListings").Document(listingId);
        LogCall("update", doc, uid);

        try { await doc.UpdateAsync(new Dictionary<string, object> { { "priceGold", newPriceGold } }); Debug.Log("[FS-OK] Listing price updated."); }
        catch (Exception e) { Debug.LogError($"[FS-ERR] Listing update failed: {e}"); }
    }

    // ---------------- B. nicknames (read only) ----------------

    /// <summary>
    /// [역할] 규칙 B: 로그인 필요. lowerName(소문자) 문서를 읽음.
    /// </summary>
    public async Task ReadNickname(string lowerName)
    {
        if (!CanCall(out var uid)) return;
        if (string.IsNullOrWhiteSpace(lowerName)) { Debug.LogError("[FS] lowerName empty."); return; }

        var doc = FS.Collection("nicknames").Document(lowerName.ToLowerInvariant());
        LogCall("read", doc, uid);

        try
        {
            var snap = await doc.GetSnapshotAsync();
            Debug.Log($"[FS-OK] nicknames/{lowerName} exists={snap.Exists}");
        }
        catch (Exception e) { Debug.LogError($"[FS-ERR] nickname read failed: {e}"); }
    }

    // ---------------- C. friendRequests (create only from client) ----------------

    /// <summary>
    /// [역할] 규칙 C: 친구요청 생성.
    /// - toUid != 내 uid
    /// - toNicknameLower 문서는 nicknames/{toNicknameLower}.uid == toUid 이어야 통과
    /// </summary>
    public async Task CreateFriendRequest(string toUid, string toNicknameLower)
    {
        if (!CanCall(out var uid)) return;
        if (string.IsNullOrWhiteSpace(toUid) || toUid == uid) { Debug.LogError("[FS] invalid toUid."); return; }
        if (string.IsNullOrWhiteSpace(toNicknameLower)) { Debug.LogError("[FS] toNicknameLower empty."); return; }

        var doc = FS.Collection("friendRequests").Document(toUid)
                     .Collection("inbox").Document(); // auto-id
        LogCall("create", doc, uid);

        var data = new Dictionary<string, object> {
            { "fromUid", uid },
            { "toUid", toUid },
            { "status", "pending" },
            { "createdAt", FieldValue.ServerTimestamp },
            { "toNicknameLower", toNicknameLower.ToLowerInvariant() }
        };

        try { await doc.SetAsync(data); Debug.Log("[FS-OK] friend request created."); }
        catch (Exception e) { Debug.LogError($"[FS-ERR] friend request failed: {e}"); }
    }

    // ---------------- E. mailbox (client can update only isRead) ----------------

    /// <summary>
    /// [역할] 규칙 E: 본인 우편의 isRead만 변경 가능. create/delete는 클라이언트 금지.
    /// </summary>
    public async Task UpdateMailIsRead(string ownerUid, string mailId, bool isRead)
    {
        if (!CanCall(out var uid)) return;
        var target = string.IsNullOrWhiteSpace(ownerUid) ? uid : ownerUid;
        if (string.IsNullOrWhiteSpace(mailId)) { Debug.LogError("[FS] mailId empty."); return; }

        var doc = FS.Collection("mailbox").Document(target)
                     .Collection("mails").Document(mailId);
        LogCall("update", doc, uid);

        try { await doc.UpdateAsync(new Dictionary<string, object> { { "isRead", isRead } }); Debug.Log("[FS-OK] mail isRead updated."); }
        catch (Exception e) { Debug.LogError($"[FS-ERR] mailbox update failed: {e}"); }
    }
}
