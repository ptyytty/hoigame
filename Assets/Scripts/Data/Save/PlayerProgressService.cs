using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Save;
using Unity.VisualScripting;

/// <summary>
/// 런타임 ↔ Save DTO 변환, 저장 타이밍 관리, 싱글턴 서비스
/// </summary>
public class PlayerProgressService : MonoBehaviour
{
    public static PlayerProgressService Instance { get; private set; }

    // 현재 세이브 상태(메모리)
    public SaveGame Current { get; private set; }

    // --- 런타임 시스템 참조(실제 프로젝트에 맞게 할당) ---
    [Header("Runtime Systems")]
    [SerializeField] private TestHero testHero;     // 사용자의 보유 영웅 리스트를 들고있는 런타임 오브젝트(예: jobs 리스트)
    [SerializeField] private InventorySave playerItems; // 사용자의 인벤토리 런타임(예시)
    [SerializeField] private MasterCatalog masterCatalog;       // ID->마스터 데이터 맵핑(예: 영웅/아이템 사전)
    [SerializeField] private InventoryRuntime inventoryRuntime;
    [SerializeField] private TestInventory startingInventory;

    // 싱글턴
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        // 1) 로드
        Current = await SaveSystem.LoadAsync();

        // 2) 버전 마이그레이션(필요하면)
        MigrateIfNeeded(Current);

