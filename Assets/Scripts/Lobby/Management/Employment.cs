using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using System;
using System.Collections;

public class Employment : ListUIBase<Job>
{
    [Header("Refs")]
    [SerializeField] private ListUpManager listUpManager;
    [SerializeField] private HeroListUp heroListUp;

    [Header("Set Prefab")]
    [SerializeField] private GameObject heroPricePrefab;
    [SerializeField] private Transform gridPricePanel;

    [Header("Extra Panel")]
    [SerializeField] private GameObject pricePanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text employText;

    [Header("Extra Assets")]
    [SerializeField] private GoodsImage currencyImage;
    [SerializeField] private InventoryRuntime wallet;
    [SerializeField] private TestHero testHero; // 실제 소유 목록(로비)
    [SerializeField] private TestHero DBHero;   // 풀/DB 목록

    private readonly List<Job> randomHeros = new();
    private readonly HashSet<int> hiredIds = new();               // 이미 고용한 영웅 id_job
    private readonly Dictionary<int, List<Button>> heroButtons = new(); // 같은 영웅 id를 가리키는 버튼들(비활성 처리용)

    private Job selectedHero;
    [SerializeField] private int heroPrice = 3;

    private bool _hasRolledThisSession = false; // 로비 세션 내 1회만 랜덤
    private UIClickResetHandler _clickReset;    // 등록/해제 관리

    private InventoryRuntime Wallet
    {
        get
        {
            if (!wallet) wallet = InventoryRuntime.Instance; // 자동 보정
            return wallet;
        }
    }


    // ===== Lifecycle =====
    protected override void OnEnable()
    {
        base.OnEnable();

        // Confirm
        if (confirmButton) confirmButton.onClick.AddListener(EmployHero);
        if (employText) employText.text = "고용";

        // 외부 클릭 리셋
        _clickReset = FindObjectOfType<UIClickResetHandler>();
        if (_clickReset != null) _clickReset.RegisterResetCallback(ResetEmployUI);

        // 재화 변화에 반응
        if (wallet) wallet.OnCurrencyChanged += RefreshAffordability;

        // 최초 1회 랜덤 추출, 그 외는 캐시만 그리기
        if (!_hasRolledThisSession)
        {
            RerollCandidates();
            _hasRolledThisSession = true;
        }
        else
        {
            RebuildListUIFromCache();
        }

        RefreshAffordability();
    }

    private void OnDisable()
    {
        if (confirmButton) confirmButton.onClick.RemoveListener(EmployHero);
        if (_clickReset != null) _clickReset.UnregisterResetCallback(ResetEmployUI);
        if (Wallet) Wallet.OnCurrencyChanged -= RefreshAffordability;
    }

    // ===== 데이터/화면 구성 =====
    private void RerollCandidates()
    {
        randomHeros.Clear();
        randomHeros.AddRange(PickRandomDistinct(DBHero.jobs, 2)); // 중복 없이 2명
        RebuildListUIFromCache();
    }

    private void RebuildListUIFromCache()
    {
        // 리스트/가격 UI 모두 초기화
        ClearList();
        heroButtons.Clear();

        if (gridPricePanel)
        {
            for (int i = gridPricePanel.childCount - 1; i >= 0; i--)
                Destroy(gridPricePanel.GetChild(i).gameObject);
        }

        // 후보 출력
        foreach (var hero in randomHeros)
        {
            // 가격 패널 한 줄 생성
            var p = Instantiate(heroPricePrefab, gridPricePanel);
            var image = p.transform.Find("image")?.GetComponent<Image>();
            var priceText = p.transform.Find("price")?.GetComponent<TMP_Text>();

            if (image)
            {
                switch (hero.jobCategory)
                {
                    case JobCategory.Warrior: image.sprite = currencyImage.warriorImage; break;
                    case JobCategory.Ranged:  image.sprite = currencyImage.rangeImage;   break;
                    case JobCategory.Special: image.sprite = currencyImage.specialImage; break;
                    case JobCategory.Healer:  image.sprite = currencyImage.healerImage;  break;
                }
            }
            if (priceText)
            {
                priceText.text = heroPrice.ToString();
                bool ok = Wallet && Wallet.GetSoul(hero.jobCategory) >= heroPrice;
                priceText.color = ok ? Color.white : Color.red;
            }

            // 리스트 버튼 생성(베이스 유틸 사용)
            CreateButton(hero); // SetLabel/OnSelected가 연결됨

            // 생성된 마지막 버튼 참조
            var btn = contentParent.GetChild(contentParent.childCount - 1).GetComponent<Button>();
            if (!btn) continue;

            btn.name = $"Hire_{hero.id_job}";

            // 동일 영웅 id 버튼 모음 등록(고용 후 비활성화)
            if (!heroButtons.TryGetValue(hero.id_job, out var list))
            {
                list = new List<Button>();
                heroButtons[hero.id_job] = list;
            }
            list.Add(btn);

            // 이미 고용된 영웅이면 바로 비활성화
            if (hiredIds.Contains(hero.id_job)) btn.interactable = false;
        }
    }

    // ===== 상속 훅 =====
    protected override void LoadList()
    {
        // 베이스에서 호출할 가능성 대비: 캐시 기반으로만 재구축
        RebuildListUIFromCache();
    }

