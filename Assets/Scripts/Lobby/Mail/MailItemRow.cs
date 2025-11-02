using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using System.Collections.Generic;

/// <summary>
/// [역할] 우편 한 줄에 대한 표시 및 '받기' 버튼 동작.
///  - type == "SaleIncome": 금액을 내 지갑(InventoryRuntime)으로 지급하고 우편 isClaimed=true
///  - type == "FriendRequest": 여기선 '구분'만. 추후 친구 시스템과 연결.
/// </summary>
public class MailItemRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtAmount;
    [SerializeField] private Button btnClaim;

    private DocumentReference mailRef;
    private string mailType;
    private int amount;
    private bool isClaimed;
    private System.Action onClaimed;

    /// <summary> [역할] 외부 바인딩(문서/표시/상태) </summary>
    public void Bind(DocumentReference mailRef, string type, string title, int amount, bool isClaimed, System.Action onClaimed = null)
    {
        this.mailRef = mailRef;
        this.mailType = type;
        this.amount = Mathf.Max(0, amount);
        this.isClaimed = isClaimed;
        this.onClaimed = onClaimed;

        if (txtTitle)  txtTitle.text  = string.IsNullOrEmpty(title) ? "우편" : title;
        if (txtAmount) txtAmount.text = (type == "SaleIncome") ? $"{this.amount}" : "";

        if (btnClaim)
        {
            btnClaim.onClick.RemoveAllListeners();
            btnClaim.onClick.AddListener(() => _ = OnClickClaimAsync());
            btnClaim.interactable = !isClaimed && (type == "SaleIncome"); // 친구요청이면 다른 처리(비활성)
        }
    }

    /// <summary>
    /// [역할] '받기' 버튼: 판매 수익이면 골드 지급 → 우편 isClaimed=true 로 마감
    /// </summary>
    private async Task OnClickClaimAsync()
    {
        if (mailRef == null || isClaimed) return;

    if (mailType == "SaleIncome" && amount > 0)
    {
        // 1) 내 지갑에 골드 지급
        InventoryRuntime.Instance?.AddGold(amount);
    }

    // 2) 우편 문서 상태 갱신(isClaimed=true, claimedAt)
    try
    {
        await mailRef.UpdateAsync(new Dictionary<string, object>
        {
            { "isClaimed", true },
            { "claimedAt", FieldValue.ServerTimestamp }
        });
    }
    catch (System.Exception e)
    {
        Debug.LogWarning($"[Mail] 수령 상태 업데이트 실패: {e.Message}");
    }

    // 3) UI 즉시 반영
    isClaimed = true;

    // (기존: 버튼 비활성만)
    // if (btnClaim) btnClaim.interactable = false;

    // ✅ (변경) 해당 우편 오브젝트 즉시 제거
    if (onClaimed != null)
        onClaimed.Invoke();    // MailboxUI에서 Destroy(rowGo) 실행
    else
        Destroy(gameObject);   // 혹시 onClaimed 연결 안된 경우도 안전 처리
    }
}
