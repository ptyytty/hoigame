using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 몬스터 스포너
[RequireComponent(typeof(BoxCollider))]
public class EnemySpawner : MonoBehaviour
{
    // ===== Party =====
    [SerializeField] private MonoBehaviour partyProvider; // IHeroPartyProvider 구현체(없으면 fallbackParty 사용)
    [SerializeField] private List<Job> fallbackParty = new();

    // ===== Catalog (Formation 전용) =====
    public EnemyCatalog catalog;

    // ===== Trigger =====
    public string partyTag = "Party";   // trigger tag
    public bool triggerOnce = true;     // 트리거 한 번만 발동

    // ===== Party Direction =====
    public enum DirectionMode { Auto, Velocity, TransformForward, ApproachToSpawner }
    [Tooltip("파티 정면 방향 계산 방식")]
    public DirectionMode directionMode = DirectionMode.Auto;
    [Tooltip("계산된 진행 방향을 반대로 뒤집습니다.")]
    public bool invertDirection = false;

    // ===== Spawn Pivot =====
    public enum PivotMode { ZoneCenter, PartyOffset }
    public PivotMode pivot = PivotMode.PartyOffset;
    [Tooltip("파티 전진 방향으로 얼마나 앞에 스폰할지(m)")]
    public float forwardOffset = 6f;
    public float verticalOffset = 0f;
    [Tooltip("파티와의 최소 이격 거리. 이보다 가까우면 전방 오프셋을 자동 보정")]
    public float minDistanceFromParty = 2f;

    // ===== NavMesh 스냅(선택) =====
    public bool useNavMesh = true;
    public float navSampleRange = 2f;
    public float navSnapMaxDistance = 0.75f;

    // ===== Facing (고정: 파티 진행 반대) =====
    [Tooltip("Yaw(수평)만 회전. 경사면에서도 고개를 들썩이지 않음")]
    public bool yawOnly = true;
    [Tooltip("추가 Yaw 오프셋(도). 0=정면, 180=등 돌리기")]
    public float yawOffsetDeg = 0f;

    // ===== Battle Start (고정: Invoke C# Event) =====
    public static event System.Action<IReadOnlyList<Job>, IReadOnlyList<GameObject>> OnBattleStart;
    public interface IHeroPartyProvider { IReadOnlyList<Job> GetParty(); }

    public static class BattleContext
    {
        public static List<Job> Heroes;
        public static List<GameObject> Enemies;
    }

    // ===== Debug =====
    public bool showGizmos = true;
    public bool debugLogs = false;
    [Range(10, 2000)] public int debugMaxLogs = 200;

    private int dbgCount;
    private BoxCollider box;
    private bool consumed;
    private Vector3 lastPartyForward = Vector3.forward;
    private Transform lastPartyRoot;

    void D(string msg)
    {
        if (!debugLogs) return;
        if (dbgCount++ >= debugMaxLogs) return;
        Debug.Log($"[EnemySpawnerDBG] {msg}", this);
    }

    void Reset()
    {
        box = GetComponent<BoxCollider>();
        if (box) box.isTrigger = true;
    }
    void Awake()
    {
        box = GetComponent<BoxCollider>();
        if (box && !box.isTrigger) box.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        bool tagged = other.CompareTag(partyTag) || other.transform.root.CompareTag(partyTag);
        D($"Trigger by {other.name}, tagOk={tagged}, other.tag={other.tag}, root.tag={other.transform.root.tag}");
        if (!tagged) return;
        if (triggerOnce && consumed) return;

        lastPartyRoot = other.transform.root;
        lastPartyForward = GetPartyDirection(directionMode, true);
        D($"PartyRoot={lastPartyRoot.name}, partyDir={lastPartyForward}");

        // ★★★ 스폰 계산 전에 파티 이동을 확정적으로 멈춤 ★★★
        //FreezeParty(lastPartyRoot);
        DungeonManager.instance?.StopMoveHard();

        StartCoroutine(SpawnAndStartBattle());
    }

    IEnumerator SpawnAndStartBattle()
    {
        consumed = true;
        if (catalog == null)
        {
            Debug.LogWarning("[EnemySpawner] Catalog is null.");
            yield break;
        }

        // === Formation 전용 경로 ===
        if (catalog.selectionMode != EnemyCatalog.SelectionMode.Formations)
        {
            Debug.LogWarning("[EnemySpawner] Catalog is not in Formations mode. Please set to Formations.");
            yield break;
        }

        var f = catalog.PickFormation();
        if (f == null || f.prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] No formation prefab in catalog.");
            yield break;
        }