    [Preserve]
    protected override void SetLabel(Button button, Job hero)
    {
        // 버튼 프리팹 내부 라벨 바인딩
        var heroImage = button.transform.Find("HeroImage")?.GetComponent<Image>();
        var nameText  = button.transform.Find("Text_Name")?.GetComponent<TMP_Text>();
        var jobText   = button.transform.Find("Text_Job")?.GetComponent<TMP_Text>();
        var levelText = button.transform.Find("Text_Level")?.GetComponent<TMP_Text>();

        if (heroImage) heroImage.sprite = hero.portrait;
        if (nameText)  nameText.text    = hero.name_job;
        if (jobText)   jobText.text     = hero.jobCategory.ToString();
        if (levelText) levelText.text   = $"Lv.{hero.level}";
    }

    [Preserve]
    protected override void OnSelected(Job hero)
    {
        selectedHero = hero;

        if (pricePanel) pricePanel.SetActive(true);
        if (confirmButton) confirmButton.gameObject.SetActive(true);

        // 이미 고용한 영웅이면 버튼 비활성/문구 고정
        if (hiredIds.Contains(hero.id_job))
        {
            confirmButton.interactable = false;
            if (employText) employText.text = "고용 완료";
        }
        else
        {
            bool ok = Wallet && Wallet.GetSoul(hero.jobCategory) >= heroPrice;
            confirmButton.interactable = ok;
            if (employText) employText.text = "고용";
        }

        listUpManager?.EmployPanelState(true);
        ShowHeroInfo(hero); // 베이스 제공: 좌측 정보 패널
    }

    // ===== 재화 비교/지불/갱신 =====
    private void EmployHero()
    {
        if (selectedHero == null || Wallet == null) return;
        if (hiredIds.Contains(selectedHero.id_job)) return; // 중복 방지

        // 지불(단일 경로)
        if (!Wallet.TrySpendSoul(selectedHero.jobCategory, heroPrice))
        {
            // TODO: 부족 알림(사운드/토스트)
            return;
        }

        // 실제 고용: DB에서 사본 생성 후 보유 목록에 추가
        var hired = CopyForHire(selectedHero);
        testHero.jobs.Add(hired);
        hiredIds.Add(selectedHero.id_job);

        // 동일 영웅 id의 버튼들 비활성화
        if (heroButtons.TryGetValue(selectedHero.id_job, out var btns))
            foreach (var b in btns) if (b) b.interactable = false;

        if (confirmButton) confirmButton.interactable = false;
        if (employText)    employText.text = "고용 완료";

        // 외부 UI 갱신
        heroListUp?.RefreshHeroList();
        listUpManager?.RefreshList();

        // 진행 저장(있다면)
        var svc = PlayerProgressService.Instance;
        if (svc) _ = svc.SaveAsync();

        StartCoroutine(ResetAfterHire());
    }

    private IEnumerator ResetAfterHire()
    {
        yield return null; // 한 프레임 뒤(UI 반영 대기)

        // 선택 표시 초기화(베이스 유틸)
        ResetButtonImage();

        if (pricePanel) pricePanel.SetActive(false);
        if (confirmButton) confirmButton.gameObject.SetActive(false);
        if (employText) employText.text = "고용";
        listUpManager?.EmployPanelState(false);
        selectedHero = null;
    }

    public void ResetEmployUI()
    {
        // 선택 버튼 이미지는 베이스에서 처리 → 여기선 패널만 끔
        if (pricePanel) pricePanel.SetActive(false);
        if (confirmButton) confirmButton.gameObject.SetActive(false);
        if (employText) employText.text = "고용";
        listUpManager?.EmployPanelState(false);
    }

    // ===== 내부 유틸 =====
    private static List<Job> PickRandomDistinct(List<Job> src, int count)
    {
        var pool = new List<Job>(src);
        var result = new List<Job>();
        count = Mathf.Clamp(count, 0, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int r = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[r]);
            pool.RemoveAt(r);
        }
        return result;
    }

    private Job CopyForHire(Job src)
    {
        var copy = new Job
        {
            id_job = src.id_job,
            displayName = src.displayName ?? src.name_job,
            name_job = src.name_job,
            portrait = src.portrait,

            level = (src.level <= 0 ? 1 : src.level),   // 최소 1 보장
            exp   = src.exp,
            maxHp = src.maxHp,
            hp    = (src.hp > 0 ? src.hp : src.maxHp),
            def   = src.def,
            res   = src.res,
            spd   = src.spd,
            hit   = src.hit,

            loc         = src.loc,
            category    = src.category,
            jobCategory = src.jobCategory,
        };

        copy.instanceId = System.Guid.NewGuid().ToString("N");
        copy.NormalizeLevelExp();
        return copy;
    }

    /// <summary>
    /// 보유 소울 기준으로 가격 색상과 확인 버튼을 갱신.
    /// 버튼 참조/헬퍼 없이 randomHeros 인덱스로만 처리.
    /// </summary>
    private void RefreshAffordability()
    {
        // 가격 색상
        if (gridPricePanel)
        {
            // randomHeros를 만든 순서 == UI 생성 순서
            int n = Mathf.Min(gridPricePanel.childCount, randomHeros.Count);
            for (int i = 0; i < n; i++)
            {
                var hero = randomHeros[i];
                var priceText = gridPricePanel.GetChild(i).Find("price")?.GetComponent<TMP_Text>();
                if (!priceText) continue;
                bool ok = Wallet && Wallet.GetSoul(hero.jobCategory) >= heroPrice;
                priceText.color = ok ? Color.white : Color.red;
            }
        }

        // 선택된 영웅의 확인 버튼
        if (confirmButton && selectedHero != null)
        {
            bool ok = Wallet
                      && Wallet.GetSoul(selectedHero.jobCategory) >= heroPrice
                      && !hiredIds.Contains(selectedHero.id_job);
            confirmButton.interactable = ok;
        }
    }

    public void ResetButtonImage()
    {
        base.ResetSelectedButton();
        selectedHero = null;
    }
}