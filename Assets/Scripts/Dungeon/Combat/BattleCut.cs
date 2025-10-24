using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// [역할] 전투 연출(컷)의 온/오프를 총괄.
/// - 컷 시작 시: 입력/스킬 UI/하이라이트/아웃라인 일시 비활성화
/// - 컷 종료 시: 원상복구
/// </summary>

public class BattleCut : MonoBehaviour
{
    public static BattleCut Instance { get; private set; }
    void Awake() => Instance = this;

    [Header("연출 토글 대상")]
    [SerializeField] private UIManager uiManager;          // 스킬 패널 등 숨김
    [SerializeField] private BattleInput battleInput;      // 탭 입력 차단
    [SerializeField] private bool disableOutlines = true;  // 아웃라인 일시 비활성화
    [SerializeField] private float uiFade = 0.1f;   // 페이드 속도

    private int _cutNest = 0;      // 중첩 보호(코루틴 중복 대비)

    /// <summary>
    /// [역할] 컷 시작(즉시 적용)
    /// </summary>
    public void BeginCut()
    {
        _cutNest++;
        if (_cutNest > 1) return;

        if (battleInput) battleInput.enabled = false;
        SkillTargetHighlighter.Instance?.ClearAll();

        // ★ 전체 캔버스 숨김 (신규)
        if (uiManager) uiManager.SetCanvasVisible(false, uiFade);

        if (disableOutlines)
        {
            foreach (var od in FindObjectsOfType<OutlineDuplicator>(true))
                od.EnableOutline(false);
        }

    }

    /// <summary>
    /// [역할] 컷 종료(즉시 복구)
    /// </summary>
    public void EndCut()
    {
        _cutNest = Mathf.Max(0, _cutNest - 1);
        if (_cutNest > 0) return; // 중첩 해제 남아있으면 아직 종료하지 않음

        // 1) 입력 복구
        if (battleInput) battleInput.enabled = true;

        if (uiManager) uiManager.SetCanvasVisible(true, uiFade);
    }

    /// <summary>
    /// [역할] 지정 시간만큼 컷 유지(코루틴)
    /// </summary>
    public IEnumerator CutForSeconds(float seconds)
    {
        BeginCut();
        yield return new WaitForSeconds(seconds);
        EndCut();
    }
}
