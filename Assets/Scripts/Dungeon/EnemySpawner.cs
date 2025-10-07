using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 몬스터 스포너
[RequireComponent(typeof(BoxCollider))]
public class EnemySpawner : MonoBehaviour
{
    // ===== 몬스터 카탈로그 =====
    public EnemyCatalog catalog;

    // ===== Trigger =====
    public string partyTag = "Party";   // trigger tag
    public bool triggerOnce = true;     // 트리거 한 번만 발동

    // ===== Party Direction =====
    public enum DirectionMode { Auto, Velocity, TransformForward, ApproachToSpawner }   // 움직이는 속도 / 트랜스폼 벡터 / 스포너 중심을 향하는 벡터
    [Tooltip("파티 정면 방향 계산 방식")]
    public DirectionMode directionMode = DirectionMode.Auto;
    [Tooltip("계산된 진행 방향을 반대로 뒤집습니다.")]
    [SerializeField] bool invertDirection = false;

    // ===== Spawn Pivot =====
    public enum PivotMode { ZoneCenter, PartyOffset }       // 스포너 중심 / 파티 앞쪽 오프셋
    public PivotMode pivot = PivotMode.PartyOffset;
    [Tooltip("파티 전진 방향으로 얼마나 앞에 스폰할지(m)")]
    public float forwardOffset = 6f;                        // 스폰 위치 거리
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
    public static event System.Action<Vector3, Quaternion, float> OnEnemyFocusHint;     // VCam_Enemy 전용
    public interface IHeroPartyProvider { IReadOnlyList<Job> GetParty(); }

    // 히어로, 몬스터 리스트 정적 보관
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
    private Vector3 lastPartyForward = Vector3.forward;     // 트랜스폼 저장 시 진행 방향
    private Transform lastPartyRoot;                        // 파티 트랜스폼

    void D(string msg)
    {
        if (!debugLogs) return;
        if (dbgCount++ >= debugMaxLogs) return;
        Debug.Log($"[EnemySpawnerDBG] {msg}", this);
    }

    // 에디터에서 Trigger 강제 리셋
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

    // 1) 태그 확인 (이미 소비된 스포너, triggerOnce => 무시)
    void OnTriggerEnter(Collider other)
    {
        bool tagged = other.CompareTag(partyTag) || other.transform.root.CompareTag(partyTag);
        D($"Trigger by {other.name}, tagOk={tagged}, other.tag={other.tag}, root.tag={other.transform.root.tag}");
        if (!tagged) return;
        if (triggerOnce && consumed) return;

        lastPartyRoot = other.transform.root;
        lastPartyForward = GetPartyDirection(directionMode, true);
        D($"PartyRoot={lastPartyRoot.name}, partyDir={lastPartyForward}");

        // 파티 이동 정지
        DungeonManager.instance?.StopMoveHard();

        StartCoroutine(SpawnAndStartBattle());
    }

    // 2) 몬스터 스폰 및 전투 시작
    IEnumerator SpawnAndStartBattle()
    {
        consumed = true;        // 스포너 소비 확인

        var f = catalog.PickFormation();        // f = 카탈로그 내 선택된 몬스터 (스폰될 몬스터)

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

        // 프리팹 하나만 스폰
        var root = Instantiate(f.prefab, pos, rot);
        D($"Formation spawned at {pos}, rotY={rot.eulerAngles.y:F1}, yawAdd={f.yawOffsetDeg}");

        var renderBounds = ComputeBoundsFromRenderers(root);
        var center = renderBounds.center;
        float ortho = OrthoFromBounds(renderBounds, 1.2f); // 패딩은 상황 보며 1.1~1.3

        OnEnemyFocusHint?.Invoke(center, rot, ortho);

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

    // PartyBridge.Instance로 파티 정보 수집
    private IReadOnlyList<Job> ResolveParty()
    {
        var bridge = PartyBridge.Instance;
        if (bridge != null && bridge.HasParty())
            return bridge.ActiveParty;

        return System.Array.Empty<Job>();
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

    // ===== 몬스터 스폰 방향 지정 (파티 진행 반대 방향으로 회전) =====
    Quaternion ComputeFacingRotation_FacePartyForwardOpposite(Vector3 spawnPos)
    {
        Vector3 dir = -lastPartyForward;
        if (yawOnly) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        look *= Quaternion.Euler(0f, yawOffsetDeg, 0f);
        return look;
    }

    // ===== Battle Start: Invoke C# Event만 사용 =====
    // BattleManager, DungeonManager가 구독해서 전투 구현
    void BeginBattle_InvokeEvent(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        BattleContext.Heroes = new List<Job>(heroes);
        BattleContext.Enemies = new List<GameObject>(enemies);
        int subs = OnBattleStart?.GetInvocationList()?.Length ?? 0;
        Debug.Log($"[EnemySpawner] BeginBattle enter: heroes={heroes.Count}, enemies={enemies.Count}, subs={subs}");
        OnBattleStart?.Invoke(heroes, enemies);     // event 시작
    }

    // 몬스터 스폰 지점 계산
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

    // ========== Camaer OrthoSize 계산 ============
    static Bounds ComputeBoundsFromRenderers(GameObject root, float minExpand = 2f)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs == null || rs.Length == 0)
            return new Bounds(root.transform.position, new Vector3(minExpand, minExpand, 0f));
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        // 너무 얇을 때 대비한 소량 확장
        b.Expand(new Vector3(minExpand, minExpand, 0f));
        return b;
    }

    static float OrthoFromBounds(Bounds b, float padding = 1.2f)
    {
        float aspect = (Screen.height == 0) ? 1f : (float)Screen.width / Screen.height;
        float needed = Mathf.Max(b.extents.y, b.extents.x / aspect) * padding;
        return Mathf.Max(needed, 1f);
    }

    //============ 트리거 박스, 스폰 피봇 확인 ==============
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
