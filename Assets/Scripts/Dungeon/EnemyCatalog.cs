using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(menuName = "Game/Enemy Catalog", fileName = "EnemyCatalog")]
public class EnemyCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string id; // 식별용(옵션)
        public GameObject prefab; // 반드시 전투에 사용할 프리팹
        [Range(1, 100)] public int weight = 10; // 가중치 랜덤 선택용 (높을수록 잘 나옴)
        [Tooltip("스폰 시 서로 겹치지 않도록 확보할 반경(m)")]
        public float separationRadius = 0.6f;
    }


    public List<Entry> entries = new();


    public Entry PickOne()
    {
        if (entries == null || entries.Count == 0) return null;
        int total = entries.Sum(e => Mathf.Max(1, e.weight));
        int r = Random.Range(0, total);
        foreach (var e in entries)
        {
            r -= Mathf.Max(1, e.weight);
            if (r < 0) return e;
        }
        return entries[entries.Count - 1];
    }


    public List<Entry> PickMany(int count)
    {
        var list = new List<Entry>(count);
        for (int i = 0; i < count; i++) list.Add(PickOne());
        return list;
    }
}