        // 피벗 계산 (파티 기준 오프셋 또는 존 중심)
        Vector3 pos = GetSpawnPivotWorld();

        // (선택) NavMesh 스냅
        if (useNavMesh && NavMesh.SamplePosition(pos, out var hit, navSampleRange, NavMesh.AllAreas))
        {
            if ((hit.position - pos).sqrMagnitude <= navSnapMaxDistance * navSnapMaxDistance)
                pos = hit.position;
        }

        // Facing: 파티 진행 반대로 고정 (+ 추가 yaw)
        Quaternion rot = ComputeFacingRotation_FacePartyForwardOpposite(pos);
        if (!Mathf.Approximately(f.yawOffsetDeg, 0f)) rot *= Quaternion.Euler(0f, f.yawOffsetDeg, 0f);

        // 프리팹 하나만 스폰 (내부가 이미 2x2 구성)
        var root = Instantiate(f.prefab, pos, rot);
        D($"Formation spawned at {pos}, rotY={rot.eulerAngles.y:F1}, yawAdd={f.yawOffsetDeg}");

        // 적 리스트는 프리팹(및 자식)으로 수집
        var enemies = CollectFormationEnemies(root);
        if (enemies.Count == 0) enemies.Add(root);

        // 파티 해석
        var heroes = ResolveParty();
        if (heroes == null || heroes.Count == 0)
        {
            Debug.LogWarning("[EnemySpawner] No heroes in party.");
            yield break;
        }

