// BattleManager.cs — 영웅+몬스터 통합 SPD 정렬版
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

// 전투 스크립트
public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }   // 클릭 리스너가 접근
    void Awake() { Instance = this; }

    [Header("Party Info")]
    private IReadOnlyList<Job> party;

    // === 통합 유닛 엔트리 ===
    private class UnitEntry
    {
        public bool isHero;
        public Job hero;                 // isHero==true
        public GameObject go;            // isHero==false일 때 표시/추적용
        public Combatant combatant;      // 적(또는 영웅)에 붙은 Combatant(있으면)
        public int spd;                  // 정렬용 속도 (영웅=job.spd, 몬스터=monsterData.spd 또는 폴백)
        public string label;             // 디버그 출력용 이름
        public bool IsAlive =>
            isHero ? hero != null /*추후 HP 검사로 대체*/ :
            (combatant != null ? combatant.IsAlive : (go != null));
    }

    // 턴 순서 리스트
    private List<UnitEntry> initiative = new();
    private int turnIndex = -1;

    [Header("Enemy")]
    [SerializeField] private EnemyCatalog enemyCatalog;  // 에디터에서 Catalog 드롭
    [SerializeField] private bool useCatalogSlots = true; // 켜면 Monster Slots로 자동 주입

    [Header("UI Control")]
    [SerializeField] private UIManager uiManager;

    // 스킬 사용이 실제로 커밋된 시점(대상에게 적용된 뒤) 알림
    public event Action OnSkillCommitted;
    // 대상 선택 이벤트
    public event Action<bool> OnTargetingStateChanged;  // true=시작, false=종료

    [Header("Debug")]
    public bool minimalCombatLog = true;  // ✅ 간단 로그 on/off

    private HeroSkills heroSkills = new HeroSkills();

    // === [대상 지정 상태] ===
    private Skill _pendingSkill = null;
    public Skill PendingSkill => _pendingSkill;
    private Combatant _pendingCaster = null;
    public Combatant PendingCaster => _pendingCaster;
    private int _pendingSkillKey;
    int GetSkillKey(Skill s) => s != null ? s.skillId : -1;   // ← ID 기반 키
    private bool _isTargeting = false;
    public bool IsTargeting => _isTargeting;

    void OnEnable()
    {
        EnemySpawner.OnBattleStart += HandleBattleStart;
    }

    void OnDisable()
    {
        EnemySpawner.OnBattleStart -= HandleBattleStart;
    }

    private void HandleBattleStart(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        party = heroes;
        BeginBattle(party, enemies);
    }

    /// <summary>
    /// 영웅/몬스터를 모두 수집하여 SPD 기반 이니셔티브를 만든다.
    /// </summary>
    public void BeginBattle(IEnumerable<Job> overrideParty = null, IReadOnlyList<GameObject> enemies = null)
    {
        // --- Party 태그 루트에 선택 영웅 자동 바인딩 ---
        if (party != null && party.Count > 0)
        {
            var partyGo = GameObject.FindGameObjectWithTag("Party");
            if (partyGo != null)
            {
                var root = partyGo.transform;
                int n = Mathf.Min(root.childCount, party.Count);
                for (int i = 0; i < n; i++)
                {
                    var child = root.GetChild(i)?.gameObject;
                    if (!child) continue;

                    var c = child.GetComponent<Combatant>();
                    if (c == null) c = child.AddComponent<Combatant>();

                    // 상위 태그로 진영 자동 판단 + 해당 영웅 주입
                    c.AutoInitByHierarchy(heroCandidate: party[i]);
                }
            }
        }

        if (overrideParty != null) party = overrideParty.Where(hero => hero != null).ToList();

        BuildInitiative(party, enemies);

        turnIndex = -1;

        // 턴 순서 디버그 출력
        if (initiative.Count > 0)
        {
            var order = string.Join(" → ",
                initiative.Select((u, i) => $"{i + 1}. {u.label} (SPD {u.spd})"));
            Debug.Log($"[INIT] 턴 순서: {order}");
        }
        else
        {
            Debug.LogWarning("[INIT] 유닛이 없습니다.");
        }

        NextTurn();
    }

    /// <summary>
    /// SPD 내림차순으로 모든 유닛(영웅+몬스터)을 정렬한다.
    /// </summary>
    private void BuildInitiative(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        var list = new List<UnitEntry>();

        // 1) 영웅 수집
        if (heroes != null)
        {
            foreach (var h in heroes.Where(h => h != null))
            {
                list.Add(new UnitEntry
                {
                    isHero = true,
                    hero = h,
                    spd = h.spd,
                    label = $"H:{h.name_job}"
                });
            }
        }

        // 2) 몬스터 수집 (스폰된 GameObject 트리에서 Combatant 자동 확보)
        if (enemies != null)
        {
            foreach (var root in enemies.Where(e => e != null))
            {
                // 2-1) 자식들 포함 Combatant 수집
                var comps = root.GetComponentsInChildren<Combatant>(true);

                // 2-2) 없다면 루트에 하나 붙여서라도 전투 단위 확보
                if (comps == null || comps.Length == 0)
                {
                    var c0 = root.GetComponent<Combatant>() ?? root.AddComponent<Combatant>();
                    c0.AutoInitByHierarchy(); // 상위 태그가 Party가 아니므로 Enemy로 초기화
                    comps = new[] { c0 };
                }
                else
                {
                    // 이미 붙어 있으나 미초기화일 수 있으므로 보증 초기화
                    foreach (var c in comps)
                        c.AutoInitByHierarchy(); // monsterData가 연결돼 있으면 그걸로, 아니면 폴백
                }

                // 2-3) 이니셔티브 등록 (표준화: Combatant 값을 신뢰)
                foreach (var c in comps)
                {
                    list.Add(new UnitEntry
                    {
                        isHero = false,
                        go = c.gameObject,
                        combatant = c,
                        spd = c.EffectiveSpeed,
                        label = $"E:{c.DisplayName}"
                    });
                }
            }
        }


        // 3) SPD 정렬 (+ 안정적 타이브레이커)
        initiative = list
            .OrderByDescending(u => u.spd)        // 속도 우선
            .ThenBy(u => u.isHero ? 0 : 1)        // 동률 시 영웅 우선(옵션)
            .ThenBy(u => u.label)                 // 최종 안정화
            .ToList();
    }

    // 턴 진행
    private void NextTurn()
    {
        SkillTargetHighlighter.Instance?.ClearAll();    // 아웃라인 제거
        uiManager?.ClearSkillSelection();

        if (initiative == null || initiative.Count == 0)
        {
            Debug.LogWarning("[Battle] No units in initiative.");
            return;
        }

        // 사망/이탈 정리
        initiative = initiative.Where(u => u != null && u.IsAlive).ToList();

        // 도트(출혈, 중독) 피해 사망 시 전투 종료
        TryEndBattleIfEnemiesDefeated();
        if (initiative.Count == 0) return;

        turnIndex = (turnIndex + 1) % initiative.Count; // 턴 무한 순회
        var actor = initiative[turnIndex];

        // 디버그 출력
        Debug.Log($"[Turn] {actor.label}, SPD: {actor.spd}");

        if (actor.isHero)
        {
            // 영웅: 스킬 UI 표시
            var hero = actor.hero;
            if (hero == null)
            {
                Debug.LogWarning("[Battle] Hero is null on turn.");
                NextTurn(); // 건너뛰기
                return;
            }

            ShowHeroSkill(hero);
        }
        else
        {
            TryUseSkill_EnemyAI_Random(actor);
        }
    }

    // 스킬 표시 (영웅 턴 전용)
    private void ShowHeroSkill(Job hero)
    {
        var c = Combatant.FindByHero(hero);
        var skills = (c ? c.GetSkills(heroSkills) : heroSkills.GetHeroSkills(hero)).ToList();

        if (skills.Count == 0)
        {
            Debug.LogError($"[SkillUI] {hero.name_job} has no skills");
            return;
        }

        uiManager.ShowSkills(hero, skills);     // UI 출력
        uiManager.ShowHeroInfo(hero);
    }


    // =================== 스킬 사용 =================
    // 스킬 클릭 시 반응
    public void OnSkillClickedFromUI(Skill skill)
    {
        Debug.Log($"[UI→BM] OnSkillClickedFromUI: {skill?.skillName}");

        var actor = initiative[turnIndex]; // 현재 턴
        Combatant actingC =
            actor.isHero
            ? FindObjectsOfType<Combatant>(true).FirstOrDefault(c => c.hero == actor.hero)
            : actor.combatant;
        if (!actingC) { Debug.LogWarning("[BM] acting Combatant not found"); return; }

        int clickedKey = GetSkillKey(skill);

        // 같은 스킬을 다시 클릭 → 타게팅 해제(토글)
        if (_isTargeting && _pendingCaster == actingC && _pendingSkillKey == clickedKey)
        {
            CancelTargeting();
            return;
        }

        EnterTargeting(actingC, skill);
    }

    // 스킬 대상 선택
    private void EnterTargeting(Combatant caster, Skill s)
    {
        _pendingCaster = caster;
        _pendingSkill = s;
        _pendingSkillKey = GetSkillKey(s);
        _isTargeting = true;

        SkillTargetHighlighter.Instance?.ClearAll();                    // 이전 아웃라인 제거
        SkillTargetHighlighter.Instance?.HighlightForSkill(caster, s);

        OnTargetingStateChanged?.Invoke(true);  // 대상 선택 시작

        Debug.Log($"[Targeting] {_pendingCaster.DisplayName} → {_pendingSkill.skillName} 대상 클릭 대기");
    }

    // 스킬 취소
    public void CancelTargeting()
    {
        // 이미 꺼져 있으면 아무 것도 하지 않음(이벤트도 안 쏨)
        if (!_isTargeting && _pendingCaster == null && _pendingSkill == null)
            return;

        _pendingCaster = null;
        _pendingSkill = null;
        _pendingSkillKey = -1;

        if (_isTargeting)
        {
            _isTargeting = false;
            SkillTargetHighlighter.Instance?.ClearAll();
            OnTargetingStateChanged?.Invoke(false);  // 여기서 단 1번만 알림
        }
        else
        {
            // 이미 꺼진 상태라면 하이라이트만 안전하게 정리하고 끝
            SkillTargetHighlighter.Instance?.ClearAll();
        }

        Debug.Log("[Targeting] 취소");
    }

    // 스킬 사용 대상 확정
    public void NotifyCombatantClicked(Combatant clicked)
    {
        if (!_isTargeting || _pendingCaster == null || _pendingSkill == null) return;
        if (clicked == null || !clicked.IsAlive) return;

        if (!SkillTargeting.IsCandidate(_pendingCaster, clicked, _pendingSkill))
        {
            Debug.Log("[BM] Invalid target for current skill");
            return;
        }

        var all = FindObjectsOfType<Combatant>(includeInactive: false)
              .Where(c => c != null && c.IsAlive);

        var targets = SkillTargeting.GetExecutionTargets(_pendingCaster, clicked, all, _pendingSkill);
        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[BM] No resolved targets");
            return;
        }

        CastSkillResolved(_pendingCaster, _pendingSkill, targets);

        RaiseSkillCommitted();
        CancelTargeting();
        NextTurn();
    }

    void RaiseSkillCommitted()
    {
        try { OnSkillCommitted?.Invoke(); }
        catch (Exception e) { Debug.LogException(e, this); }
    }

    // 사망 유닛 오브젝트 제거
    private void DestroyDeadUnit()
    {
        // 현재 씬의 Combatant 중 죽은 오브젝트가 아직 살아있다면 정리
        var all = FindObjectsOfType<Combatant>(true);
        foreach (var c in all)
        {
            if (c == null) continue;
            if (!c.IsAlive && c.gameObject) // 혹시 남아있다면 안전하게 제거
            {
                Destroy(c.gameObject);
            }
        }

        // 2) 이니셔티브에서도 제거 (턴 순서 리스트)
        if (initiative != null)
            initiative = initiative.Where(u => u != null && u.IsAlive).ToList();

        // 전멸 확인 => 전투 종료
        TryEndBattleIfEnemiesDefeated();
    }

    // ========== 공통 유틸 ==========
    // 시전자 위치 조건(Loc) 만족?
    bool IsCastableFromLoc(Combatant caster, Skill s)
        => s.loc == Loc.None || s.loc == caster.currentLoc;

    // 시전자 기준 대상 풀
    List<Combatant> GatherByTarget(Combatant caster, Target target)
    {
        var all = FindObjectsOfType<Combatant>(true)
                    .Where(x => x && x.IsAlive).ToList();

        return target switch
        {
            Target.Self => new List<Combatant> { caster },
            Target.Ally => all.Where(x => x.side == caster.side && x != caster).ToList(),
            Target.Enemy => all.Where(x => x.side != caster.side).ToList(),
            _ => new List<Combatant>()
        };
    }

    // Area에 따라 최종 대상 확장
    List<Combatant> ExpandByArea(Area area, Combatant seed, List<Combatant> pool)
    {
        if (seed == null) return new List<Combatant>();

        return area switch
        {
            Area.Single => new List<Combatant> { seed },
            Area.Row => pool.Where(t => t.RowIndex == seed.RowIndex).ToList(), // 같은 행(전열/후열)
            Area.Entire => pool.ToList(),
            _ => new List<Combatant> { seed }
        };
    }

    // 공통 스킬 실행 파이프라인
    void ExecuteSkill(Combatant caster, Skill s, Combatant seedTarget = null)
    {
        if (!caster || s == null) return;
        if (!IsCastableFromLoc(caster, s))
        {
            Debug.Log($"[Skill] {caster.DisplayName}는 현재 위치({caster.currentLoc})에서 {s.skillName} 사용 불가(요구 {s.loc})");
            return;
        }

        // 1) 대상 풀 수집
        var pool = GatherByTarget(caster, s.target);

        // 2) 시드 대상(없으면 무작위)
        if (seedTarget == null)
        {
            if (pool.Count == 0) { Debug.Log("[Skill] 대상 없음"); return; }
            seedTarget = pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        // 3) Area에 따른 확장
        var finalTargets = ExpandByArea(s.area, seedTarget, pool);
        if (finalTargets.Count == 0) { Debug.Log("[Skill] 최종 대상 없음"); return; }

        // 4) 이펙트 적용
        foreach (var t in finalTargets)
            foreach (var eff in s.effects)
                eff?.Apply(caster, t);

        DestroyDeadUnit();
    }

    void CastSkillResolved(Combatant caster, Skill skill, List<Combatant> finalTargets)
    {
        if (!caster || skill == null) return;

        if (finalTargets == null || finalTargets.Count == 0)
        {
            Debug.Log("[Skill] 최종 대상 없음");
            return;
        }

        foreach (var t in finalTargets)
            foreach (var eff in skill.effects)
                eff?.Apply(caster, t);

        DestroyDeadUnit();
    }

    // 몬스터 생존 확인
    bool AnyEnemiesAlive()
    {
        var all = FindObjectsOfType<Combatant>(true);
        foreach (var c in all)
            if (c && c.side == Side.Enemy && c.IsAlive)
                return true;
        return false;
    }

    // 몬스터 전멸 시 호출
    void TryEndBattleIfEnemiesDefeated()
    {
        if (AnyEnemiesAlive()) return;
        EndBattle_Victory();
    }

    // 전투 승리 후 전투 패널 초기화 / 이동 UI 전환
    void EndBattle_Victory()
    {
        // 1) 타게팅/하이라이트/UI 정리
        SkillTargetHighlighter.Instance?.ClearAll();
        CancelTargeting();                   // 대상 지정 상태 강제 종료
        uiManager?.ClearSkillSelection();
        uiManager?.CloseAll();               // UIManager에 있는 통합 닫기

        // 2) 내부 턴 상태 초기화
        initiative?.Clear();
        turnIndex = -1;

        // 3) 던전 UI 복구(중앙집중)
        if (DungeonManager.instance)
            DungeonManager.instance.ShowDungeonUIAfterBattle();     // UI 복구 및 보상 UI

        Debug.Log("[Battle] Victory → 전투 종료 및 던전 이동 UI 복구");
    }

    // ============ 몬스터 AI ===========
    void TryUseSkill_EnemyAI_Random(UnitEntry actor)
    {
        var caster = actor.combatant;
        if (!caster || !caster.IsAlive) { NextTurn(); return; }

        // 1) 사용 가능한 스킬 수집(위치 조건 충족 + 대상 존재)
        var skills = caster.GetSkills(heroSkills)?.ToList();
        if (skills == null || skills.Count == 0) { NextTurn(); return; }

        var all = FindObjectsOfType<Combatant>(includeInactive: false)
                  .Where(x => x && x.IsAlive).ToList();

        // 사용 가능 스킬 필터 (시전지 위치 제약)
        var usable = new List<Skill>();
        foreach (var s in skills)
        {
            if (s.loc != Loc.None && caster.currentLoc != s.loc) continue;

            // 후보가 1명 이상인지 간단히 확인: 적/아군 풀에서 IsCandidate로 한 번이라도 true가 있으면 OK
            bool anyCandidate = all.Any(c => SkillTargeting.IsCandidate(caster, c, s));
            if (anyCandidate) usable.Add(s);
        }
        if (usable.Count == 0) { NextTurn(); return; }

        // 2) 완전 무작위 선택
        var pick = usable[UnityEngine.Random.Range(0, usable.Count)];

        // 클릭 시뮬레이션
        var candidates = all.Where(c => SkillTargeting.IsCandidate(caster, c, pick)).ToList();
        if (candidates.Count == 0) { NextTurn(); return; }
        var click = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        // 4) 최종 실행 대상 확정
        var targets = SkillTargeting.GetExecutionTargets(caster, click, all, pick);
        if (targets.Count == 0) { NextTurn(); return; }

        Debug.Log($"[AI] {caster.DisplayName} uses {pick.skillName} → {targets.Count} target(s)");
        // 5) 실행
        CastSkillResolved(caster, pick, targets);

        // 6) 다음 턴
        NextTurn();
    }
}
