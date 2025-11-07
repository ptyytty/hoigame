using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class HeroListUp : ListUIBase<Job>
{
    [Header("Interact Panels")]
    [SerializeField] private PartySelector partySelector;
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject heroListPanel;

    [Header("Prefab")]
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Created Assets")]
    [SerializeField] private TestHero testHero;

    // delegate 정의 (형식 선언)
    public delegate void HeroSelectedHandler(Job selectedHero);
    // event 선언
    public event HeroSelectedHandler OnHeroSelected;

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshHeroList();

        // 의무실 잠금 변경 시 리스트 즉시 반영
        Recovery.OnLocksChanged -= RefreshHeroList;   // 중복 방지
        Recovery.OnLocksChanged += RefreshHeroList;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Recovery.OnLocksChanged -= RefreshHeroList;
    }

    void ApplyUniformSpriteAll()
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);
        for (int i = 0; i < n; i++)
        {
            var img = buttons[i].GetComponent<Image>();
            img.sprite = globalDefaultSprite;
        }
    }

    void ApplyUniformSpriteExceptCurrent()
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);
        for (int i = 0; i < n; i++)
        {
            var img = buttons[i].GetComponent<Image>();
            img.sprite = (buttons[i] == currentSelect) ? globalSelectedSprite : globalDefaultSprite;
        }
    }

    protected override void LoadList()
    {
        // 전열 → 후열 → 기타 순 정렬
        // 같은 그룹 내에서는 name 기준 2차 정렬
        var ordered = testHero.jobs
        .OrderBy(h => (Loc)h.loc == Loc.Front ? 0 : ((Loc)h.loc == Loc.Back ? 1 : 2))
        .ThenBy(h => h.name_job);  // 필요시 .ThenBy(h => h.level) 등으로 변경

        foreach (var hero in ordered)
            CreateButton(hero);

        currentSelect = null;

        // 생성 직후 전/후열 스프라이트로 덮어쓰기
        ApplyUniformSpriteAll();

        // 현재 파티 상태로 '리스트 버튼 잠금' 재적용(파티에 들어간 인스턴스는 비활성)
        if (partySelector != null)
            SetButtonsForParty(partySelector.GetInPartyInstanceSet());

        ApplyRecoveryLocksToButtons();
    }

    // 영웅 위치에 따른 버튼 비활성화
    public void SetHeroButtonInteractableByLoc(int requiredLoc, HashSet<string> inPartyHeroId)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var job = dataList[i];
            bool canUseLoc = job.loc == requiredLoc || job.loc == (int)Loc.None;
            bool inParty = !string.IsNullOrEmpty(job.instanceId) && inPartyHeroId.Contains(job.instanceId);

            bool interactable = canUseLoc && !inParty && !IsRecoveryLocked(job);
            if (buttons[i]) buttons[i].interactable = interactable;
        }

        ApplyRecoveryLocksToButtons();
    }

    // 기존 시그니처도 남겨서 호환(필요 시 내부에서 빈 Set로 위 메서드 호출)
    public void SetHeroButtonInteractableByLoc(int requiredLoc,
                                           HashSet<string> inPartyInstanceIds,
                                           Job[] assignedHeroes)
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);
        for (int i = 0; i < n; i++)
        {
            var job = dataList[i];
            bool canUseLoc = job.loc == requiredLoc || job.loc == (int)Loc.None;
            bool inParty = IsInParty(job, inPartyInstanceIds, assignedHeroes);

            bool interactable = canUseLoc && !inParty && !IsRecoveryLocked(job);
            if (buttons[i]) buttons[i].interactable = interactable;
        }

        ApplyRecoveryLocksToButtons();
    }

    public void SetButtonsForParty(HashSet<string> inPartyheroId)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var job = dataList[i];
            bool inParty = !string.IsNullOrEmpty(job.instanceId) && inPartyheroId.Contains(job.instanceId);
            bool interactable = !inParty && !IsRecoveryLocked(job); // 🔒 잠금 포함
            if (buttons[i]) buttons[i].interactable = interactable;
        }
        ApplyRecoveryLocksToButtons();
    }

    public void SetButtonsForParty(HashSet<string> inPartyInstanceIds,
                                   Job[] assignedHeroes)
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);
        for (int i = 0; i < n; i++)
        {
            var job = dataList[i];
            bool inParty = IsInParty(job, inPartyInstanceIds, assignedHeroes);
            bool interactable = !inParty && !IsRecoveryLocked(job); // 🔒 잠금 포함
            if (buttons[i]) buttons[i].interactable = interactable;
        }
        ApplyRecoveryLocksToButtons();
    }

    // 배치된 hero 버튼 비활성화
    public void DisableButtonFor(Job hero)
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);

        // instanceId 우선
        if (!string.IsNullOrEmpty(hero.instanceId))
        {
            for (int i = 0; i < n; i++)
                if (dataList[i] != null && dataList[i].instanceId == hero.instanceId)
                { if (buttons[i]) buttons[i].interactable = !IsRecoveryLocked(hero); return; }
        }
        // 참조 동일성 fallback
        for (int i = 0; i < n; i++)
            if (object.ReferenceEquals(dataList[i], hero))
            { if (buttons[i]) buttons[i].interactable = !IsRecoveryLocked(hero); return; }
    }

    public void ResetHeroListState()
    {
        ResetSelectedButton(); // 선택된 버튼 이미지 복구
        SetAllButtonsInteractable(true); // 버튼 상호작용 복구

        if (partySelector != null) SetButtonsForParty(partySelector.GetInPartyInstanceSet());

        ApplyUniformSpriteAll();
        ApplyRecoveryLocksToButtons();
    }

    public void RefreshHeroList()
    {
        ClearList();
        LoadList();
        ApplyUniformSpriteAll();
        ApplyRecoveryLocksToButtons();
    }

    protected override void SetLabel(Button button, Job hero)
    {
        Image heroImage = button.transform.Find("HeroImage").GetComponent<Image>();
        TMP_Text nameText = button.transform.Find("Text_Name").GetComponent<TMP_Text>();
        TMP_Text jobText = button.transform.Find("Text_Job").GetComponent<TMP_Text>();
        TMP_Text levelText = button.transform.Find("Text_Level").GetComponent<TMP_Text>();

        heroImage.sprite = hero.portrait;
        nameText.text = hero.displayName;
        jobText.text = hero.name_job.ToString();
        levelText.text = $"Lv.{hero.level}";
    }

    protected override void OnSelected(Job hero)
    {
        Debug.Log("선택됨!");
        OnHeroSelected?.Invoke(hero);

        ShowHeroInfo(hero);

        ApplyUniformSpriteExceptCurrent();
        SelectionEvents.RaiseHeroSelected(hero);
    }

    // 이하 PartySelector 호출용 Public 메소드
    public void ResetButton()
    {
        ResetSelectedButton();
        ApplyUniformSpriteAll();
    }

    public void SetInteractable(bool state)
    {
        SetAllButtonsInteractable(state);
    }

    // 유틸 확인
    bool IsInParty(Job job, HashSet<string> inPartyInstanceIds, Job[] assignedHeroes)
    {
        // 의무실 잠금(회복 확정 후, 던전 다녀오기 전까지)
        if (!string.IsNullOrEmpty(job.instanceId) && Recovery.LockedInstanceIds.Contains(job.instanceId))
            return true;

        // instanceId 최우선
        if (!string.IsNullOrEmpty(job.instanceId) && inPartyInstanceIds != null)
            if (inPartyInstanceIds.Contains(job.instanceId)) return true;

        // 참조 동일성
        if (assignedHeroes != null)
            for (int i = 0; i < assignedHeroes.Length; i++)
                if (object.ReferenceEquals(assignedHeroes[i], job)) return true;

        return false;
    }

    // 의무실 사용 영웅 버튼 비활성화
    void ApplyRecoveryLocksToButtons()
    {
        int n = Mathf.Min(buttons.Count, dataList.Count);
        for (int i = 0; i < n; i++)
        {
            var job = dataList[i];
            if (IsRecoveryLocked(job) && buttons[i])
                buttons[i].interactable = false; // 출력은 유지, 선택만 금지
        }
    }

    // 의무실 잠금 영웅 조회
    bool IsRecoveryLocked(Job job)
    {
        return !string.IsNullOrEmpty(job.instanceId)
            && Recovery.LockedInstanceIds.Contains(job.instanceId);
    }
}
