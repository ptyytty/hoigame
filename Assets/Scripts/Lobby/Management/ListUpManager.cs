using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListUpManager : ListUIBase<Job>
{
    [Header("Main Tab State")]
    [SerializeField] Toggle employToggle;
    [SerializeField] Toggle recoveryToggle;

    [Header("List Toggle")]
    [SerializeField] Toggle toggleSortByName;
    [SerializeField] Toggle toggleSortByJob;
    [SerializeField] Toggle toggleSortByLevel;

    [Header("Employ Object")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject employBtn;
    [SerializeField] private GameObject employPrice;

    [Header("Rename (Employ tab only)")]
    [SerializeField] private GameObject renamePanel;
    [SerializeField] private TMP_InputField renameInput;
    [SerializeField] private Button renameConfirm;
    [SerializeField] private Button renameCancel;

    [Header("Recovery")]
    [SerializeField] private Recovery recoveryMain;
    [SerializeField] private GameObject recoverApply;

    [Header("Created Asset")]
    [SerializeField] private TestHero testHero;

    public enum SortType { Name, Job, Level }
    private SortType currentSortType = SortType.Name;

    public Job CurrentSelectedHero { get; private set; }     // 외부에서 선택 영웅 조회

    private bool _isBuilding;
    private bool _renameBusy;
    private bool _sortListenersWired;

    void Start()
    {
        StartCoroutine(DeferredLoadList());  // 한 프레임 지연
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (renameConfirm)
        {
            renameConfirm.onClick.RemoveAllListeners();
            renameConfirm.onClick.AddListener(ConfirmRename);
        }
        if (renameCancel)
        {
            renameCancel.onClick.RemoveAllListeners();
            renameCancel.onClick.AddListener(CloseRenamePanel);
        }


        WireSortToggleListenersOnce();      // 토글 바인딩
        SyncSortTypeWithToggles();          // 토글 기준 정렬

        var click = FindObjectOfType<UIClickResetHandler>();
        if (click != null) click.RegisterResetCallback(ResetHeroListState);
    }

    private IEnumerator DeferredLoadList()
    {
        // PlayerProgressService가 런타임 적용을 끝낼 때까지 살짝 대기
        yield return null; // 1프레임
        RefreshList();
    }

    protected override void LoadList()
    {
        if (!ValidateListBinding("ListUpManager")) return;
        if (!testHero) { Debug.LogError("[ListUp] testHero is NULL"); return; }
        if (testHero.jobs == null) { Debug.LogError("[ListUp] testHero.jobs is NULL"); return; }

        // null 엔트리 제거
        var baseList = new List<Job>(testHero.jobs);
        baseList.RemoveAll(j => j == null);

        // 정렬
        switch (currentSortType)
        {
            case SortType.Name:
                baseList.Sort((a, b) => string.Compare(a?.displayName ?? "", b?.displayName ?? "", StringComparison.Ordinal));
                break;
            case SortType.Job:
                baseList.Sort((a, b) => a.name_job.CompareTo(b.name_job));
                break;
            case SortType.Level:
                baseList.Sort((a, b) => a.level.CompareTo(b.level));
                break;
        }

        foreach (var hero in baseList)
            SafeCreateButton(hero, "ListUpManager");
    }

    // ============= 리스트 정렬 ================
    private void WireSortToggleListenersOnce()
    {
        if (_sortListenersWired) return;

        if (toggleSortByName)
            toggleSortByName.onValueChanged.AddListener(isOn => { if (isOn) SetSortType(SortType.Name); });
        if (toggleSortByJob)
            toggleSortByJob.onValueChanged.AddListener(isOn => { if (isOn) SetSortType(SortType.Job); });
        if (toggleSortByLevel)
            toggleSortByLevel.onValueChanged.AddListener(isOn => { if (isOn) SetSortType(SortType.Level); });

        _sortListenersWired = true;
    }

    private void SyncSortTypeWithToggles()
    {
        if (toggleSortByJob && toggleSortByJob.isOn) currentSortType = SortType.Job;
        else if (toggleSortByLevel && toggleSortByLevel.isOn) currentSortType = SortType.Level;
        else currentSortType = SortType.Name; // 기본값

        RefreshList();
    }

    public void SetSortType(SortType sortType)
    {
        if (currentSortType == sortType) return; // 동일 기준이면 스킵 (모바일: 불필요 갱신 회피)
        currentSortType = sortType;
        RefreshList();
    }

    protected override void OnSelected(Job hero)
    {
        CurrentSelectedHero = hero;

        // 고용 탭: 리네임 유지
        if (employToggle && employToggle.isOn) OpenRenamePanel(hero);
        else CloseRenamePanel();

        // ⬇️ 의무실 탭: 슬롯에 "즉시 추가"하지 않고, '대기 영웅'만 등록
        if (recoveryToggle && recoveryToggle.isOn)
        {
            if (!recoveryMain)
            {
                Debug.LogWarning("[ListUpManager] Recovery(ref) is not assigned.");
                return;
            }
            recoveryMain.SetPendingHero(hero); // ✅ 리스트 클릭 → 대기만
        }
    }

    public void ResetButtonImage()
    {
        base.ResetSelectedButton();
        CurrentSelectedHero = null;
    }

    // ========= 고용 UI ========
    public void EmployPanelState(bool state)
    {
        infoPanel.SetActive(state);
        employBtn.SetActive(state);
        PricePanelState(state);

        // 고용 패널이 닫히면 리네임 패널도 닫음
        if (!state) CloseRenamePanel();
    }

    public void PricePanelState(bool state)
    {
        employPrice.SetActive(state);
    }

    // ======== 의무실 UI =======
    public void RecoveryPanelState(bool state)
    {
        if (state && recoveryMain != null)
        {
            recoveryMain.SetPendingHealAmount(20); // 회복량 20 부여
        }
        ApplyPanelState(state);
    }
    public void ApplyPanelState(bool state)
    {
        recoverApply.SetActive(state);
    }

    protected override void SetLabel(Button button, Job hero)
    {
        var nameT = SafeFind(button.transform, "Text_Name", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();
        var jobT = SafeFind(button.transform, "Text_Job", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();
        var levelT = SafeFind(button.transform, "Text_Level", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();

        if (nameT) nameT.text = hero?.displayName ?? hero.name_job;
        if (jobT) jobT.text = hero != null ? hero.name_job.ToString() : "-";
        if (levelT) levelT.text = hero != null ? $"Lv.{hero.level}" : "-";
    }

    public void RefreshList()
    {
        if (_isBuilding) return;   // 재진입 방지
        _isBuilding = true;
        try
        {
            ClearList();
            LoadList();
        }
        finally { _isBuilding = false; }
    }

    private void SetPanelInteractable(GameObject panel, bool on)
    {
        if (!panel) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (!cg) cg = panel.AddComponent<CanvasGroup>();

        panel.SetActive(on);
        cg.interactable = on;
        cg.blocksRaycasts = on;
        cg.ignoreParentGroups = false;
    }


    // ========== Rename =========
    private void OpenRenamePanel(Job hero)
    {
        if (!renamePanel || !renameInput) return;

        SetPanelInteractable(renamePanel, true);
        renameInput.text = hero?.name_job ?? string.Empty;

        // 포커스만 주고, 다른 버튼 클릭은 막지 않도록 CanvasGroup로 관리
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(renameInput.gameObject);
    }

    private void CloseRenamePanel()
    {
        SetPanelInteractable(renamePanel, false);
    }


    private void ConfirmRename()
    {
        if (_renameBusy) return;
        _renameBusy = true;
        try
        {
            if (CurrentSelectedHero == null)
            {
                Debug.LogWarning("[Rename] No selected hero.");
                CloseRenamePanel();
                return;
            }

            string newName = (renameInput?.text ?? "").Trim();
            if (string.IsNullOrEmpty(newName))
            {
                // 빈 이름 → 그냥 닫기
                CloseRenamePanel();
                return;
            }

            // 데이터 반영
            CurrentSelectedHero.displayName = newName;

            // 선택 버튼 라벨 업데이트
            if (currentSelect)   // ← ListUIBase가 가진 현재 선택 버튼 참조
            {
                var nameT = currentSelect.transform.Find("Text_Name")?.GetComponent<TMPro.TMP_Text>();
                if (nameT) nameT.text = newName;
            }
            else
            {
                // 선택 버튼이 없다면, 다음 프레임에 안전하게 전체 갱신
                StartCoroutine(RefreshNextFrame());
            }

            // 3) 저장(있으면)
            if (PlayerProgressService.Instance != null)
                _ = PlayerProgressService.Instance.SaveAsync();
        }
        finally
        {
            // 리스트를 먼저 밀지 말고, 무조건 패널을 확실히 닫는다
            CloseRenamePanel();
            _renameBusy = false;
        }
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshList();   // ← 이때는 패널이 이미 닫혀 있어서 재진입/포커스 문제 없음
    }

    public void ResetHeroListState()
    {
        EmployPanelState(false);
        PricePanelState(false);
        ResetButtonImage();          // 선택 버튼 스프라이트 원복(중복 호출 안전)
        CurrentSelectedHero = null;  // 선택 데이터도 정리
    }

    // ============ Recover ==============
    public void ClearExternalSelection()  // ✅ 외부(Recovery)에서 호출: 리스트 선택/이미지 초기화
    {
        ResetButtonImage();   // 하이라이트/이미지 원복
        CurrentSelectedHero = null; // 선택 데이터도 함께 비움
    }
}
