using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Save;
using Game.Skills;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 런타임 ↔ Save DTO 변환, 저장 타이밍 관리, 싱글턴 서비스
/// 런타임 데이터 필요할 시 이용
/// </summary>
public class PlayerProgressService : MonoBehaviour
{

    // 저장 작동 테스트
#if UNITY_EDITOR
    [ContextMenu("DEV/Save Now")]
    private async void DEV_SaveNow()
    {
        await SaveAsync();
        Debug.Log("[DEV] Saved.");
    }

    [ContextMenu("DEV/Reload & Apply")]
    private async void DEV_ReloadApply()
    {
        var data = await SaveSystem.LoadAsync();
        ApplyToRuntime(data);
        Debug.Log("[DEV] Reloaded & Applied.");
    }

#endif

    // ==== 새 데이터 생성 ====
#if UNITY_EDITOR
    [ContextMenu("DEV/Delete Save Files ONLY (json & backup)")]
    private void DEV_DeleteSaveFilesOnly()
    {
        if (!EditorUtility.DisplayDialog(
                "Delete Save Files ONLY",
                "json/bak 파일만 삭제합니다. 런타임 상태는 유지됩니다.\n정말 삭제할까요?",
                "Delete", "Cancel")) return;

        bool deleted = SaveSystem.DeleteAllSaveFiles();
        Debug.Log(deleted
            ? "[DEV] Save files deleted."
            : "[DEV] No save files to delete or delete failed.");
    }

    [ContextMenu("DEV/Delete *My* Save → Apply Fresh Runtime")]
    private async void DEV_DeleteMySaveAndApplyFresh()
    {
        if (!EditorUtility.DisplayDialog(
                "Delete My Save & Apply Fresh",
                "저장(json/bak)을 삭제하고, 메모리 상으로 새 세이브를 생성하여 즉시 적용합니다.\n계속할까요?",
                "Yes, do it", "Cancel")) return;

        // 1) 파일 삭제
        bool deleted = SaveSystem.DeleteAllSaveFiles();
        Debug.Log(deleted
            ? "[DEV] Save files deleted."
            : "[DEV] No save files to delete or delete failed.");

        // 2) 새 세이브 생성(디스크엔 아직 없음)
        Current = SaveSystem.NewSave();

        // 3) 런타임 반영(UI/시스템 동기화)
        ApplyToRuntime(Current);

        // 4) 원하면 바로 저장까지
        //    (테스트 도중 파일 없이 유지하고 싶으면 아래 줄을 주석 처리)
        await SaveAsync();

        Debug.Log("[DEV] Fresh runtime applied (and saved).");
    }
#endif

    public static PlayerProgressService Instance { get; private set; }

    // 현재 세이브 상태(메모리)
    public SaveGame Current { get; private set; }

    // 초기 데이터 구성
    #region Starting Heroes (code-defined)
    private static readonly int[] STARTING_HERO_IDS = new int[]
    {
        // TODO: 여기에 원하는 heroId를 나열
        0, 3, 4, 6
    };
    #endregion


    // --- 런타임 시스템 참조(실제 프로젝트에 맞게 할당) ---
    [Header("Runtime Systems")]
    [SerializeField] private TestHero ownHero;                 // 보유 영웅 리스트를 들고있는 런타임 오브젝트
    [SerializeField] private InventorySave playerItems;         // 보유 인벤토리 런타임 오브젝트
    [SerializeField] private MasterCatalog masterCatalog;       // ID->마스터 데이터 맵핑(예: 영웅/아이템 사전)
    [SerializeField] private InventoryRuntime inventoryRuntime; // 현재 보유 재화
    [SerializeField] private TestInventory startingInventory;   // 현재 보유 아이템(프로토 타입)

    public static event System.Action InventoryApplied;

