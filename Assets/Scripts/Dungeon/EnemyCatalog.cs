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

    // ===== 포메이션(부모 프리팹) 모드 =====
    [System.Serializable]
    public class FormationEntry
    {
        public string id;                         // 포메이션 이름
        public GameObject prefab;                 // 부모 프리팹(자식에 몬스터 배치)
        [Range(1, 100)] public int weight = 10;   // 선택 가중치
        [Tooltip("프리팹의 원래 회전을 유지(=true) / 스포너의 페이싱 규칙으로 회전(=false)")]
        public bool usePrefabRotation = false;
        [Tooltip("추가 Yaw 오프셋(도)")]
        public float yawOffsetDeg = 0f;
    }

    // ===== 스폰 아이템(스폰러에 넘길 최종 형태) =====
    [System.Serializable]
    public class SpawnItem
    {
        public GameObject prefab;
        public float separationRadius;
        public SpawnItem(GameObject prefab, float sep) { this.prefab = prefab; separationRadius = sep; }
    }

    public enum SelectionMode { Entries /*개별*/, Groups /*조합*/, Formations /*부모 프리팹*/ }
    [Header("Selection Mode")]
    public SelectionMode selectionMode = SelectionMode.Entries;

    [Header("Entries (개별 몬스터 풀)")]
    public List<Entry> entries = new();

    [Header("Groups (조합 풀)")]
    public List<Group> groups = new();

    [Header("Formations (부모 프리팹)")]
    public List<FormationEntry> formations = new();
    // ========== 공개 API ==========
    public List<SpawnItem> BuildSpawnList(int wantCount)
    {
        return selectionMode == SelectionMode.Groups
            ? BuildFromGroups()
            : BuildFromEntries(wantCount);
    }

    public FormationEntry PickFormation()
    {
        if (formations == null || formations.Count == 0) return null;
        int total = 0; foreach (var f in formations) total += Mathf.Max(1, f.weight);
        int r = Random.Range(0, total);
        foreach (var f in formations)
        {
            r -= Mathf.Max(1, f.weight);
            if (r < 0) return f;
        }
        return formations.Count > 0 ? formations[^1] : null;
    }
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

    EnemyCatalog.Entry PickOneEntry()
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