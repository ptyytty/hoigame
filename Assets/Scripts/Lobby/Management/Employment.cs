using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using System;

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
    [SerializeField] private TestMoney testMoney;       // 임시 데이터
    [SerializeField] private TestHero testHero;
    [SerializeField] private TestHero DBHero;

    private List<Job> randomHeros = new();
    private Job selectedHero;
    private int heroPrice = 3;

    private readonly HashSet<int> hiredIds = new();     // 고용 영웅 아이디 저장
    private readonly Dictionary<int, List<Button>> heroButtons = new();     // 영웅 아이디와 영웅 버튼 매핑

    private bool _hasRolledThisSession = false;   // ← 로비 세션 내 1회만 랜덤

    protected override void OnEnable()
    {
        base.OnEnable();

        confirmButton.onClick.AddListener(EmployHero);
        employText.text = "고용";

        // ✅ 최초 1회만 랜덤 추출, 그 뒤로는 캐시로만 UI 재구성
        if (!_hasRolledThisSession)
        {
            RerollCandidates();                   // 내부에서 randomHeros 갱신 + UI 재구성
            _hasRolledThisSession = true;
        }
        else
        {
            RebuildListUIFromCache();             // 캐시만으로 화면만 다시 그림 (랜덤 유지)
        }
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveListener(EmployHero);
    }

    // ====== 영웅 랜덤 추출 수행 ======
    private void RerollCandidates()
    {
        randomHeros.Clear();
        randomHeros.AddRange(PickRandomDistinct(DBHero.jobs, 2));  // 중복 없이 영웅 2 선정
        RebuildListUIFromCache();
    }

    // ====== 화면 다시 출력 ======
    private void RebuildListUIFromCache()
    {
        // 리스트/가격패널 UI 정리
        ClearList();
        heroButtons.Clear(); // ← 버튼 맵도 싹 비움

        if (gridPricePanel)
        {
            for (int i = gridPricePanel.childCount - 1; i >= 0; i--)
                Destroy(gridPricePanel.GetChild(i).gameObject);
        }

        // 캐시된 후보로만 출력
        foreach (var hero in randomHeros)
        {
            var p = Instantiate(heroPricePrefab, gridPricePanel);
            var image = p.transform.Find("image")?.GetComponent<Image>();
            var price = p.transform.Find("price")?.GetComponent<TMP_Text>();

            if (image)
            {
                switch (hero.jobCategory)
                {
                    case JobCategory.Warrior: image.sprite = currencyImage.warriorImage; break;
                    case JobCategory.Ranged: image.sprite = currencyImage.rangeImage; break;
                    case JobCategory.Special: image.sprite = currencyImage.specialImage; break;
                    case JobCategory.Healer: image.sprite = currencyImage.healerImage; break;
                }
            }
            if (price)
            {
                price.text = "3";
                price.color = testMoney.HasEnoughSoul(hero.jobCategory, 3) ? Color.white : Color.red;
            }

            CreateButton(hero); // SetLabel/OnSelected 연결

            var btn = contentParent.GetChild(contentParent.childCount - 1).GetComponent<Button>();
            if (btn)
            {
                btn.name = $"Hire_{hero.id_job}";

                // 맵 등록
                if (!heroButtons.TryGetValue(hero.id_job, out var list))
                {
                    list = new List<Button>();
                    heroButtons[hero.id_job] = list;
                }
                list.Add(btn);

                // 이미 고용한 영웅 버튼 비활성화
                if (hiredIds.Contains(hero.id_job))
                    btn.interactable = false;
            }
        }
    }

    // 씬 복귀 시 외부에서 호출하면 ‘이번 세션의 랜덤’을 갱신
    public void ForceReroll()
    {
        _hasRolledThisSession = false;
        RerollCandidates();
        _hasRolledThisSession = true;
    }



    // ===== 상속 훅 구현 =====

    // LoadList()는 랜덤 뽑기 X
    protected override void LoadList()
    {
        // 베이스가 호출할 수도 있으니 안전하게 캐시 재그리기만 수행
        RebuildListUIFromCache();
    }

    [Preserve]
    protected override void SetLabel(Button button, Job hero)
    {
        // 버튼 프리팹 내부 텍스트 바인딩
        var nameText = button.transform.Find("Text_Name")?.GetComponent<TMP_Text>();
        var jobText = button.transform.Find("Text_Job")?.GetComponent<TMP_Text>();
        var levelText = button.transform.Find("Text_Level")?.GetComponent<TMP_Text>();

        if (nameText) nameText.text = hero.name_job;
        if (jobText) jobText.text = hero.jobCategory.ToString(); // ⬅ 기존 코드에선 name_job을 ToString() 하던 버그 수정  :contentReference[oaicite:6]{index=6}
        if (levelText) levelText.text = $"Lv.{hero.level}";
    }

    [Preserve]
    protected override void OnSelected(Job hero)
    {
        // 이미 고용한 영웅이면 confirm을 영구 비활성(또는 숨김)
        if (hiredIds.Contains(hero.id_job))
        {
            selectedHero = hero;
            if (pricePanel) pricePanel.SetActive(true);

            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
            employText.text = "고용 완료";

            listUpManager?.EmployPanelState(true);
            ShowHeroInfo(hero);
            return;
        }

        // 아직 미고용인 경우 기존 로직
        selectedHero = hero;
        if (pricePanel) pricePanel.SetActive(true);

        confirmButton.gameObject.SetActive(true);
        confirmButton.interactable = testMoney.HasEnoughSoul(hero.jobCategory, heroPrice);
        employText.text = "고용";

        listUpManager?.EmployPanelState(true);
        ShowHeroInfo(hero);
    }

    // ===== 내부 유틸 =====

    private void RebuildListUI()
    {
        LoadList();                                // 상속된 훅을 표준 경로로 호출
        // 필요 시 추가 UI 상태 초기화 가능
    }

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

    private void EmployHero()
    {
        if (selectedHero == null) return;
        if (!testMoney.HasEnoughSoul(selectedHero.jobCategory, heroPrice)) return;

        // 중복 방지: 이미 고용했으면 스킵
        if (hiredIds.Contains(selectedHero.id_job)) return;

        // 실제 고용
        testHero.jobs.Add(selectedHero);
        testMoney.PayHeroPrice(selectedHero.jobCategory, heroPrice);

        // 상태 기록
        hiredIds.Add(selectedHero.id_job);

        // ✅ 리스트에서 해당 영웅 버튼(들) 비활성화
        if (heroButtons.TryGetValue(selectedHero.id_job, out var btns))
        {
            foreach (var b in btns)
                if (b) b.interactable = false;
        }

        // 확인 버튼 라벨/상태(원하면 유지)
        confirmButton.interactable = false;
        employText.text = "고용 완료";

        // 외부 패널/리스트 갱신(필요에 따라)
        heroListUp?.RefreshHeroList();
        listUpManager?.RefreshList();
    }

    // 외부에서 호출하는 기존 API 유지
    public void ResetButtonImage() => base.ResetSelectedButton();
}