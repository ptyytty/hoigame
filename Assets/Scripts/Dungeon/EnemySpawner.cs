using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;               // 3D NavMesh 사용 시
using UnityEngine.SceneManagement;  // 전투 씬 로드 옵션

/// <summary>
/// 파티가 트리거 존을 통과하면 EnemyCatalog에서 원하는 수만큼 랜덤 스폰 후, 즉시 전투를 시작한다.
/// - 2D라면 OnTriggerEnter2D/Collider2D, Physics2D.OverlapCircle 등으로 치환
/// - 오브젝트 풀, 웨이브, 재스폰, 카메라 연출도 이 파일에서 쉽게 확장 가능
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SpawnAndBattleZone : MonoBehaviour
{
    // ====== 파티 연동(간소화) ======
    /// <summary>
    /// 외부 파티 제공자. 같은 파일 안에 정의된 인터페이스를 구현한 컴포넌트를 지정하면 런타임에서 자동으로 영웅 목록을 읽어옵니다.
    /// 미지정 시 fallbackParty 사용.
    /// </summary>
    [SerializeField] private MonoBehaviour partyProvider;

    /// <summary>
    /// partyProvider를 쓰지 않는다면, 인스펙터나 코드에서 직접 세팅할 수 있는 파티 목록입니다.
    /// </summary>
    [SerializeField] private List<Job> fallbackParty = new();

    // ====== 카탈로그 & 스폰 옵션 ======
    [Header("Catalog & Count")]
    public EnemyCatalog catalog;            // ScriptableObject

    // 스폰될 몬스터 수
    [Min(1)] public int minCount = 2;
    [Min(1)] public int maxCount = 4;

    [Header("Trigger & Once")] 
    public string partyTag = "Party"; // 파티 오브젝트에 이 태그 지정
    public bool triggerOnce = true;          // 한 번만 발동

    [Header("Placement")] 
    public bool useNavMesh = true;
    public float navSampleRange = 2f;
    public LayerMask blockMask;              // 장애물/지형 레이어 (스폰 불가)
    public float extraSeparation = 0.2f;     // 카탈로그 반경 + 추가 간격

    [Header("Battle Start")]
    public BattleStartMode battleStart = BattleStartMode.LoadSceneAdditive; // 전투 씬 전환
    public string battleSceneName = "Battle";
    public bool loadSceneAdditively = true;

    [Header("Debug")] 
    public bool showGizmos = true;  // 에디터에서 존 범위 표시

    private BoxCollider box;
    private bool consumed;

    // ====== 내부 타입/헬퍼 ======
    public enum BattleStartMode { LoadSceneAdditive, InvokeCSharpEvent, DoNothing } // 1. Additive 모드 진입 2. 외부 전투 시스템 호출 3. 아무 것도 하지 않음

    /// <summary>
    /// 외부 전투 시스템에 직접 연결하고 싶을 때 구독 가능한 이벤트.
    /// UnityEvent는 List<T> 시리얼라이즈 제약이 있어 C# event로 제공.
    /// </summary>
    public static event System.Action<IReadOnlyList<Job>, IReadOnlyList<GameObject>> OnBattleStart;

    public interface IHeroPartyProvider { IReadOnlyList<Job> GetParty(); }

    public static class BattleContext
    {
        public static List<Job> Heroes;
        public static List<GameObject> Enemies;
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
        if (triggerOnce && consumed) return;
        if (!other.CompareTag(partyTag)) return;
        StartCoroutine(SpawnAndStartBattle());
    }

    IEnumerator SpawnAndStartBattle()
    {
        consumed = true;
        if (catalog == null || catalog.entries == null || catalog.entries.Count == 0)
        {
            Debug.LogWarning("[SpawnAndBattleZone] Catalog is empty.");
            yield break;
        }

        int want = Random.Range(minCount, maxCount + 1);
        var picks = catalog.PickMany(want);

        var positions = FindSpawnPositions(picks);
        if (positions.Count == 0)
        {
            Debug.LogWarning("[SpawnAndBattleZone] No valid positions.");
            yield break;
        }

        var spawned = new List<GameObject>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            var def = picks[i];
            if (def?.prefab == null) continue;
            var go = Instantiate(def.prefab, positions[i], Quaternion.identity);
            spawned.Add(go);
            yield return null; // 프레임 분산(옵션)
        }

        var heroes = ResolveParty();
        if (heroes == null || heroes.Count == 0)
        {
            Debug.LogWarning("[SpawnAndBattleZone] No heroes in party.");
            yield break;
        }

        BeginBattle(heroes, spawned);
    }

    IReadOnlyList<Job> ResolveParty()
    {
        if (partyProvider is IHeroPartyProvider provider)
            return provider.GetParty();
        return fallbackParty;
    }

    void BeginBattle(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        // 공용 컨텍스트에 주입(씬 로드 방식에서 사용)
        BattleContext.Heroes  = new List<Job>(heroes);
        BattleContext.Enemies = new List<GameObject>(enemies);

        switch (battleStart)
        {
            case BattleStartMode.LoadSceneAdditive:
                if (!string.IsNullOrEmpty(battleSceneName))
                {
                    if (loadSceneAdditively) SceneManager.LoadScene(battleSceneName, LoadSceneMode.Additive);
                    else SceneManager.LoadScene(battleSceneName);
                }
                else Debug.LogWarning("[SpawnAndBattleZone] battleSceneName is empty.");
                break;

            case BattleStartMode.InvokeCSharpEvent:
                OnBattleStart?.Invoke(heroes, enemies);
                break;

            case BattleStartMode.DoNothing:
                // 외부 시스템이 BattleContext를 직접 읽어 사용하는 경우
                break;
        }
    }

    List<Vector3> FindSpawnPositions(List<EnemyCatalog.Entry> defs)
    {
        var result = new List<Vector3>();
        if (box == null) return result;

        var bounds = box.bounds; // 월드 좌표
        int triesPerEnemy = 16;  // 시도 수 증가 시 밀집에서도 성공률↑

        for (int i = 0; i < defs.Count; i++)
        {
            var def = defs[i];
            float sep = (def != null ? def.separationRadius : 0.6f) + extraSeparation;

            bool placed = false;
            for (int t = 0; t < triesPerEnemy; t++)
            {
                var p = RandomPointInBounds(bounds);

                if (useNavMesh && NavMesh.SamplePosition(p, out var hit, navSampleRange, NavMesh.AllAreas))
                    p = hit.position;

                // 서로 간격 유지 + 장애물 충돌 확인
                if (Physics.CheckSphere(p, sep, blockMask))
                    continue;
                if (result.Any(r => Vector3.SqrMagnitude(r - p) < sep * sep))
                    continue;

                result.Add(p);
                placed = true;
                break;
            }

            // 공간 부족하면 남은 적 스킵(원하는 수만큼 못 채울 수 있음)
            if (!placed) break;
        }
        return result;
    }

    static Vector3 RandomPointInBounds(Bounds b)
    {
        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            b.center.y,
            Random.Range(b.min.z, b.max.z)
        );
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        var c = Gizmos.color;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.35f);
        var col = GetComponent<BoxCollider>();
        if (col)
        {
            var m = Matrix4x4.TRS(transform.TransformPoint(col.center), transform.rotation, Vector3.Scale(transform.lossyScale, col.size));
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
        Gizmos.color = c;
    }
}