        // 3) 저장 상태를 런타임으로 적용
        ApplyToRuntime(Current);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            _ = SaveAsync(); // 일시정지 시 저장(에러 무시)
        }
    }

    private void OnApplicationQuit()
    {
        // 종료 시 동기 저장을 원하면 .GetAwaiter().GetResult()로 기다릴 수도 있음
        _ = SaveAsync();
    }

    // ========== 저장/로드 보조 ==========

    public async Task<bool> SaveAsync()
    {
        // 1) 런타임 -> Save DTO
        CaptureFromRuntime();

        // 2) 파일로 기록
        return await SaveSystem.SaveAsync(Current);
    }

    /// <summary>
    /// 현재 런타임 상태를 Save DTO에 반영
    /// </summary>
    public void CaptureFromRuntime()
    {
        var save = Current;
        if (save == null) return;

        // ---- 영웅 보유/성장 ----
        save.heroes.Clear();
        foreach (var hero in testHero.jobs) // testHero.jobs: List<Job>
        {
            var h = new HeroSave
            {
                heroId = hero.id_job,
                level = GetHeroLevel(hero),   // TODO: 프로젝트의 레벨 시스템 참조
                exp = GetHeroExp(hero)      // TODO: 프로젝트의 경험치 시스템 참조
            };

            // 스킬 성장/업그레이드(예: skillId->level)
            h.skillLevels = GetHeroSkillLevels(hero); // TODO: 실제 로직 반환

            // 추가 성장치(선택)
            h.growthStats = GetHeroGrowthStats(hero); // TODO: 실제 로직 반환

            save.heroes.Add(h);
        }

        // ---- 인벤토리 ----

        save.inventory.slots.Clear();

        // ---- 화폐/자원 ----
        save.inventory.slots = inventoryRuntime.ToSaveSlots();    // ← 런타임 → 세이브
        save.gold = inventoryRuntime.Gold;
        save.redSoul = inventoryRuntime.redSoul;
        save.blueSoul = inventoryRuntime.blueSoul;
        save.purpleSoul = inventoryRuntime.purpleSoul;
        save.greenSoul = inventoryRuntime.greenSoul;
    }

    /// <summary>
    /// Save DTO를 런타임 시스템에 적용
    /// </summary>
    public void ApplyToRuntime(SaveGame save)
    {
        // ---- 영웅 ----
        testHero.jobs.Clear();

        foreach (var hs in save.heroes)
        {
            // 마스터 데이터에서 Job 프로토타입/팩토리 호출
            var job = masterCatalog.CreateJobInstance(hs.heroId);
            if (job == null) continue;

            // 레벨/경험/성장/스킬 적용
            SetHeroLevel(job, hs.level);
            SetHeroExp(job, hs.exp);
            ApplyHeroSkillLevels(job, hs.skillLevels);
            ApplyHeroGrowthStats(job, hs.growthStats);

            testHero.jobs.Add(job);
        }

        // ---- 인벤토리 ----
        // 저장 데이터 그대로 런타임 참조/복사(필요 시 새로 생성)
        if (save.inventory == null) save.inventory = new InventorySave { slots = new List<Save.Item>() };
        // 런타임에서 보여줄 컨테이너가 따로 있다면 복사:
        if (playerItems == null) playerItems = new InventorySave { slots = new List<Save.Item>() };
        playerItems.slots = new List<Save.Item>(save.inventory.slots ?? new List<Save.Item>());

        // ---- 화폐/자원 ----
        inventoryRuntime.ClearAll();
        inventoryRuntime.LoadFromSave(save.inventory);            // ← 세이브 → 런타임
        inventoryRuntime.Gold = save.gold;
        inventoryRuntime.redSoul = save.redSoul;
        inventoryRuntime.blueSoul = save.blueSoul;
        inventoryRuntime.purpleSoul = save.purpleSoul;
        inventoryRuntime.greenSoul = save.greenSoul;
    }

    // ========== 버전 마이그레이션 ==========
    private void MigrateIfNeeded(SaveGame save)
    {
        if (save == null) return;

        // 1) 영웅 컨테이너/필드 정규화
        save.heroes ??= new List<HeroSave>();
        foreach (var h in save.heroes)
        {
            h.skillLevels ??= new Dictionary<int, int>();
            h.growthStats ??= new Dictionary<string, int>();
            if (h.level <= 0) h.level = 1;
            if (h.exp < 0) h.exp = 0;
        }

        // 2) 인벤토리 컨테이너/슬롯 정규화
        save.inventory ??= new InventorySave { slots = new List<Item>() };
        save.inventory.slots ??= new List<Item>();

        // 깨진 슬롯 제거 (음수/0, 잘못된 id)
        save.inventory.slots.RemoveAll(s => s == null || s.itemId <= 0 || s.count <= 0);

        // 3) 화폐/자원 정규화 (음수 방지)
        if (save.gold < 0) save.gold = 0;
        save.redSoul = Mathf.Max(0, save.redSoul);
        save.blueSoul = Mathf.Max(0, save.blueSoul);
        save.purpleSoul = Mathf.Max(0, save.purpleSoul);
        save.greenSoul = Mathf.Max(0, save.greenSoul);

        // 4) 스키마 버전 업 (현재 스키마가 2라고 가정)
        if (save.version < 2) save.version = 2;
    }

    // ========== 아래는 프로젝트별로 구현해야 하는 부분(샘플/스텁) ==========
    private int GetHeroLevel(Job hero) => hero.level; // 예시
    private int GetHeroExp(Job hero) => hero.exp;     // 예시

    private Dictionary<int, int> GetHeroSkillLevels(Job hero)
    {
        // 예시: hero가 들고 있는 스킬 목록에서 id->강화레벨을 구성
        // return hero.skills.ToDictionary(s => s.id, s => s.level);
        return new Dictionary<int, int>();
    }

    private Dictionary<string, int> GetHeroGrowthStats(Job hero)
    {
        // 예시: 성장치(능력 강화 등) 모아서 저장
        // return new Dictionary<string, int> { { "hp", hero.hpGrowth }, { "def", hero.defGrowth } };
        return new Dictionary<string, int>();
    }

    private void SetHeroLevel(Job hero, int level) { hero.level = level; }
    private void SetHeroExp(Job hero, int exp) { hero.exp = exp; }

    private void ApplyHeroSkillLevels(Job hero, Dictionary<int, int> map)
    {
        if (map == null) return;
        // 예시:
        // foreach (var kv in map)
        //     hero.GetSkillById(kv.Key).SetLevel(kv.Value);
    }

    private void ApplyHeroGrowthStats(Job hero, Dictionary<string, int> stats)
    {
        if (stats == null) return;
        // 예시:
        // if (stats.TryGetValue("hp", out var hpUp)) hero.hp += hpUp * 5;
    }
}