    // 싱글턴
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()      // 비동기식 Start문
    {
        // 1) 로드
        Current = await SaveSystem.LoadAsync();

        // 2) 버전 마이그레이션(필요하면)
        MigrateIfNeeded(Current);

        EnsureStartingHeroesFromDbIfEmpty(Current);
        EnsureStartingInventoryIfEmpty(Current);

        // 3) 저장 상태를 런타임으로 적용
        ApplyToRuntime(Current);

    }

    /// <summary>
    /// 일시정지,종료 시 자동 저장
    /// </summary>
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            _ = SaveAsync();
        }
    }

    private void OnApplicationQuit()
    {
        _ = SaveAsync();
    }

    private void EnsureInstanceIdsOnOwnHeroes()
{
    foreach (var j in ownHero.jobs)
    {
        if (j == null) continue;
        if (string.IsNullOrEmpty(j.instanceId))
            j.instanceId = System.Guid.NewGuid().ToString("N");
    }
}

    private void EnsureStartingHeroesFromDbIfEmpty(SaveGame save)
    {
        if (save == null) return;

        save.heroes ??= new List<HeroSave>();
        if (save.heroes.Count > 0) return;   // 이미 세이브에 보유 영웅이 있으면 시드 안 함

        // ✅ 빌드/배포: 무조건 STARTING_HERO_IDS만 사용 (ownHero 에셋은 무시)
        if (STARTING_HERO_IDS == null || STARTING_HERO_IDS.Length == 0)
        {
            Debug.LogWarning("[Seed] STARTING_HERO_IDS is empty. No heroes will be seeded.");
            return;
        }

        // MasterCatalog 보장 (빨리 실패/폴백)
        if (!masterCatalog)
            masterCatalog = Resources.Load<MasterCatalog>("MasterCatalog");
        if (!masterCatalog)
        {
            Debug.LogError("[Seed] MasterCatalog is missing. Cannot seed heroes.");
            return;
        }

        int added = 0;
        foreach (int heroId in STARTING_HERO_IDS)
        {
            var job = masterCatalog.CreateJobInstance(heroId);
            if (job == null)
            {
                Debug.LogWarning($"[Seed] Unknown heroId: {heroId} (skip)");
                continue;
            }

            var h = new Save.HeroSave
            {
                heroUid = System.Guid.NewGuid().ToString("N"),
                heroId = heroId,
                displayName = string.IsNullOrWhiteSpace(job.name_job) ? $"Hero {heroId}" : job.name_job,
                level = (job.level > 0) ? job.level : 1,
                exp = Mathf.Max(0, job.exp),
                currentHp = (job.maxHp > 0) ? job.maxHp : Mathf.Max(1, job.hp),
                skillLevels = new System.Collections.Generic.Dictionary<int, int>(),
                growthStats = new System.Collections.Generic.Dictionary<string, int>()
            };

            NormalizeHeroSkillLevels(h);
            save.heroes.Add(h);
            added++;
        }

        Debug.Log($"[Seed] Added {added} hero(es) from STARTING_HERO_IDS.");
    }

    // ▼▼ 로컬 정규화 함수 추가 ▼▼
    private static void NormalizeHeroSkillLevels(HeroSave hero)
    {
        if (hero == null) return;

        hero.skillLevels ??= new Dictionary<int, int>();

        // 이 영웅(직업)의 스킬 ID 목록을 카탈로그에서 가져옴
        var ids = SkillCatalog.GetHeroSkillIds(hero.heroId);

        // 누락된 키는 0으로 채우기
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            if (!hero.skillLevels.ContainsKey(id))
                hero.skillLevels[id] = 0;
        }

        // 카탈로그에 없는 키는 제거
        var toRemove = new List<int>();
        foreach (var key in hero.skillLevels.Keys)
            if (!ids.Contains(key))
                toRemove.Add(key);
        for (int i = 0; i < toRemove.Count; i++)
            hero.skillLevels.Remove(toRemove[i]);
    }

    // 아이템 인벤토리
    private void EnsureStartingInventoryIfEmpty(SaveGame save)
    {
        if (save == null) return;

        save.inventory ??= new InventorySave { slots = new List<Save.Item>() };
        save.inventory.slots ??= new List<Save.Item>();

        // 이미 뭔가 있으면 시드 불필요
        if (save.inventory.slots.Count > 0) return;

        if (startingInventory == null) return;

        // TestInventory -> Save.Item
        if (startingInventory.startingConsumeItems != null)
        {
            foreach (var owned in startingInventory.startingConsumeItems)
            {
                if (owned?.itemData == null || owned.count <= 0) continue;
                save.inventory.slots.Add(new Save.Item
                {
                    itemId = owned.itemData.id_item,
                    num = 0,
                    type = ItemType.Consume,
                    count = owned.count
                });
            }
        }

        if (startingInventory.startingEquipItems != null)
        {
            foreach (var owned in startingInventory.startingEquipItems)
            {
                if (owned?.itemData == null || owned.count <= 0) continue;
                save.inventory.slots.Add(new Save.Item
                {
                    itemId = owned.itemData.id_item,
                    num = 0,
                    type = ItemType.Equipment,
                    count = owned.count   // 장비도 count 지원. LoadFromSave에서 count회 추가 처리함.
                });
            }
        }
    }


    // ========== 저장/로드 보조 ==========
    public async Task<bool> SaveAsync()         // Task: 작업의 단위를 반환해주는 변수  Task >> void(반환값X)   Task<int> >> int형 반환
    {
        // 1) 런타임 -> Save DTO
        CaptureFromRuntime();

        // 2) 파일로 기록
        return await SaveSystem.SaveAsync(Current);     // await: 내부 코드가 끝날 때까지 이 지점에서 저ㅓㅇ지
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
        foreach (var hero in ownHero.jobs) // ownHero.jobs: List<Job>
        {
            var h = new HeroSave
            {
                heroId = hero.id_job,
                displayName = string.IsNullOrEmpty(hero.displayName) ? null : hero.displayName,    // 기본값 = 영웅 이름
                level = GetHeroLevel(hero),     // 레벨 대입
                exp = GetHeroExp(hero),         // 경험치 대입
                currentHp = Mathf.Max(0, hero.hp),

                heroUid = string.IsNullOrEmpty(hero.instanceId)
                        ? System.Guid.NewGuid().ToString("N")       // N = 하이픈 없는 32자
                        : hero.instanceId
            };

            // 스킬 성장/업그레이드(예: skillId->level)
            h.skillLevels = GetHeroSkillLevels(hero); // 영웅 스킬 레벨 대입

            // 추가 성장치(선택)
            h.growthStats = GetHeroGrowthStats(hero); // 성장 능력치 대입

            save.heroes.Add(h);     // 현재 영웅 정보 확인
        }

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
        ownHero.jobs.Clear();                  // 보유 영웅 우선 초기화

        foreach (var hero in save.heroes)
        {
            // 마스터 데이터에서 Job 프로토타입/팩토리 호출
            var job = masterCatalog.CreateJobInstance(hero.heroId);
            if (job == null) continue;

            job.instanceId = string.IsNullOrEmpty(hero.heroUid)
                ? Guid.NewGuid().ToString("N")
                : hero.heroUid;

            // 레벨/경험/성장/스킬 적용
            SetHeroLevel(job, hero.level);      // 마스터 데이터 -> ownHero 호출
            SetHeroExp(job, hero.exp);          // 
            ApplyHeroSkillLevels(job, hero.skillLevels);
            ApplyHeroGrowthStats(job, hero.growthStats);

            // 세이브에 표시 이름이 있으면 덮어쓰기
            if (!string.IsNullOrEmpty(hero.displayName))
                job.displayName = hero.displayName;

            // 저장된 현재 체력 복원 (없으면 풀피)
            job.hp = (hero.currentHp > 0) ? Mathf.Min(hero.currentHp, (job.maxHp > 0 ? job.maxHp : job.hp))
                                          : (job.maxHp > 0 ? job.maxHp : job.hp);

            ownHero.jobs.Add(job);             // 보유 영웅 상태 업데이트
        }

        // ---- 인벤토리 ----
        // 저장 데이터 그대로 런타임 참조/복사(필요 시 새로 생성)
        if (save.inventory == null) save.inventory = new InventorySave { slots = new List<Save.Item>() };
        // 런타임에서 보여줄 컨테이너가 따로 있다면 복사:
        if (playerItems == null) playerItems = new InventorySave { slots = new List<Save.Item>() };
        playerItems.slots = new List<Save.Item>(save.inventory.slots ?? new List<Save.Item>());

        // ---- 화폐/자원 ----
        inventoryRuntime.LoadFromSave(save.inventory);            // ← 세이브 → 런타임
        inventoryRuntime.Gold = save.gold;
        inventoryRuntime.redSoul = save.redSoul;
        inventoryRuntime.blueSoul = save.blueSoul;
        inventoryRuntime.purpleSoul = save.purpleSoul;
        inventoryRuntime.greenSoul = save.greenSoul;

        // ---- 아이템 ----
        InventoryApplied?.Invoke();

        EnsureInstanceIdsOnOwnHeroes();
    }

    // ========== 버전 마이그레이션 ==========
    private void MigrateIfNeeded(SaveGame save)
    {
        if (save == null) return;

        // 1) 영웅 컨테이너/필드 정규화
        save.heroes ??= new List<HeroSave>();           //  if (save.heroes == null)    save.heroes = new List<HeroSave>();
        foreach (var hero in save.heroes)
        {
            hero.skillLevels ??= new Dictionary<int, int>();
            hero.growthStats ??= new Dictionary<string, int>();
            if (hero.level <= 0) hero.level = 1;
            if (hero.exp < 0) hero.exp = 0;

            if (string.IsNullOrEmpty(hero.heroUid))
                hero.heroUid = System.Guid.NewGuid().ToString("N");
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
        if (save.version < 1) save.version = 1;
    }


    // ========== 아래는 프로젝트별로 구현해야 하는 부분(샘플/스텁) ==========
    private int GetHeroLevel(Job hero) => hero.level; // 예시
    private int GetHeroExp(Job hero) => hero.exp;     // 예시

    private Dictionary<int, int> GetHeroSkillLevels(Job hero)
    {
        return new Dictionary<int, int>(hero.skillLevels);
    }

    private Dictionary<string, int> GetHeroGrowthStats(Job hero)
    {
        // 예시: 성장치(능력 강화 등) 모아서 저장
        // return new Dictionary<string, int> { { "hp", hero.hpGrowth }, { "def", hero.defGrowth } };
        return new Dictionary<string, int>();
    }

    private void SetHeroLevel(Job hero, int level) { hero.level = level; }
    private void SetHeroExp(Job hero, int exp) { hero.exp = exp; }

    /// <summary>
    /// 영웅 스킬, 성장 상황 업데이트
    /// </summary>
    private void ApplyHeroSkillLevels(Job hero, Dictionary<int, int> map)
    {
        hero.skillLevels.Clear();

        if (map == null) return;

        foreach (var key in map)
        {
            int hId = SkillKey.ExtractHeroId(key.Key);
            if (hId != hero.id_job) continue;
            hero.skillLevels[key.Key] = (int)MathF.Max(0, key.Value);
        }

        // TODO: 스킬 객체/데미지/쿨타임 등에 실제 반영이 필요하면 여기서 적용
    }

    private void ApplyHeroGrowthStats(Job hero, Dictionary<string, int> stats)
    {
        if (stats == null) return;
        // 예시:
        // if (stats.TryGetValue("hp", out var hpUp)) hero.hp += hpUp * 5;
    }
}

