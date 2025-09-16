using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemies/Enemy Catalog", fileName = "EnemyCatalog")]
public class EnemyCatalog : ScriptableObject
{
    // ===== 개별 엔트리(기존) =====
    [System.Serializable]
    public class Entry
    {
        public string id;                       // 식별용(옵션)
        public GameObject prefab;               // 전투용 프리팹
        [Range(1, 100)] public int weight = 10; // 개별 엔트리 가중치
        [Tooltip("스폰 시 서로 겹치지 않도록 확보할 반경(m)")]
        public float separationRadius = 0.6f;
    }

    // ===== 그룹(조합) 모드 =====
    [System.Serializable]
    public class GroupItem
    {
        public GameObject prefab;
        [Min(1)] public int count = 1;
        public float separationRadius = 0.6f; // 해당 몬스터 간격
    }

    [System.Serializable]
    public class Group
    {
        public string id;                        // 조합 이름
        [Range(1, 100)] public int weight = 10;  // 조합 선택 가중치
        public List<GroupItem> items = new();     // 이 조합에 포함될 몬스터들
    }

    // ===== 스폰 아이템(스폰러에 넘길 최종 형태) =====
    [System.Serializable]
    public class SpawnItem
    {
        public GameObject prefab;
        public float separationRadius;
        public SpawnItem(GameObject prefab, float sep) { this.prefab = prefab; separationRadius = sep; }
    }

    public enum SelectionMode { Entries /*개별*/, Groups /*조합*/ }
    [Header("Selection Mode")]
    public SelectionMode selectionMode = SelectionMode.Entries;

    [Header("Entries (개별 몬스터 풀)")]
    public List<Entry> entries = new();

    [Header("Groups (조합 풀)")]
    public List<Group> groups = new();

    // ========== 공개 API ==========
    public List<SpawnItem> BuildSpawnList(int wantCount)
    {
        return selectionMode == SelectionMode.Groups
            ? BuildFromGroups()
            : BuildFromEntries(wantCount);
    }

    // ========== 내부 구현 ==========
    List<SpawnItem> BuildFromEntries(int want)
    {
        var result = new List<SpawnItem>(want);
        if (entries == null || entries.Count == 0) return result;

        for (int i = 0; i < want; i++)
        {
            var e = PickOneEntry();
            if (e?.prefab == null) continue;
            result.Add(new SpawnItem(e.prefab, Mathf.Max(0.05f, e.separationRadius)));
        }
        return result;
    }

    List<SpawnItem> BuildFromGroups()
    {
        var result = new List<SpawnItem>();
        if (groups == null || groups.Count == 0) return result;

        var g = PickOneGroup();
        if (g == null || g.items == null || g.items.Count == 0) return result;

        foreach (var gi in g.items)
        {
            if (gi?.prefab == null || gi.count <= 0) continue;
            float sep = Mathf.Max(0.05f, gi.separationRadius);
            for (int c = 0; c < gi.count; c++)
                result.Add(new SpawnItem(gi.prefab, sep));
        }
        return result;
    }

    Entry PickOneEntry()
    {
        int total = 0;
        foreach (var e in entries) total += Mathf.Max(1, e.weight);
        int r = Random.Range(0, total);
        foreach (var e in entries)
        {
            r -= Mathf.Max(1, e.weight);
            if (r < 0) return e;
        }
        return entries.Count > 0 ? entries[^1] : null;
    }

    Group PickOneGroup()
    {
        int total = 0;
        foreach (var g in groups) total += Mathf.Max(1, g.weight);
        int r = Random.Range(0, total);
        foreach (var g in groups)
        {
            r -= Mathf.Max(1, g.weight);
            if (r < 0) return g;
        }
        return groups.Count > 0 ? groups[^1] : null;
    }
}