        // 전투 시작 이벤트
        BeginBattle_InvokeEvent(heroes, enemies);
        yield break;
    }

    private IReadOnlyList<Job> ResolveParty()
    {
        var bridge = PartyBridge.Instance;
        if (bridge != null && bridge.HasParty())
            return bridge.ActiveParty;

        return System.Array.Empty<Job>();
    }

    // ===== 파티 정보 전달 =====
    public static class PartyInbox
    {
        private static readonly List<Job> _incoming = new(4);

        public static void Set(IEnumerable<Job> party)
        {
            _incoming.Clear();
            if (party == null) return;
            foreach (var h in party)
                if (h != null) _incoming.Add(h);
        }

        public static bool Has => _incoming.Count > 0;
        public static IReadOnlyList<Job> Get() => _incoming;
        public static void Clear() => _incoming.Clear();
    }

    // ===== Battle Start: Invoke C# Event만 사용 =====
    void BeginBattle_InvokeEvent(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        BattleContext.Heroes = new List<Job>(heroes);
        BattleContext.Enemies = new List<GameObject>(enemies);
        int subs = OnBattleStart?.GetInvocationList()?.Length ?? 0;
        Debug.Log($"[EnemySpawner] BeginBattle enter: heroes={heroes.Count}, enemies={enemies.Count}, subs={subs}");
        OnBattleStart?.Invoke(heroes, enemies);
    }

    // ===== Facing 고정: '파티 진행 반대' =====
    Quaternion ComputeFacingRotation_FacePartyForwardOpposite(Vector3 spawnPos)
    {
        Vector3 dir = -lastPartyForward;
        if (yawOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        look *= Quaternion.Euler(0f, yawOffsetDeg, 0f);
        return look;
    }

    // ===== Party Direction / Pivot =====
    Vector3 GetPartyDirection(DirectionMode mode, bool refresh = false)
    {
        Vector3 dir = lastPartyForward;
        if (lastPartyRoot)
        {
            Vector3 center = transform.TransformPoint(box ? box.center : Vector3.zero);
            switch (mode)
            {
                case DirectionMode.Velocity:
                {
                    var rb = lastPartyRoot.GetComponent<Rigidbody>();
                    if (rb != null && rb.velocity.sqrMagnitude > 0.01f) { dir = rb.velocity; break; }
                    var cc = lastPartyRoot.GetComponent<CharacterController>();
                    if (cc != null && cc.velocity.sqrMagnitude > 0.01f) { dir = cc.velocity; break; }
                    var agent = lastPartyRoot.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.velocity.sqrMagnitude > 0.01f) { dir = agent.velocity; break; }
                    dir = lastPartyRoot.forward; break;
                }
                case DirectionMode.TransformForward:
                    dir = lastPartyRoot.forward; break;
                case DirectionMode.ApproachToSpawner:
                    dir = (center - lastPartyRoot.position); break;
                case DirectionMode.Auto:
                default:
                {
                    var rb = lastPartyRoot.GetComponent<Rigidbody>();
                    if (rb != null && rb.velocity.sqrMagnitude > 0.01f) dir = rb.velocity;
                    else
                    {
                        var cc = lastPartyRoot.GetComponent<CharacterController>();
                        if (cc != null && cc.velocity.sqrMagnitude > 0.01f) dir = cc.velocity;
                        else
                        {
                            var agent = lastPartyRoot.GetComponent<NavMeshAgent>();
                            if (agent != null && agent.velocity.sqrMagnitude > 0.01f) dir = agent.velocity;
                            else
                            {
                                Vector3 appr = (center - lastPartyRoot.position);
                                dir = (appr.sqrMagnitude > 0.0001f) ? appr : lastPartyRoot.forward;
                            }
                        }
                    }
                    break;
                }
            }
        }
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir = dir.normalized;

        if (invertDirection) dir = -dir;
        if (refresh) lastPartyForward = dir;
        return dir;
    }

    Vector3 GetSpawnPivotWorld()
    {
        if (pivot == PivotMode.PartyOffset && lastPartyRoot)
        {
            // 파티 전방 확보가 부족하면 forwardOffset 자동 보정
            var f = GetPartyDirection(directionMode, false);
            float needFwd = minDistanceFromParty + 0.1f;
            if (forwardOffset < needFwd) forwardOffset = needFwd;

            return lastPartyRoot.position + f * Mathf.Abs(forwardOffset) + Vector3.up * verticalOffset;
        }
        // 존 중심
        return transform.TransformPoint(box ? box.center : Vector3.zero);
    }

    // 프리팹 및 자식에서 씬에 소속된 적 오브젝트 수집
    List<GameObject> CollectFormationEnemies(GameObject root)
    {
        var list = new List<GameObject>();
        var trs = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in trs)
        {
            if (t == null || t == root.transform) continue;
            if (t.gameObject.scene.IsValid())
                list.Add(t.gameObject);
        }
        return list;
    }

    // ★★★ 파티 이동 확정 정지(이벤트/리스너 없이 내부에서 즉시) ★★★
    // void FreezeParty(Transform root)
    // {
    //     if (!root) return;
    //     D($"FreezeParty root={root.name}");

    //     // (1) NavMeshAgent 전부 정지
    //     var agents = root.GetComponentsInChildren<NavMeshAgent>(true);
    //     for (int i = 0; i < agents.Length; i++)
    //     {
    //         var ag = agents[i];
    //         if (!ag) continue;
    //         ag.isStopped = true;
    //         ag.ResetPath();
    //         ag.velocity = Vector3.zero;
    //         ag.updateRotation = false; // 회전도 멈춤
    //     }

    //     // (2) Rigidbody 전부 정지(물리/루트모션로 밀리는 것 차단)
    //     var rbs = root.GetComponentsInChildren<Rigidbody>(true);
    //     for (int i = 0; i < rbs.Length; i++)
    //     {
    //         var rb = rbs[i];
    //         if (!rb) continue;
    //         rb.velocity = Vector3.zero;
    //         rb.angularVelocity = Vector3.zero;
    //         rb.isKinematic = true; // 전투 중 외력으로 움직이지 않게
    //     }

    //     // (3) 대표적인 입력/이동 스크립트 비활성화(이름 패턴 하드코딩)
    //     string[] nameFilters = { "PlayerController", "PartyMover", "Input", "CharacterMove" };
    //     var mbs = root.GetComponentsInChildren<MonoBehaviour>(true);
    //     for (int i = 0; i < mbs.Length; i++)
    //     {
    //         var mb = mbs[i];
    //         if (!mb) continue;
    //         var typeName = mb.GetType().Name;
    //         for (int k = 0; k < nameFilters.Length; k++)
    //         {
    //             if (!string.IsNullOrEmpty(nameFilters[k]) && typeName.Contains(nameFilters[k]))
    //             {
    //                 mb.enabled = false;
    //                 D($"Disable behaviour: {typeName} on {mb.gameObject.name}");
    //                 break;
    //             }
    //         }
    //     }
    // }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        var c = Gizmos.color;
        var col = GetComponent<BoxCollider>();
        if (col)
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.35f);
            var m = Matrix4x4.TRS(transform.TransformPoint(col.center), transform.rotation, Vector3.Scale(transform.lossyScale, col.size));
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
        Gizmos.color = Color.red;
        try { Gizmos.DrawSphere(GetSpawnPivotWorld(), 0.15f); } catch { }
        Gizmos.color = c;
    }
}
