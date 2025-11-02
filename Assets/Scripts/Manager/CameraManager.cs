using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineVirtualCamera dungeonCam;       // 기본 카메라
    [SerializeField] private CinemachineVirtualCamera vcamEnemy;
    [SerializeField] private CinemachineVirtualCamera vcamParty;

    [Header("Enemy Target Group (your 'Camera Control' object)")]
    [SerializeField] private CinemachineTargetGroup enemyGroup;        // 'Camera Control' 오브젝트에 달린 컴포넌트 할당
    [SerializeField] private CinemachineTargetGroup heroGroup;

    [Header("Priorities")]
    [SerializeField] private int basePriority = 10;   // 베이스 기본
    [SerializeField] private int focusPriority = 12;    // 포커스 시 값

    [Header("Framing (Group Framing 미사용 시만 ON)")]
    [Tooltip("VCam EnemyFocus에서 Group Framing을 쓰지 않는다면 true로 두고 자동 오쏘사이즈 계산 사용")]
    [SerializeField] private bool autoComputeOrthoSize = true;
    [SerializeField] private bool applyConstantWidth = true;
    [SerializeField] private float referenceAspect = 16f / 9f; // 1.777...

    [SerializeField, Tooltip("화면 여유 비율(1.1~1.3)")] private float padding = 1.2f;
    [SerializeField] private float minEnemySize = 8f;
    [SerializeField] private float maxEnemySize = 18f;
    [SerializeField] private float minSizeHero = 8f;
    [SerializeField] private float maxSizeHero = 18f;

    [Header("Base Lens")]
    [SerializeField] private float baseOrthoSize = 16f;

    [Header("Stability (Enemy/Party 포커스가 엉뚱한 곳 보는 문제 방지)")]
    [Tooltip("포커스 카메라의 Z(깊이)를 dungeonCam과 동일하게 고정합니다.")]
    [SerializeField] private bool lockFocusZToBase = true;
    [Tooltip("필요 시 포커스 카메라에 소량 Z 오프셋을 더합니다.")]
    [SerializeField] private float focusZOffset = 0f;
    [Tooltip("포커스 카메라의 회전을 dungeonCam과 동일하게 맞춥니다.")]
    [SerializeField] private bool matchFocusRotationToBase = true;

    [Header("Dungeon Follow Move")]
    public float speed = 3f;

    private bool _suppressBaseSizeOnce = false;         // 전투 확대
    private Vector3 _enemyHintCenter;
    private Quaternion _enemyHintRotation = Quaternion.identity;
    private float _enemyHintOrtho = -1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dungeonCam)
        {
            dungeonCam.Priority = basePriority;
            SetOrthoSize(dungeonCam, baseOrthoSize);
        }
        if (vcamEnemy)
        {
            vcamEnemy.Priority = 5; // 비활성 수준
            // Group Framing을 쓴다면 인스펙터에서 Body=Framing Transposer, Follow/LookAt=enemyGroup 설정
        }
        if (vcamParty)
        {
            vcamParty.Priority = 5; // 비활성 수준
            EnsureOrthographic(vcamParty, true);
        }
    }

    void OnEnable()
    {
        // 전투 시작 시 스포너가 heroes/enemies를 넘겨줌
        EnemySpawner.OnBattleStart += HandleBattleStart;
        EnemySpawner.OnEnemyFocusHint += HandleEnemyFocusHint;

        TrySubscribeBattleEvents();
        StartCoroutine(SubscribeBMDeferred());      // 싱글톤 초기화 늦을 시 1프레임 뒤 재시도
    }

    IEnumerator SubscribeBMDeferred()
    {
        if (BattleManager.Instance != null) yield break;
        yield return null; // 다음 프레임
        TrySubscribeBattleEvents();
    }

    void TrySubscribeBattleEvents()
    {
        var bm = BattleManager.Instance;
        if (bm == null) return;
        // 중복 구독 방지하려면 일단 한번 해제 후 구독
        bm.OnTargetingStateChanged -= HandleTargetingState;
        bm.OnSkillCommitted -= HandleSkillCommitted;
        bm.OnTargetingStateChanged += HandleTargetingState;
        bm.OnSkillCommitted += HandleSkillCommitted;
    }

    void OnDisable()
    {
        EnemySpawner.OnBattleStart -= HandleBattleStart;
        EnemySpawner.OnEnemyFocusHint -= HandleEnemyFocusHint;

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnTargetingStateChanged -= HandleTargetingState;
            BattleManager.Instance.OnSkillCommitted -= HandleSkillCommitted;
        }
    }

    void LateUpdate()
    {
        if (DungeonManager.instance.partyTransform == null) return;

        float zoffset = 10f;    // 기본 카메라 위치 값


        if (DungeonManager.instance.currentDir == MoveDirection.Left)
        {
            zoffset = 0f;
        }
        else if (DungeonManager.instance.currentDir == MoveDirection.Right)
        {
            zoffset = 45f;
        }

        float targetZ = Mathf.Lerp(transform.position.z, DungeonManager.instance.partyTransform.position.z
                                         + zoffset, Time.deltaTime * speed);
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
    }

    // ===== EnemySpawner → 전투 시작 =====
    private void HandleBattleStart(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        BuildEnemyGroup(enemies);
        BuildHeroGroupAuto();

        // 전투 시작 시 힌트 초기화
        _enemyHintOrtho = -1f;
        _suppressBaseSizeOnce = true;
        ReturnToBase();
    }

    // ===== EnemySpawner → 적 포커스 힌트 수신 =====
    private void HandleEnemyFocusHint(Vector3 center, Quaternion rot, float ortho)
    {
        _enemyHintCenter = center;
        _enemyHintRotation = rot;
        _enemyHintOrtho = ortho;
    }

    // ===== 타게팅 on/off =====
    private void HandleTargetingState(bool on)
    {
        if (!on) { ReturnToBase(); return; }

        var bm = BattleManager.Instance;
        var skill = bm?.PendingSkill;
        var caster = bm?.PendingCaster;
        if (skill == null) { ReturnToBase(); return; }

        switch (skill.target)
        {
            case Target.Enemy:
                RaiseEnemyFocus(); // ★ 여기서 힌트 우선 적용
                break;

            case Target.Ally:
            case Target.Self:
                RaisePartyFocus(caster?.side ?? Side.Hero);
                break;

            default:
                ReturnToBase();
                break;
        }
    }

    // ===== 스킬 커밋 후: 그룹 최신화 + 베이스 복귀 =====
    private void HandleSkillCommitted()
    {
        var aliveEnemies = FindObjectsOfType<Combatant>(true)
            .Where(c => c && c.IsAlive && c.side == Side.Enemy)
            .Select(c => c.gameObject)
            .ToList();
        BuildEnemyGroup(aliveEnemies);
        BuildHeroGroupAuto();

        ReturnToBase();
    }

    // ===================== 그룹 구성 =====================

    private void BuildEnemyGroup(IReadOnlyList<GameObject> enemies)
    {
        if (!enemyGroup) return;

        var targets = new List<CinemachineTargetGroup.Target>();
        if (enemies != null)
        {
            foreach (var go in enemies)
            {
                if (!go) continue;
                targets.Add(new CinemachineTargetGroup.Target
                {
                    target = go.transform,
                    weight = 1f,
                    radius = 0.5f
                });
            }
        }
        enemyGroup.m_Targets = targets.ToArray();

        if (vcamEnemy)
        {
            vcamEnemy.Follow = enemyGroup ? enemyGroup.transform : null;
            vcamEnemy.LookAt = enemyGroup ? enemyGroup.transform : null;
        }
    }

    private void BuildHeroGroupAuto()
    {
        if (heroGroup == null) return;

        var heroes = FindObjectsOfType<Combatant>(true)
            .Where(c => c && c.IsAlive && c.side == Side.Hero)
            .Select(c => c.transform)
            .ToList();

        var targets = new List<CinemachineTargetGroup.Target>();
        foreach (var t in heroes)
        {
            targets.Add(new CinemachineTargetGroup.Target
            {
                target = t,
                weight = 1f,
                radius = 0.5f
            });
        }
        heroGroup.m_Targets = targets.ToArray();

        if (vcamParty)
        {
            vcamParty.Follow = heroGroup ? heroGroup.transform : null;
            vcamParty.LookAt = heroGroup ? heroGroup.transform : null;
        }
    }

    // ===================== 포커스/복귀 =====================

    private void RaiseEnemyFocus()
    {
        if (!vcamEnemy) { ReturnToBase(); return; }

        // 1) 스포너 힌트가 있으면 "그대로" 적용 (가장 단순/견고)
        if (_enemyHintOrtho > 0f)
        {
            float z = vcamEnemy.transform.position.z;
            if (lockFocusZToBase && dungeonCam) z = dungeonCam.transform.position.z + focusZOffset;

            var rot = (matchFocusRotationToBase && dungeonCam)
                        ? dungeonCam.transform.rotation
                        : _enemyHintRotation;

            vcamEnemy.transform.SetPositionAndRotation(
                new Vector3(_enemyHintCenter.x, _enemyHintCenter.y, z),
                rot
            );
            SetOrthoSize(vcamEnemy, CW(Mathf.Clamp(_enemyHintOrtho, minEnemySize, maxEnemySize)));
        }
        else
        {
            // 2) 힌트가 없으면 폴백: 그룹/씬 스캔 기반 자동 보정
            List<Transform> members;
            if (enemyGroup && enemyGroup.m_Targets != null && enemyGroup.m_Targets.Length > 0)
            {
                members = enemyGroup.m_Targets
                    .Where(t => t.target != null && t.target.gameObject.activeInHierarchy)
                    .Select(t => t.target)
                    .ToList();
            }
            else
            {
                members = FindObjectsOfType<Combatant>(true)
                    .Where(c => c && c.IsAlive && c.side == Side.Enemy)
                    .Select(c => c.transform)
                    .ToList();
            }

            if (members.Count == 0) { ReturnToBase(); return; }

            // 렌더러 바운즈 기반으로 중심/사이즈 산출
            Bounds b = ComputeBoundsFromRenderers(members, true);
            float size = ComputeOrthoSizeByBounds(b, padding, minEnemySize, maxEnemySize);

            SnapFocusTransform(vcamEnemy, b.center);
            SetOrthoSize(vcamEnemy, CW(size));
        }

        Raise(vcamEnemy);
    }

    private void RaisePartyFocus(Side casterSide)
    {
        if (!vcamParty) { ReturnToBase(); return; }

        List<Transform> members;
        if (heroGroup && heroGroup.m_Targets != null && heroGroup.m_Targets.Length > 0)
        {
            members = heroGroup.m_Targets
                .Where(t => t.target != null && t.target.gameObject.activeInHierarchy)
                .Select(t => t.target)
                .ToList();
        }
        else
        {
            members = FindObjectsOfType<Combatant>(true)
                .Where(c => c && c.IsAlive && c.side == Side.Hero)
                .Select(c => c.transform)
                .ToList();
        }

        if (members.Count == 0) { ReturnToBase(); return; }

        Bounds b = ComputeBoundsFromRenderers(members, true);
        float size = ComputeOrthoSizeByBounds(b, padding, minSizeHero, maxSizeHero);

        SnapFocusTransform(vcamParty, b.center);
        SetOrthoSize(vcamParty, CW(size));

        Raise(vcamParty);
    }

    private void ReturnToBase()
    {
        LowerAllFocus();
        if (dungeonCam)
        {
            dungeonCam.Priority = focusPriority + 1;

            // 전투 입장 직후 OrthoSize 확대 X
            if (!_suppressBaseSizeOnce)
                SetOrthoSize(dungeonCam, baseOrthoSize);

            _suppressBaseSizeOnce = false;
        }
    }

    // ===================== 내부 헬퍼 =====================

    private void Raise(CinemachineVirtualCamera target)
    {
        if (!target) return;
        if (vcamEnemy) vcamEnemy.Priority = 5;
        if (vcamParty) vcamParty.Priority = 5;

        target.Priority = focusPriority;
        if (dungeonCam) dungeonCam.Priority = Mathf.Min(basePriority, focusPriority - 1);
    }

    private void LowerAllFocus()
    {
        if (vcamEnemy) vcamEnemy.Priority = 5;
        if (vcamParty) vcamParty.Priority = 5;
    }

    private void SnapFocusTransform(CinemachineVirtualCamera cam, Vector3 centerXY)
    {
        if (!cam) return;

        float z = cam.transform.position.z;
        if (lockFocusZToBase && dungeonCam) z = dungeonCam.transform.position.z + focusZOffset;

        cam.transform.position = new Vector3(centerXY.x, centerXY.y, z);

        if (matchFocusRotationToBase && dungeonCam)
            cam.transform.rotation = dungeonCam.transform.rotation;
        // 원하는 고정 회전이 있으면 아래 라인으로 강제 가능:
        // cam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
    }

    // ======== OrthoSize 자동 조정 =========
    // 3) Constant-Width 보정도 "유효 화면비" 기준으로
    //    - 레터박스(16:9) 사용 시 보정이 과해질 수 있으니, 레터박스를 쓰면 끄는 걸 권장합니다.
    //    - 쓰고 싶다면 아래처럼 유효 화면비 기준으로만 살짝 보정.
    private float CW(float size)
    {
        if (!applyConstantWidth) return size;
        float curAspect = GetEffectiveAspect(); // ← 여기만 교체
        return size * ((16f / 9f) / curAspect);
    }

    private static void SetOrthoSize(CinemachineVirtualCamera cam, float size)
    {
        if (!cam) return;
        var lens = cam.m_Lens;
        lens.OrthographicSize = size;
        cam.m_Lens = lens;
    }

    private static void EnsureOrthographic(CinemachineVirtualCamera cam, bool ortho)
    {
        if (!cam) return;
        var lens = cam.m_Lens;
        lens.Orthographic = ortho;
        cam.m_Lens = lens;
    }

    // ==== 렌더러 바운즈 기반 계산(스폰포인트/피벗 오차 방지) ====
    private static Bounds ComputeBoundsFromRenderers(List<Transform> members, bool expandFallback)
    {
        // 첫 유효 렌더러 찾기
        Renderer first = null;
        foreach (var t in members)
        {
            if (!t) continue;
            var r = t.GetComponentInChildren<Renderer>(true);
            if (r) { first = r; break; }
        }
        if (first == null)
        {
            // 렌더러가 없다면 트랜스폼 기반으로 폴백
            return ComputeBoundsByTransforms(members, expandFallback);
        }

        Bounds b = first.bounds;
        foreach (var t in members)
        {
            if (!t) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                b.Encapsulate(r.bounds);
        }
        if (expandFallback) b.Expand(new Vector3(2f, 2f, 0f));
        return b;
    }

    private static Bounds ComputeBoundsByTransforms(List<Transform> members, bool expandFallback)
    {
        Bounds b = new Bounds(members[0].position, Vector3.zero);
        for (int i = 1; i < members.Count; i++) b.Encapsulate(members[i].position);
        if (expandFallback) b.Expand(new Vector3(2f, 2f, 0f));
        return b;
    }

    // 1) "현재 카메라에 적용된 유효 화면비"를 가져오는 헬퍼
    //    - CameraLetterbox가 있으면 그 referenceAspect(=16:9)를 사용
    //    - 없으면 Screen 비율 사용 (폴백)
    private float GetEffectiveAspect()
    {
        var cam = Camera.main;
        if (cam)
        {
            var lb = cam.GetComponent<CameraLetterbox>();
            if (lb) return lb.referenceAspect;
        }
        return (Screen.height == 0) ? (16f / 9f) : (float)Screen.width / Screen.height;
    }

    // 2) Bounds → OrthoSize 계산에서 화면비를 "유효 화면비"로 교체
    private static float ComputeOrthoSizeByBounds(Bounds b, float pad, float min, float max)
    {
        // ※ 정적 메서드라면 Instance를 통해 aspect를 받도록 바꿔도 되고,
        //    간단히 16:9 고정으로 써도 무방(레터박스 사용 중이므로).
        float aspect = (CameraManager.Instance != null)
            ? CameraManager.Instance.GetEffectiveAspect()
            : 16f / 9f;

        float needed = Mathf.Max(b.extents.y, b.extents.x / aspect) * pad;
        return Mathf.Clamp(needed, min, max);
    }


}
