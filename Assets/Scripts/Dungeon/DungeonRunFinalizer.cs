using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonRunFinalizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string mainSceneName = "MainScene";   // 전환할 메인 씬 이름
    [SerializeField] private bool clearRunRewardAfterSave = true;  // 저장 후 러닝 보상 초기화

    [Header("Optional UI")]
    [SerializeField] private CanvasGroup blocker;                  // 중복 탭 방지용 블로커
    [SerializeField] private GameObject savingToast;               // "저장 중..." 같은 토스트

    /// <summary>
    /// [탐험 종료] 버튼용 엔트리 포인트.
    /// - 모바일 더블탭 대비로 중복 실행 막고, 예외가 떠도 씬 상태 손상 없도록 안전 처리.
    /// </summary>
    public void OnClickEndRunAndReturn()
    {
        // 버튼 스팸 방지
        if (blocker) { blocker.blocksRaycasts = true; blocker.alpha = 1f; }
        if (savingToast) savingToast.SetActive(true);

        // 비동기 파이프라인 실행
        _ = EndRunPipelineAsync();
    }

    /// <summary>
    /// 실제 파이프라인: 보상 반영 → 저장 → 정리 → 씬 전환
    /// </summary>
    private async Task EndRunPipelineAsync()
    {
        try
        {
            // 1) 이번 러닝 누적 보상 취득
            var reward = RunReward.Instance ? RunReward.Instance.GetTotals() : null;

            // 2) 보상 지갑 반영 (런타임 수치 증가)
            ApplyRunRewardsToWallet(reward);

            // 3) (선택) 던전 결과창이 열려 있다면 닫는 처리 등 UI 정리
            var dm = DungeonManager.instance;
            if (dm) dm.HideDungeonClearUI();

            // 4) 저장 (영웅 HP/EXP/레벨, 인벤/재화 포함)
            //    PlayerProgressService는 런타임 객체를 Save DTO로 캡처해 파일에 기록함
            var progress = PlayerProgressService.Instance;
            if (progress != null)
            {
                bool ok = await progress.SaveAsync();
#if UNITY_EDITOR
                if (!ok) Debug.LogWarning("[EndRun] SaveAsync returned false.");
#endif
            }

            // 5) 러닝 한정 데이터 정리(다음 던전을 위한 초기화)
            if (clearRunRewardAfterSave && RunReward.Instance) RunReward.Instance.Clear();
            // 필요 시 던전 인벤토리도 초기화
            // var dungeonInv = FindObjectOfType<DungeonInventory>(true);
            // if (dungeonInv) dungeonInv.ResetAll();

            // 6) 메인 씬으로 전환
            SceneManager.LoadScene(mainSceneName);
        }
        finally
        {
            // 블로커/토스트 해제 (씬 넘어가면 자동 파괴되지만, 예외 대비)
            if (savingToast) savingToast.SetActive(false);
            if (blocker) { blocker.blocksRaycasts = false; blocker.alpha = 0f; }
        }
    }

    /// <summary>
    /// RunReward 누적치를 InventoryRuntime 지갑(골드/소울)에 반영.
    /// - 런타임 수치만 올리면 PlayerProgressService가 저장 시점에 함께 캡처해서 파일에 기록.
    /// </summary>
    private void ApplyRunRewardsToWallet(DungeonRunRewardsData totals)
    {
        if (totals == null) return;

        var wallet = InventoryRuntime.Instance;
        if (wallet == null) return;

        // 코인
        wallet.Gold = Mathf.Max(0, wallet.Gold + Mathf.Max(0, totals.coins));

        // 소울류 (인덱스: RunReward의 enum과 순서 맞춤)
        wallet.redSoul  = Mathf.Max(0, wallet.redSoul  + GetSafe(totals.souls, 0));
        wallet.blueSoul = Mathf.Max(0, wallet.blueSoul + GetSafe(totals.souls, 1));
        wallet.greenSoul= Mathf.Max(0, wallet.greenSoul+ GetSafe(totals.souls, 2));

#if UNITY_EDITOR
        Debug.Log($"[EndRun] Applied rewards → Gold {wallet.Gold}, Souls [{wallet.redSoul},{wallet.blueSoul},{wallet.greenSoul}]");
#endif
    }

    /// <summary>
    /// 배열 인덱스 안전 접근(없으면 0)
    /// </summary>
    private int GetSafe(int[] arr, int idx)
    {
        if (arr == null || idx < 0 || idx >= arr.Length) return 0;
        return Mathf.Max(0, arr[idx]);
    }
}
