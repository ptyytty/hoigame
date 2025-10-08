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
        public bool IsAlive => combatant != null && combatant.IsAlive;
    }

    // 턴 순서 리스트
    private List<UnitEntry> initiative = new();
    private int turnIndex = -1;

    // ==== 프레임 중복 실행 차단 ====
    private int _lastTurnFrame = -1;
    private bool _nextTurnScheduled = false;
    private int _pendingTurnRequests = 0;           // 대기 중 턴 요청 수
    private UnitEntry _lastActorRef = null;

    [Header("Enemy")]
    [SerializeField] private EnemyCatalog enemyCatalog;  // 에디터에서 Catalog 드롭
    [SerializeField] private bool useCatalogSlots = true; // 켜면 Monster Slots로 자동 주입

    [Header("UI Control")]
    [SerializeField] private UIManager uiManager;

    [Header("Dot Damage")]
    [SerializeField] private float BLEED_RATE = 0.05f;
    [SerializeField] private float POISON_RATE = 0.07f;
    [SerializeField] private int BURN_FIXED = 5;

    private Combatant _tauntHeroSide = null;
    private Combatant _tauntEnemySide = null;

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

    // 도트 데미지 임시 테이블
    public static readonly Dictionary<BuffType, int> DOT_AT_Start = new()
    {
        { BuffType.Bleeding, 2 }
    };
    public static readonly Dictionary<BuffType, int> DOT_AT_END = new(){
        { BuffType.Poison,   3 },
        { BuffType.Burn,     4 },
    };

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
    /// SPD 순으로 턴 정렬 (파티, 몬스터)
    /// </summary>
    private void BuildInitiative(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        var list = new List<UnitEntry>();

        // 1) 영웅 수집
        if (heroes != null)
        {
            foreach (var h in heroes.Where(h => h != null))
            {
                var ch = Combatant.FindByHero(h);
                if (!ch) continue;

                list.Add(new UnitEntry
                {
                    isHero = true,
                    hero = h,
                    combatant = ch,
                    go = ch.gameObject,
                    spd = ch.EffectiveSpeed,    // Combatant 기준 속도
                    label = $"H:{ch.DisplayName}"
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

    // 턴 진행 1 => 프레임 중복 금지 + 다음 프레임으로 미루기
    // NextTurn은 여러 곳에서 호출되기 때문에 같은 프레임에서 최대 1회 호출 필요 (안하면 무한 호출됨)
    private void NextTurn()
    {
        // 여러 곳에서 NextTurn()이 연속 호출돼도 최소 1회는 실행되도록 병합
        _pendingTurnRequests++;

        // 다음 프레임 호출 예정 시 중복 X
        if (_nextTurnScheduled) return;

        // 다음 프레임으로 딜레이(같은 프레임 재귀 루프 근원 차단)
        _nextTurnScheduled = true;
        StartCoroutine(NextTurnDeferred());
    }

    // 턴 진행 2 => 턴 진행 상세 내용
    private void NextTurnCore()
    {
        SkillTargetHighlighter.Instance?.ClearAll();    // 아웃라인 제거
        uiManager?.ClearSkillSelection();

        if (initiative == null || initiative.Count == 0)
        {
            Debug.LogWarning("[Battle] No units in initiative.");
            return;
        }

        // 1) 정리 전, 직전 배우(anchor) 참조 확보
        UnitEntry anchor = _lastActorRef;
        Combatant anchorC = anchor != null ? anchor.combatant : null;

        // 2) 사망/이탈 정리 (여기 '한 곳'에서만 수행)
        initiative = initiative.Where(u => u != null && u.IsAlive && u.combatant).ToList();

        // 3) 전멸/빈 리스트 처리
        TryEndBattleIfEnemiesDefeated();
        if (initiative.Count == 0) return;

        // 4) anchor의 '새 인덱스' 찾기 (오브젝트가 같으면 같은 참조)
        int baseIdx = -1;
        if (anchorC != null)
        {
            baseIdx = initiative.FindIndex(u => u.combatant == anchorC);
        }

        // 5) 다음 배우로 이동 (anchor 뒤로 한 칸)
        //    - anchor가 사라졌거나 첫 턴이면 baseIdx == -1 → 0부터 시작하도록 처리
        turnIndex = ((baseIdx < 0 ? -1 : baseIdx) + 1) % initiative.Count;
        var actor = initiative[turnIndex];

        if (actor.combatant == null || !actor.combatant.IsAlive)
        {
            Debug.Log("[TurnSkip] 사망/무효 유닛 건너뜀");
            NextTurn();
            return;
        }

        // ★ 여기서 '이번 턴 배우'를 다음 루프의 anchor로 기록
        _lastActorRef = actor;

        ClearTauntIfOwnerTurnStarts(actor.combatant);       // actor가 도발 유닛일 경우 해제

        // 턴 시작 시 상태 처리
        if (!ApplyStartOfTurnStatues(actor.combatant))
        {
            NextTurn();         // 죽거나 기절이 -> 다음 턴
            return;
        }

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
            OnSkillCommitted += EndTickOnce;
        }
        else
        {
            TryUseSkill_EnemyAI_Random(actor);
            ApplyEndOfTurnTick(actor.combatant);
        }
    }

    // 턴 진행 3 => 딜레이 코루틴
    private System.Collections.IEnumerator NextTurnDeferred()
    {
        // 다음 프레임로 미뤄 모든 동시 호출을 코얼레싱
        yield return null;

        _nextTurnScheduled = false;

        // 누적 요청 1회만 처리(과도한 중복 방지)
        if (_pendingTurnRequests > 0)
        {
            _pendingTurnRequests = 0;
            NextTurnCore();
        }
    }

    // 스킬 사용 커밋 => 턴 종료 틱 1회 적용
    private void EndTickOnce()
    {
        OnSkillCommitted -= EndTickOnce;
        if (initiative == null || initiative.Count == 0) return;
        if (turnIndex < 0 || turnIndex >= initiative.Count) return;

        var actor = initiative.ElementAtOrDefault(turnIndex);
        if (actor == null || actor.combatant == null) return;

        ApplyEndOfTurnTick(actor.combatant);
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
        if (initiative == null || initiative.Count == 0) return;

        try { OnSkillCommitted?.Invoke(); }
        catch (Exception e) { Debug.LogException(e, this); }
    }

    // 턴 시작 시 버프 상태 (도트, 지속 턴 등)
    private bool ApplyStartOfTurnStatues(Combatant actor)
    {
        if (!actor || !actor.IsAlive) return false;

        int dotSum = 0;
        if (actor.HasDebuff(BuffType.Bleeding))
        {
            int lost = Mathf.Max(0, actor.maxHp - actor.currentHp);
            int bleedDmg = PercentOf(lost, BLEED_RATE);
            Debug.Log($"[DOT/Bleed@Start] {actor.DisplayName}: lost={lost}, rate={BLEED_RATE}, dmg={bleedDmg}");
            dotSum += bleedDmg;
        }

        if (dotSum > 0)
        {
            Debug.Log($"[DOT] {actor.DisplayName} -{dotSum} (Poison/Bleed/Burn)", actor);
            actor.ApplyDamage(dotSum);
            if (!actor.IsAlive) return false;   // 도트로 사망 -> 행동 불가
        }

        if (actor.HasDebuff(BuffType.Faint))
        {
            Debug.Log($"[CC] {actor.DisplayName} 기절로 턴 스킵", actor);
            actor.TickStatuses();   // 턴 소모
            DestroyDeadUnit();      // 사망 시 정리
            return false;           // 행동 X
        }

        return true;
    }

    // 턴 종료 시 버프 상태
    private void ApplyEndOfTurnTick(Combatant actor)
    {
        if (!actor) return;

        int dotSum = 0;
        if (actor.HasDebuff(BuffType.Burn))
        {
            Debug.Log($"[DOT/Burn@End] {actor.DisplayName}: +{BURN_FIXED}");
            dotSum += BURN_FIXED;
        }
        if (actor.HasDebuff(BuffType.Poison))
        {
            int p = PercentOf(actor.currentHp, POISON_RATE);
            Debug.Log($"[DOT/Poison@End] {actor.DisplayName}: hpNow={actor.currentHp}, rate={POISON_RATE}, dmg={p}");
            dotSum += p;
        }

        if (dotSum > 0) actor.ApplyDamage(dotSum);

        actor.TickStatuses();
        DestroyDeadUnit();
    }

    // 체력 비례 데미지 계산
    private static int PercentOf(int hp, float rate)
    {
        var dmg = Mathf.FloorToInt(hp * rate);
        return Mathf.Max(1, dmg);
    }

    // ========================= 도발 기능 ======================
    // 도발 시작 (스킬 효과에서 호출)
    public void BeginTaunt(Combatant protector)
    {
        if (!protector || !protector.IsAlive) return;

        if (protector.side == Side.Hero) _tauntHeroSide = protector;
        else _tauntEnemySide = protector;

        protector.OnDied += ClearTauntOnDeath;

        Debug.Log($"[Taunt] {protector.DisplayName}가 도발 시작 (진영:{protector.side})");
    }

    // 도발 유닛 사망 시 호출
    private void ClearTauntOnDeath(Combatant dead)
    {
        if (dead == _tauntHeroSide) _tauntHeroSide = null;
        if (dead == _tauntEnemySide) _tauntEnemySide = null;
        if (dead != null) dead.OnDied -= ClearTauntOnDeath;
        Debug.Log($"[Taunt] {dead?.DisplayName} 사망으로 도발 해제");
    }

    // 턴 순환 후 도발 해제
    private void ClearTauntIfOwnerTurnStarts(Combatant actor)
    {
        if (!actor) return;
        if (actor == _tauntHeroSide) { _tauntHeroSide = null; actor.OnDied -= ClearTauntOnDeath; Debug.Log("[Taunt] 아군 도발 해제(턴 시작)"); }
        if (actor == _tauntEnemySide) { _tauntEnemySide = null; actor.OnDied -= ClearTauntOnDeath; Debug.Log("[Taunt] 적군 도발 해제(턴 시작)"); }
    }

    // 도발 기능 (적대적 스킬 효과 대상 재지정)
    private Combatant RedirectPerEffectIfTaunt(Combatant caster, SkillEffect eff, Combatant originalTarget)
    {
        if (caster == null || eff == null || originalTarget == null) return originalTarget;

        // (a) 캐스터와 대상이 같은 진영 → 우호 효과: 리다이렉트 금지
        if (caster.side == originalTarget.side) return originalTarget;

        // (b) 적대적 효과 확인
        if (!IsHostileEffect(eff)) return originalTarget;

        // (c) 도발 유닛으로 재지정
        var taunter = (originalTarget.side == Side.Hero) ? _tauntHeroSide : _tauntEnemySide;
        if (taunter != null && taunter.IsAlive && taunter != originalTarget)
        {
            Debug.Log($"[Taunt] {originalTarget.DisplayName} → {taunter.DisplayName} (효과:{eff.GetType().Name})");
            return taunter;
        }

        return originalTarget;
    }

    // 적대적 효과 구분
    private bool IsHostileEffect(SkillEffect eff)
    {
        // 타입 이름 기준
        if (eff is DamageEffect) return true;
        if (eff is DebuffEffect) return true;

        if (eff.GetType().Name.Contains("Damage", StringComparison.OrdinalIgnoreCase)) return true;        // 이름에 "Damage"가 들어간 효과는 전부 적대적 효과 처리

        // AbilityBuff(+/-)를 쓰고 있으니 값으로 판정
        if (eff is AbilityBuff ab) return ab.value < 0;

        if (eff is HealEffect) return false;
        // (있다면) HealEffect, Pure BuffEffect 등은 우호적으로 처리
        // 필요시 추가: if (eff is HealEffect) return false; ...

        return false; // 기본은 Friendly 취급
    }

    // 6) 한 스킬이 중복 적용되는 것 방지
    private void ApplySkillEffectsWithTaunt(Combatant caster, Skill skill, List<Combatant> finalTargets)
    {
        if (caster == null || skill == null || finalTargets == null || finalTargets.Count == 0) return;

        foreach (var eff in skill.effects)
        {
            Debug.Log($"[Skill/Apply] {caster.DisplayName} uses '{skill.skillName}' → Effect={eff.GetType().Name}, Targets={finalTargets.Count}");

            var appliedOnce = new HashSet<Combatant>(); // 같은 이펙트가 같은 대상에 여러 번 들어가는 것 방지
            foreach (var tgt in finalTargets)
            {
                if (tgt == null || !tgt.IsAlive) continue;

                var realTarget = RedirectPerEffectIfTaunt(caster, eff, tgt);
                if (!appliedOnce.Add(realTarget))
                {
                    Debug.Log($"[Skill/SkipDuplicate] Effect={eff.GetType().Name} already applied to {realTarget.DisplayName}");
                    continue; // 같은 이펙트가 같은 타깃에 중복 적용되는 것 차단
                }

                Debug.Log($"[Skill/Effect→Target] {eff.GetType().Name} → {realTarget.DisplayName}");
                eff?.Apply(caster, realTarget);
            }
        }
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

    // 스킬 사용
    void CastSkillResolved(Combatant caster, Skill skill, List<Combatant> finalTargets)
    {
        if (!caster || skill == null) return;

        if (finalTargets == null || finalTargets.Count == 0)
        {
            Debug.Log("[Skill] 최종 대상 없음");
            return;
        }

        ApplySkillEffectsWithTaunt(caster, skill, finalTargets);
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

        OnSkillCommitted = null;

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

        if (caster.HasDebuff(BuffType.Faint))
        {
            Debug.Log($"[AI] {caster.DisplayName} 기절로 행동 불가");
            caster.TickStatuses();       // 턴 소모
            DestroyDeadUnit();
            NextTurn();
            return;
        }

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
