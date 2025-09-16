using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class EnemySpawnerV2 : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("이 콜라이더(또는 자식 콜라이더)에 Party가 들어오면 스폰합니다.")]
    [SerializeField] private string partyTag = "Party";
    [SerializeField] private bool oneShot = true;           // 한 번만 스폰
    [SerializeField] private bool disableAfterSpawn = true; // 스폰 후 비활성화

    [Header("Spawn Distance / Position")]
    [Tooltip("기준 지점(파티 또는 스포너)에서 앞으로 얼마나 떨어진 곳에 스폰할지(m).")]
    [Min(0f)] public float spawnDistance = 4f;
    [Tooltip("스폰 기준(거리/방향 계산용)")]
    public SpawnOrigin origin = SpawnOrigin.Party; // Party 또는 Spawner
    [Tooltip("생성 위치에 추가로 더할 로컬 오프셋(스포너 기준).")]
    public Vector3 extraOffset = Vector3.zero;

    public enum SpawnOrigin { Party, Spawner }

    [Header("Facing (방향 설정)")]
    public FacingMode facingMode = FacingMode.PartyForward;
    public float yawOffsetDeg = 0f; // 추가 회전(Yaw)
    public Vector3 customDirection = Vector3.forward;  // FacingMode.CustomDirection 일 때
    public Vector3 fixedEuler = Vector3.zero;          // FacingMode.FixedEuler 일 때

    public enum FacingMode
    {
        PartyForward,     // 파티가 바라보는 방향
        SpawnerForward,   // 스포너(이 오브젝트)가 바라보는 방향
        FaceTowardsParty, // 파티를 향하도록
        CustomDirection,  // 커스텀 벡터 방향
        FixedEuler        // 정확한 오일러 각 고정
    }

    [Header("Encounter (내가 고른 세트 중 하나)")]
    [Tooltip("스폰 시 이 목록 중 하나의 세트를 선택해서 생성합니다.")]
    public List<Encounter> encounters = new();

    [Tooltip("강제로 이 인덱스의 세트를 사용 (-1이면 가중치 랜덤).")]
    public int forcedEncounterIndex = -1;

    [Header("Lifecycle / Events")]
    [Tooltip("스폰된 몬스터들을 부모로 담아둘 컨테이너(없으면 계층 최상위 생성).")]
    public Transform spawnParent;
    public UnityEvent<List<GameObject>> onSpawned; // 스폰 직후 콜백

    [Serializable]
    public class Encounter
    {
        public string name;
        [Tooltip("이 세트에 포함될 몬스터 프리팹들(필수).")]
        public List<GameObject> monsterPrefabs = new();

        [Header("세트 선택 가중치")]
        [Min(0f)] public float weight = 1f;

        [Header("포메이션(선택)")]
        [Tooltip("각 몬스터의 상대 위치(스폰 기준점 기준). 비워두면 자동으로 일렬 배치.")]
        public List<Vector3> localPositions = new();

        [Tooltip("각 몬스터의 추가 회전(옵션).")]
        public List<Vector3> extraEulerPerMonster = new();
    }

    bool _hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if (oneShot && _hasSpawned) return;
        if (!other || !other.CompareTag(partyTag)) return;

        Transform party = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        Spawn(party);
    }

    /// <summary>원할 때 스크립트에서 수동으로 호출해도 됨.</summary>
    public void Spawn(Transform party)
    {
        if (oneShot && _hasSpawned) return;

        // 1) 세트 고르기
        var enc = PickEncounter();
        if (enc == null || enc.monsterPrefabs == null || enc.monsterPrefabs.Count == 0)
        {
            Debug.LogWarning($"[EnemySpawnerV2] 사용할 Encounter가 없습니다. (게임오브젝트: {name})");
            return;
        }

        // 2) 기준점/방향 계산
        Vector3 basePos; Quaternion baseRot;
        ComputeBasePose(party, out basePos, out baseRot);

        // 3) 포메이션 위치들 계산
        var positions = BuildPositions(enc, basePos, baseRot, enc.monsterPrefabs.Count);

        // 4) 스폰
        var spawned = new List<GameObject>(enc.monsterPrefabs.Count);
        for (int i = 0; i < enc.monsterPrefabs.Count; i++)
        {
            var prefab = enc.monsterPrefabs[i];
            if (!prefab) continue;

            Quaternion rot = baseRot;
            if (i < enc.extraEulerPerMonster.Count)
            {
                rot = rot * Quaternion.Euler(enc.extraEulerPerMonster[i]);
            }

            var go = Instantiate(prefab, positions[i], rot, spawnParent ? spawnParent : null);
            spawned.Add(go);
        }

        _hasSpawned = true;
        onSpawned?.Invoke(spawned);

        if (disableAfterSpawn) gameObject.SetActive(false);
    }

    Encounter PickEncounter()
    {
        if (encounters == null || encounters.Count == 0) return null;

        // 강제 인덱스
        if (forcedEncounterIndex >= 0 && forcedEncounterIndex < encounters.Count)
            return encounters[forcedEncounterIndex];

        // 가중치 랜덤
        float sum = 0f;
        foreach (var e in encounters) sum += Mathf.Max(0f, e.weight);
        if (sum <= 0f) return encounters[UnityEngine.Random.Range(0, encounters.Count)];

        float r = UnityEngine.Random.value * sum;
        float acc = 0f;
        foreach (var e in encounters)
        {
            acc += Mathf.Max(0f, e.weight);
            if (r <= acc) return e;
        }
        return encounters[encounters.Count - 1];
    }

    void ComputeBasePose(Transform party, out Vector3 pos, out Quaternion rot)
    {
        // 기준 forward
        Vector3 forward = Vector3.forward;
        switch (facingMode)
        {
            case FacingMode.PartyForward:
                forward = (party ? party.forward : transform.forward);
                rot = Quaternion.LookRotation(SafeDir(forward), Vector3.up);
                break;
            case FacingMode.SpawnerForward:
                forward = transform.forward;
                rot = Quaternion.LookRotation(SafeDir(forward), Vector3.up);
                break;
            case FacingMode.FaceTowardsParty:
                // pos 계산 후에 다시 회전 보정한다(아래에서 최종 rot에 적용).
                rot = Quaternion.identity;
                break;
            case FacingMode.CustomDirection:
                forward = customDirection.sqrMagnitude < 1e-6f ? Vector3.forward : customDirection.normalized;
                rot = Quaternion.LookRotation(SafeDir(forward), Vector3.up);
                break;
            case FacingMode.FixedEuler:
                rot = Quaternion.Euler(fixedEuler);
                break;
            default:
                rot = Quaternion.identity;
                break;
        }

        // 기준 위치
        Transform o = (origin == SpawnOrigin.Party && party) ? party : transform;
        Vector3 basisForward =
            (origin == SpawnOrigin.Party && party) ? party.forward : transform.forward;

        pos = o.position + SafeDir(basisForward) * spawnDistance + transform.TransformVector(extraOffset);

        // FaceTowardsParty 모드면 여기서 회전 결정
        if (facingMode == FacingMode.FaceTowardsParty && party)
        {
            Vector3 toParty = party.position - pos;
            rot = Quaternion.LookRotation(SafeDir(toParty), Vector3.up);
        }

        // 추가 Yaw
        rot = Quaternion.Euler(0f, yawOffsetDeg, 0f) * rot;
    }

    List<Vector3> BuildPositions(Encounter enc, Vector3 basePos, Quaternion baseRot, int count)
    {
        var list = new List<Vector3>(count);
        bool hasCustom = enc.localPositions != null && enc.localPositions.Count > 0;

        if (hasCustom)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 local = i < enc.localPositions.Count ? enc.localPositions[i] : Vector3.zero;
                list.Add(basePos + baseRot * local);
            }
        }
        else
        {
            // 자동 일렬 배치: 전방을 바라본 상태에서 좌우로 펼침
            // 예) -1.5, -0.5, +0.5, +1.5 … (간격 spacing)
            float spacing = 1.2f;
            float start = -(count - 1) * 0.5f * spacing;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(start + i * spacing, 0f, 0f); // 로컬 X로 펼침
                list.Add(basePos + baseRot * offset);
            }
        }
        return list;
    }

    static Vector3 SafeDir(in Vector3 v)
    {
        if (v.sqrMagnitude < 1e-6f) return Vector3.forward;
        var n = v.normalized;
        // Up과 평행하면 LookRotation이 불안정할 수 있어 약간 기울여줌
        if (Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.999f)
            n = (n + Vector3.forward * 0.001f).normalized;
        return n;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 에디터에서 예상 스폰 위치/방향 가시화
        var party = FindPartyTransformForPreview(); // 대략적인 미리보기
        ComputeBasePose(party, out var p, out var r);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, 0.25f);

        // 방향 화살표
        Vector3 f = r * Vector3.forward;
        Gizmos.DrawLine(p, p + f * 1.5f);

        // 기본 4마리 가정하여 자동 배치 미리보기
        int previewCount = 4;
        var dummy = new Encounter();
        var pos = BuildPositions(dummy, p, r, previewCount);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        foreach (var pt in pos) Gizmos.DrawWireCube(pt, Vector3.one * 0.4f);
    }

    Transform FindPartyTransformForPreview()
    {
        // 단순 미리보기용: 씬에서 "Party" 태그 오브젝트가 있으면 사용
        var go = GameObject.FindWithTag(partyTag);
        return go ? go.transform : null;
    }
#endif
}
