using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 던전 첫 입장 등 특정 타이밍에, 슬롯 후보들 중 랜덤으로 골라
/// EnemySpawner 프리팹을 배치하는 전용 매니저.
/// </summary>

public class SpawnerPlacer : MonoBehaviour
{
    [Header("Spawner Prefab")]
    [Tooltip("EnemySpawner가 붙어있는 프리팹을 연결하세요.")]
    public GameObject spawnerPrefab;

    [Header("Count & Random")]
    [Min(0)] public int minCount = 2;
    [Min(1)] public int maxCount = 5;
    [Tooltip("고정 시드(재현용). 음수면 매번 다른 결과.")]
    public int seed = -1;

    [Header("Parent / Container")]
    [Tooltip("생성될 스포너들의 부모 Transform (비워두면 본 오브젝트 하위의 컨테이너가 자동 생성).")]
    public Transform parentForSpawned;
    [Tooltip("컨테이너 오브젝트 이름(부모가 비어있을 때만 사용).")]
    public string containerName = "_SpawnedSpawners";
    [Tooltip("배치 전에 컨테이너 내부를 비웁니다.")]
    public bool clearBeforePlace = true;

    [Header("When To Run")]
    [Tooltip("Start() 시점에 자동 배치할지 여부.")]
    public bool runOnStart = true;

    [Header("Slot Sources")]
    [Tooltip("수동으로 지정한 슬롯(우선 사용).")]
    public List<Transform> manualSlots = new List<Transform>();

    [Tooltip("Floor 태그 오브젝트 하위에서 슬롯을 자동 스캔합니다.")]
    public bool autoScanFloor = true;

    [Tooltip("이 태그를 가진 오브젝트는 슬롯으로 간주합니다.")]
    public string slotTag = "SpawnerSlot";

    [Tooltip("이름에 해당 문자열이 포함되면 슬롯으로 간주합니다.")]
    public string[] slotNameFilters = { "Spawner", "EnemySpawn", "MonsterSpawn" };

    const string kPlacedPrefix = "EnemySpawner_";
    Transform _container; // 실제 생성 부모

    static bool s_placedThisPlay;  // 플레이 중 1회만

    void Awake()
    {
        if (seed >= 0) Random.InitState(seed);
    }

    void Start()
    {
        if (runOnStart && !s_placedThisPlay)
        {
            PlaceNow();
            s_placedThisPlay = true;
        }
    }

    [ContextMenu("Place Now")]
    public void PlaceNow()
    {
        if (!spawnerPrefab)
        {
            Debug.LogWarning("[SpawnerPlacer] spawnerPrefab이 비어있습니다.");
            return;
        }

        var slots = GatherSlots();
        if (slots.Count == 0)
        {
            Debug.LogWarning("[SpawnerPlacer] 슬롯 후보를 찾지 못했습니다.");
            return;
        }

        _container = EnsureContainer();

        if (clearBeforePlace)
            ClearContainerChildren(_container);

        int toPlace = Mathf.Clamp(RandomRange(minCount, maxCount + 1), 0, slots.Count);
        Shuffle(slots);

        for (int i = 0; i < toPlace; i++)
        {
            var s = slots[i];
            var go = Instantiate(spawnerPrefab, s.position, s.rotation, _container);
            go.name = $"{kPlacedPrefix}{s.name}";
        }

        Debug.Log($"[SpawnerPlacer] Placed {toPlace} spawners to '{_container.name}'.");
    }

    // ===== Helpers =====

    Transform EnsureContainer()
    {
        if (parentForSpawned) return parentForSpawned;

        // 본 오브젝트 하위에 컨테이너를 만든다(또는 재사용)
        var t = transform.Find(containerName);
        if (t) return t;

        var obj = new GameObject(containerName);
        obj.transform.SetParent(transform, false);
        return obj.transform;
    }

    void ClearContainerChildren(Transform container)
    {
        var toDelete = new List<Transform>();
        foreach (Transform c in container)
            toDelete.Add(c);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // 에디터 모드에서도 즉시 제거
            foreach (var c in toDelete) Undo.DestroyObjectImmediate(c.gameObject);
            return;
        }
#endif
        foreach (var c in toDelete) Destroy(c.gameObject);
    }

    List<Transform> GatherSlots()
    {
        var result = new List<Transform>(manualSlots);

        // 1) 태그로 전체 씬에서 수집 (SpawnerSlot)
        if (!string.IsNullOrEmpty(slotTag))
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(slotTag))
            {
                if (go && go.transform) result.Add(go.transform);
            }
        }

        // 2) (선택) Floor 태그 하위 이름 필터 스캔
        if (autoScanFloor)
        {
            foreach (var floor in GameObject.FindGameObjectsWithTag("Floor"))
            {
                foreach (Transform c in floor.transform)
                {
                    if (IsSlotCandidate(c)) result.Add(c);
                }
            }
        }

        return Dedup(result);
    }

    bool IsSlotCandidate(Transform t)
    {
        if (!string.IsNullOrEmpty(slotTag) && t.CompareTag(slotTag))
            return true;

        var name = t.name.ToLower();
        foreach (var f in slotNameFilters)
        {
            if (!string.IsNullOrEmpty(f) && name.Contains(f.ToLower()))
                return true;
        }
        return false;
    }

    List<Transform> Dedup(List<Transform> list)
    {
        var set = new HashSet<Transform>(list);
        return new List<Transform>(set);
    }

    int RandomRange(int minInclusive, int maxExclusive)
    {
        return Random.Range(minInclusive, maxExclusive);
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RandomRange(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

#if UNITY_EDITOR
    // 간단한 슬롯 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.identity;

        // 수동 슬롯
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.4f);
        foreach (var t in manualSlots)
        {
            if (!t) continue;
            Gizmos.DrawSphere(t.position, 0.3f);
        }

        // 자동 스캔 미리보기(러프)
        if (autoScanFloor)
        {
            var preview = new List<Transform>();
            foreach (var floor in GameObject.FindGameObjectsWithTag("Floor"))
            {
                foreach (Transform c in floor.transform)
                    if (IsSlotCandidate(c)) preview.Add(c);
            }
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
            foreach (var t in preview)
                Gizmos.DrawWireCube(t.position, Vector3.one * 0.6f);
        }
    }
#endif
}