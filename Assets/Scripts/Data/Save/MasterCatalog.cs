// Assets/Scripts/Master/MasterCatalog.cs
using System.Collections.Generic;
using Save;
using UnityEngine;

/// <summary>
/// 마스터 데이터 중앙 허브:
/// - heroId -> TestHero(프로토타입)에서 런타임 Job 복제본 생성
/// - (선택) itemId -> ItemData 조회
/// </summary>
[CreateAssetMenu(menuName = "Game/Master Catalog", fileName = "MasterCatalog")]
public class MasterCatalog : ScriptableObject
{
    
    [Header("Hero DB")]
    [SerializeField] private TestHero heroDB;   // TestHero.asset 지정

    [Header("Item Master")]
    [SerializeField] private List<Item> itemMasters = new();

    private Dictionary<int, Job> heroMap;
    private Dictionary<int, Item> itemMap;

    private void OnEnable()   => BuildMaps();
    private void OnValidate() => BuildMaps();

    private void BuildMaps()
    {
        // ---- Hero Map ----
        heroMap = new Dictionary<int, Job>();
        if (heroDB != null && heroDB.jobs != null)
        {
            foreach (var job in heroDB.jobs)
            {
                if (job == null) continue;
                if (heroMap.ContainsKey(job.id_job))
                {
                    Debug.LogError($"[MasterCatalog] Duplicate heroId: {job.id_job} ({job.name_job})");
                    continue;
                }
                heroMap[job.id_job] = job; // heroDB 정보 => heroMap으로 복제
            }
        }

        // ---- Item Map ----
        itemMap = new Dictionary<int, Item>();
        foreach (var item in itemMasters)
        {
            if (item == null) continue;
            if (itemMap.ContainsKey(item.itemId))
            {
                Debug.LogError($"[MasterCatalog] Duplicate itemId: {item.itemId} ({item.type})");
                continue;
            }
            itemMap[item.itemId] = item;
        }
    }

    /// <summary> heroId로 런타임 Job 복제본 생성 </summary>
    public Job CreateJobInstance(int heroId)
    {
        if (heroMap == null || heroMap.Count == 0) BuildMaps();     // heroMap 미생성 시 BuilMaps 실행
        if (!heroMap.TryGetValue(heroId, out var hero))
        {
            Debug.LogWarning($"[MasterCatalog] Unknown heroId: {heroId}");
            return null;
        }
        return CloneJob(hero);
    }

    private Job CloneJob(Job hero)  // 영웅 정보 복제
    {
        return new Job
        {
            id_job      = hero.id_job,
            name_job    = hero.name_job,
            level       = hero.level,  // 저장 로드시 덮어씀
            exp         = hero.exp,    // 저장 로드시 덮어씀
            maxHp       = hero.maxHp > 0 ? hero.maxHp : hero.hp,    // 방어적 저장
            hp          = hero.hp,      // 현재 체력
            def         = hero.def,
            res         = hero.res,
            spd         = hero.spd,
            hit         = hero.hit,
            loc         = hero.loc,
            category    = hero.category,
            jobCategory = hero.jobCategory,
            equippedItem = null     // 런타임에서 별도 장비 처리
        };
    }

    // ===== Item =====
    public Item GetItemData(int itemId)
    {
        if (itemMap == null || itemMap.Count == 0) BuildMaps();
        itemMap.TryGetValue(itemId, out var data);
        return data;
    }
}
