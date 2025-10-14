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
    [SerializeField] Toggle growthToggle;
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

    [Header("Growth Object")]
    [SerializeField] private GameObject growthImage;

    [Header("Created Asset")]
    [SerializeField] private TestHero testHero;

    private enum SortType { Name, Job, Level }
    private SortType currentSortType = SortType.Name;

    public event Action<Job> OnOwnedHeroSelected;           // Growth 구독 이벤트
    public Job CurrentSelectedHero { get; private set; }     // 외부에서 선택 영웅 조회

    private bool _isBuilding;
    private bool _renameBusy;

    void Start()
    {
        StartCoroutine(DeferredLoadList());  // ← 한 프레임 지연
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

        // null-safe 정렬
        switch (currentSortType)
        {
            case SortType.Name:
                baseList.Sort((a, b) => string.Compare(a?.name_job ?? "", b?.name_job ?? "", StringComparison.Ordinal));
                break;
            case SortType.Job:
                baseList.Sort((a, b) => a.jobCategory.CompareTo(b.jobCategory));
                break;
            case SortType.Level:
                baseList.Sort((a, b) => a.id_job.CompareTo(b.id_job));
                break;
        }

        foreach (var hero in baseList)
            SafeCreateButton(hero, "ListUpManager");
    }

    private void ChangeSortType(SortType sortType)
    {
        currentSortType = sortType;
        RefreshList();
    }

    protected override void OnSelected(Job hero)
    {
        CurrentSelectedHero = hero;

        // ‘고용’ 탭에서만 리네임 허용
        if (employToggle && employToggle.isOn) OpenRenamePanel(hero);
        else CloseRenamePanel();

        if (growthToggle && growthToggle.isOn)
            OnOwnedHeroSelected?.Invoke(hero);
    }

    public void ResetButtonImage()
    {
        base.ResetSelectedButton();
        CurrentSelectedHero = null;
    }

    public void EmployPanelState(bool state)
    {
        infoPanel.SetActive(state);
        employBtn.SetActive(state);

        // 고용 패널이 닫히면 리네임 패널도 닫음
        if (!state) CloseRenamePanel();
    }

    public void PricePanelState(bool state)
    {
        employPrice.SetActive(state);
    }

    public void GrowthPanelState(bool state)
    {
        growthImage.SetActive(state);
    }

    public void RecoveryPanelState(bool state)
    {

    }

    protected override void SetLabel(Button button, Job hero)
    {
        var nameT = SafeFind(button.transform, "Text_Name", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();
        var jobT = SafeFind(button.transform, "Text_Job", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();
        var levelT = SafeFind(button.transform, "Text_Level", "ListUpManager")?.GetComponent<TMPro.TMP_Text>();

        if (nameT) nameT.text = hero?.name_job ?? "(null)";
        if (jobT) jobT.text = hero != null ? hero.jobCategory.ToString() : "-";
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

            // 1) 데이터에 즉시 반영
            CurrentSelectedHero.name_job = newName;

            // 2) UI에 즉시 반영(리스트 전체 리프레시 없이 선택 버튼 라벨만 업데이트)
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

    private System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshList();   // ← 이때는 패널이 이미 닫혀 있어서 재진입/포커스 문제 없음
    }

    public void ResetHeroListState()
    {
        EmployPanelState(false);
        PricePanelState(false);
        GrowthPanelState(false);
        ResetButtonImage();          // 선택 버튼 스프라이트 원복(중복 호출 안전)
        CurrentSelectedHero = null;  // 선택 데이터도 정리
    }
}
