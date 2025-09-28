using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class FormationPreset
{
    [Tooltip("식별/디버깅용 이름")]
    public string name;

    [Tooltip("직계 자식(1~4개)이 달린 부모 프리팹")]
    public GameObject prefab;

    [Tooltip("스폰 시 Y축 추가 회전(도) — EnemySpawner에서 rot *= yawOffsetDeg 적용")]
    public float yawOffsetDeg = 0f;

    [Tooltip("prefab의 '직계 자식' GameObject를 0.. 순서대로 저장(에디터에서 자동 관리)")]
    public List<GameObject> childObjects = new();

    [Tooltip("childObjects와 인덱스가 1:1 매칭되는 몬스터 데이터(비어있으면 기본 몬스터 사용)")]
    public List<MonsterData> monsterSlots = new();   // ★ 추가

    // === 읽기 헬퍼 ===
    public int ChildCount => childObjects?.Count ?? 0;
    public GameObject GetChild(int i) => childObjects[i];
}

[CreateAssetMenu(menuName = "Game/Enemies/Enemy Catalog (Formation Only)", fileName = "EnemyCatalog")]
public class EnemyCatalog : ScriptableObject
{
    [Header("Formation Presets")]
    public List<FormationPreset> formations = new();

    // ===== 런타임 유틸 =====

    /// <summary>
    /// 인덱스로 Formation을 가져옵니다(클램프).
    /// </summary>
    public FormationPreset GetFormation(int index)
    {
        if (formations == null || formations.Count == 0) return null;
        index = Mathf.Clamp(index, 0, formations.Count - 1);
        return formations[index];
    }

    /// <summary>
    /// EnemySpawner가 호출하는 선택 함수. 
    /// indexOverride가 있으면 해당 인덱스를, 없으면 랜덤으로 하나 고릅니다.
    /// </summary>
    public FormationPreset PickFormation(int? indexOverride = null)
    {
        if (formations == null || formations.Count == 0) return null;

        if (indexOverride.HasValue)
        {
            int i = Mathf.Clamp(indexOverride.Value, 0, formations.Count - 1);
            return formations[i];
        }

        int r = Random.Range(0, formations.Count);
        return formations[r];
    }

    /// <summary>
    /// 특정 Formation의 특정 자식을 반환합니다.
    /// </summary>
    public bool TryGetChild(int formationIndex, int childIndex, out GameObject child)
    {
        child = null;
        var f = GetFormation(formationIndex);
        if (f == null || f.ChildCount == 0) return false;
        if (childIndex < 0 || childIndex >= f.ChildCount) return false;
        child = f.childObjects[childIndex];
        return child != null;
    }

#if UNITY_EDITOR
    // ===== 에디터: 자식 스캔/베이크 =====

    /// <summary>
    /// 카탈로그에 등록된 모든 Formation의 자식 리스트를 재구성합니다.
    /// </summary>
    [ContextMenu("Rebuild All (scan prefab children)")]
    void RebuildAll()
    {
        if (formations == null) return;
        foreach (var f in formations) Rebuild(f);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[EnemyCatalog] Rebuilt children for {formations.Count} formations.");
    }

    /// <summary>
    /// 주어진 Formation의 프리팹 '직계 자식'을 0.. 순서대로 childObjects에 저장합니다.
    /// </summary>
    void Rebuild(FormationPreset f)
    {
        if (f == null) return;

        f.childObjects ??= new List<GameObject>();
        f.childObjects.Clear();

        if (!f.prefab)
        {
            Debug.LogWarning($"[EnemyCatalog] {f.name}: prefab is null");
            return;
        }

        // 프리팹 에셋의 '직계 자식'만 0.. 순서대로 저장
        var root = f.prefab.transform;
        int n = root.childCount;
        for (int i = 0; i < n; i++)
        {
            var child = root.GetChild(i)?.gameObject;
            if (child) f.childObjects.Add(child);
        }

        Debug.Log($"[EnemyCatalog] {f.name}: baked {f.childObjects.Count} child objects.");
    }

    /// <summary>
    /// 에디터에서 프리팹 참조/구조가 바뀌면 자동 재스캔합니다.
    /// </summary>
    void OnValidate()
    {
        if (formations == null) return;

        foreach (var f in formations)
        {
            if (f == null) continue;

            if (f.prefab == null)
            {
                f.childObjects?.Clear();
                continue;
            }

            var root = f.prefab.transform;

            // 자식 수 변화, null, 부모가 다른 경우 등 구조 변화가 감지되면 재스캔
            bool needRebuild =
                f.childObjects == null ||
                f.childObjects.Count != root.childCount ||
                f.childObjects.Any(go => go == null || go.transform.parent != root);

            if (needRebuild)
                Rebuild(f);
        }
    }
#endif
}