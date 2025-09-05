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
    [Header("Hero Prototypes DB")]
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
                heroMap[job.id_job] = job; // 프로토타입 보관
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
        if (heroMap == null || heroMap.Count == 0) BuildMaps();
        if (!heroMap.TryGetValue(heroId, out var proto))
        {
            Debug.LogWarning($"[MasterCatalog] Unknown heroId: {heroId}");
            return null;
        }
        return CloneJob(proto);
    }

    private Job CloneJob(Job p)
    {
        return new Job
        {
            id_job      = p.id_job,
            name_job    = p.name_job,
            level       = p.level,  // 저장 로드시 덮어씀
            exp         = p.exp,    // 저장 로드시 덮어씀
            hp          = p.hp,
            def         = p.def,
            res         = p.res,
            spd         = p.spd,
            hit         = p.hit,
            loc         = p.loc,
            category    = p.category,
            jobCategory = p.jobCategory,
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
