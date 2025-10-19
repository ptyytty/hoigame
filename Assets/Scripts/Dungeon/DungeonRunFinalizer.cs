using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonRunFinalizer : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene"; // [역할] 복귀 대상 씬 이름

    [Header("Optional UI")]
    [SerializeField] private CanvasGroup blocker;    // [역할] 저장/이동 중 입력 차단(모바일 탭 오입력 방지)
    [SerializeField] private GameObject savingToast; // [역할] "저장중..." 간단 토스트

    [Header("Options")]
    [Tooltip("저장 전에 어둡게(잔상/깜빡임 방지 권장)")]
    [SerializeField] private bool fadeBeforeSave = true;

    private bool _busy; // [역할] 버튼 중복 클릭 방지

    /// <summary>
    /// [버튼 핸들러] 전투 종료 → 로비 복귀.
    /// 버튼의 OnClick에 이 메서드를 연결하세요.
    /// </summary>
    public void OnClickEndRunAndReturn()
    {
        if (_busy) return; // 중복 탭 방지
        _busy = true;

        if (blocker) { blocker.blocksRaycasts = true; blocker.alpha = 1f; }
        if (savingToast) savingToast.SetActive(true);

        _ = EndRunPipelineAsync();
    }

    /// <summary>
    /// [역할] 엔드런 파이프라인:
    /// - fadeBeforeSave = true  → (1) 페이드 아웃 (2) 보상/저장 (3) 즉시 씬 로드
    /// - fadeBeforeSave = false → (1) 보상/저장 (2) 페이드 아웃 + 씬 로드
    /// → 언제나 '페이드 아웃은 한 번만' 수행되도록 분기
    /// </summary>
    private async Task EndRunPipelineAsync()
    {
        var fader = FindObjectOfType<SceneFader>(includeInactive: true);

        try
        {
            if (fadeBeforeSave && fader != null)
            {
                // (1) 저장 전 어둡게: 이후엔 다시 페이드 호출 금지
                await fader.FadeOutAsync();
                HideDungeonUIInstantIfPossible();
            }

            // (2) 보상 반영 + 저장
            ApplyRunRewardsToWallet(RunReward.Instance ? RunReward.Instance.GetTotals() : null);
            await SaveProgressAsync();

            // (3) 씬 전환
            if (fader != null)
            {
                if (fadeBeforeSave)
                {
                    // 이미 검정 상태 → 바로 로드 (중복 페이드 호출 제거)
                    SceneManager.LoadScene(mainSceneName);
                }
                else
                {
                    // 아직 밝은 상태 → 한 번만 페이드 후 로드
                    await fader.FadeOutAndLoadSceneAsync(mainSceneName);
                }
            }
            else
            {
                // Fader가 없으면 즉시 로드
                SceneManager.LoadScene(mainSceneName);
            }
        }
        finally
        {
            if (savingToast) savingToast.SetActive(false);
            if (blocker) { blocker.blocksRaycasts = false; blocker.alpha = 0f; }
            _busy = false;
        }
    }

    // ───────────────────── 보조 메서드들 ─────────────────────

    /// <summary>
    /// [역할] 런 보상 데이터를 지갑(인벤토리런타임)에 반영.
    /// </summary>
    private void ApplyRunRewardsToWallet(DungeonRunRewardsData totals)
    {
        if (totals == null) return;
        var wallet = InventoryRuntime.Instance;
        if (wallet == null) return;

        wallet.Gold      = Mathf.Max(0, wallet.Gold + Mathf.Max(0, totals.coins));
        wallet.redSoul   = Mathf.Max(0, wallet.redSoul   + GetSafe(totals.souls, 0));
        wallet.blueSoul  = Mathf.Max(0, wallet.blueSoul  + GetSafe(totals.souls, 1));
        wallet.greenSoul = Mathf.Max(0, wallet.greenSoul + GetSafe(totals.souls, 2));

#if UNITY_EDITOR
        Debug.Log($"[EndRun] Rewards Applied → Gold:{wallet.Gold}, Souls:[{wallet.redSoul},{wallet.blueSoul},{wallet.greenSoul}]");
#endif

        if (RunReward.Instance) RunReward.Instance.Clear();
    }

    /// <summary>
    /// [역할] 진행도 저장(비동기). 서비스가 없으면 아무 일도 하지 않음.
    /// </summary>
    private async Task SaveProgressAsync()
    {
        var progress = PlayerProgressService.Instance;
        if (progress == null) return;

        bool ok = await progress.SaveAsync();
#if UNITY_EDITOR
        if (!ok) Debug.LogWarning("[EndRun] SaveAsync returned false.");
#endif
    }

    /// <summary>
    /// [역할] 씬 전환 직전 던전 UI 루트를 즉시 숨겨 잔상 방지(선택).
    /// </summary>
    private void HideDungeonUIInstantIfPossible()
    {
        var dm = DungeonManager.instance;
        // 필요한 경우: if (dm && dm.dungeonRoot) dm.dungeonRoot.SetActive(false);
    }

    /// <summary> [역할] 배열 안전 접근(인덱스 범위 보정). </summary>
    private int GetSafe(int[] arr, int idx)
    {
        if (arr == null || idx < 0 || idx >= arr.Length) return 0;
        return Mathf.Max(0, arr[idx]);
    }
}
