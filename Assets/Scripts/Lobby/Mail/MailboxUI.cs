using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Firestore;

/// <summary>
/// [역할] 내 우편함 목록을 불러와서 프리팹으로 출력한다.
///   - mailboxes/{myUid}/inbox 에서 최신순 정렬
///   - 각 행은 MailItemRow가 '받기' 동작을 처리
/// </summary>
public class MailboxUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;          // 스크롤뷰 Content
    [SerializeField] private GameObject mailRowPrefab;   // 행 프리팹(제목, 코인, 금액, 받기 버튼)
    [SerializeField] private Button btnAcceptAll;

    void OnEnable()
    {
        if (btnAcceptAll)
        {
            btnAcceptAll.onClick.RemoveAllListeners();
            btnAcceptAll.onClick.AddListener(() => _ = OnClickClaimAllAsync());
        }

        _ = RefreshAsync();
    }

    /// <summary> [역할] 우편함 새로고침(목록 채우기) </summary>
    public async Task RefreshAsync()
    {
        if (!content || !mailRowPrefab) return;

    // 기존 정리
    for (int i = content.childCount - 1; i >= 0; i--)
        Destroy(content.GetChild(i).gameObject);

    var uid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(uid)) return;

    var db = FirebaseFirestore.DefaultInstance;
    var q = db.Collection("mailboxes").Document(uid).Collection("inbox")
              .OrderByDescending("createdAt");

    QuerySnapshot snap = null;
    try { snap = await q.GetSnapshotAsync(); }
    catch { snap = await db.Collection("mailboxes").Document(uid).Collection("inbox").GetSnapshotAsync(); }

    foreach (var doc in snap.Documents)
    {
        string type     = SafeStr(doc, "type");
        string title    = SafeStr(doc, "title") ?? "우편";
        int    amount   = SafeInt(doc, "amount");
        bool   isClaimed= SafeBool(doc, "isClaimed");

        // ✅ 수령 완료된 판매 수익 우편은 목록에서 제외
        if (type == "SaleIncome" && isClaimed) continue;

        // (친구요청 등 다른 타입은 추후 상태값 따라 필터링 추가 가능)
        var rowGo = Instantiate(mailRowPrefab, content);
        var row   = rowGo.GetComponent<MailItemRow>();
        if (row != null)
        {
            row.Bind(doc.Reference, type, title, amount, isClaimed, onClaimed: () =>
            {
                // [역할] 한 건 수령 시 UI에서 즉시 제거
                Destroy(rowGo);
            });
        }
        else
        {
            // 프리팹에 MailItemRow가 없을 때의 최소 바인딩 (옵션)
            rowGo.transform.Find("Txt_Content")?.GetComponent<TMPro.TMP_Text>()?.SetText(title);
            rowGo.transform.Find("Txt_Price")?.GetComponent<TMPro.TMP_Text>()?.SetText($"{amount}");
            var btn = rowGo.transform.Find("Btn_Accept")?.GetComponent<UnityEngine.UI.Button>();
            if (btn) btn.interactable = !isClaimed;
        }
    }
    }

    /// <summary>
    /// [역할] '모두 받기' 버튼 - 아직 받지 않은 판매 수익 우편만 전부 수령.
    /// </summary>
    private async Task OnClickClaimAllAsync()
    {
        var uid = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var inbox = db.Collection("mailboxes").Document(uid).Collection("inbox");

        try
        {
            // 아직 받지 않은 판매 수익 우편 전부 조회
            var snap = await inbox
                .WhereEqualTo("type", "SaleIncome")
                .WhereEqualTo("isClaimed", false)
                .GetSnapshotAsync();

            if (snap.Count == 0)
            {
                Debug.Log("[Mail] 받을 판매 수익이 없습니다.");
                return;
            }

            int totalGold = 0;

            // 일괄 업데이트: 각 문서에 isClaimed=true, claimedAt=서버시간
            var batch = db.StartBatch();

            foreach (var doc in snap.Documents)
            {
                int amount = 0;
                try { amount = doc.GetValue<int>("amount"); } catch { amount = 0; }

                if (amount > 0)
                    totalGold += amount;

                batch.Update(doc.Reference, new Dictionary<string, object>
            {
                { "isClaimed", true },
                { "claimedAt", Firebase.Firestore.FieldValue.ServerTimestamp }
            });
            }

            // 골드 지급 + Firestore 반영
            if (totalGold > 0)
            {
                InventoryRuntime.Instance?.AddGold(totalGold);
                Debug.Log($"[Mail] 모두 받기: 총 {totalGold} 골드 수령");
            }

            await batch.CommitAsync();

            // UI 즉시 새로고침
            await RefreshAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Mail] 모두 받기 실패: {e.Message}");
        }
    }


    // ── 안전 파서
    private string SafeStr(DocumentSnapshot d, string f) { try { return d.GetValue<string>(f); } catch { return null; } }
    private int    SafeInt(DocumentSnapshot d, string f) { try { return d.GetValue<int>(f); } catch { return 0; } }
    private bool   SafeBool(DocumentSnapshot d, string f) { try { return d.GetValue<bool>(f); } catch { return false; } }
}
