using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// 전투에 참여하는 모든 개체(영웅/몬스터)를 공통으로 다루기 위한 런타임 핸들
/// </summary>
// 아군 / 적 구분
public enum Side { Hero, Enemy }

public class Combatant : MonoBehaviour
{
    [Header("공통 속성")]
    public Side side;
    public int baseSpeed;       // 기본 SPD
    public int maxHp;           // 최대 체력
    public int currentHp;       // 현재 체력
    public bool IsAlive => !_dead && currentHp > 0;
    public event Action<Combatant> OnDied;
    public event Action<int, int> OnHpChanged;   // current, max
    private bool _dead = false;

    [Header("영웅 데이터")]
    public Job hero;           // side == Hero 일 때 연결
                               // Job 구조체 안에 spd, hp, name_job 등이 있음

    [Header("몬스터 데이터")]
    public MonsterData monsterData;   // side == Enemy 일 때 연결
    private readonly Dictionary<BuffType, int> _monsterBuffTurns = new();
    private readonly Dictionary<BuffType, int> _monsterDebuffTurns = new();
    public bool Marked => HasDebuff(BuffType.Sign);

    /// <summary>
    /// 턴 정렬에 사용하는 spd (버프/디버프 적용 시 수정 가능)
    /// </summary>
    public int EffectiveSpeed => GetCurrentSpeed();

    // --- 계산에 사용할 '기본값' 안전 접근 ---
    private int BaseDefense => (hero != null) ? hero.def : 0;
    private int BaseResistance => (hero != null) ? hero.res : 0;
    private int BaseHit => (hero != null) ? hero.hit : 0;

    // --- 계산 Getter ---
    public int GetCurrentDefense() => Mathf.Max(0, BaseDefense + SumAbility(BuffType.Defense));
    public int GetCurrentResistance() => Mathf.Max(0, BaseResistance + SumAbility(BuffType.Resistance));
    public int GetCurrentHit() => Mathf.Max(0, BaseHit + SumAbility(BuffType.Hit));

    /// <summary>
    /// Combatant의 UI 이름
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (side == Side.Hero && hero != null) return hero.name_job;
            if (side == Side.Enemy && monsterData != null)
            {
                if (!string.IsNullOrEmpty(monsterData.displayname))
                    return monsterData.displayname;
                return $"Enemy#{monsterData.id}";
            }
            return gameObject.name;
        }
    }

    // 현재 배치(전열/후열) – Row 판정에 사용
    public Loc currentLoc = Loc.Front;               // 기본 전열
    public int RowIndex => currentLoc == Loc.Back ? 1 : 0;

    // =============== 능력치 적용 =================
    [Serializable]
    public class AbilityModEntry    // 버프 적용 엔트리
    {
        public BuffType type;
        public int value;          // +/-
        public int remainingTurns; // 지속 턴
    }

    private readonly List<AbilityModEntry> _abilityMods = new();

    public void AddAbilityMod(BuffType type, int value, int duration)
    {
        _abilityMods.Add(new AbilityModEntry
        {
            type = type,
            value = value,
            remainingTurns = Mathf.Max(1, duration)
        });
    }

    // ======= 전투 세팅 =======

    /// <summary>
    /// 유닛이 사용할 수 있는 스킬 반환
    /// </summary>
    public IEnumerable<Skill> GetSkills(HeroSkills heroSkillsLib = null, bool preferCodeLib = true)
    {
        if (side == Side.Hero && hero != null)
        {
            if (heroSkillsLib != null)
                return heroSkillsLib.GetHeroSkills(hero);
        }
        else if (side == Side.Enemy)
        {
            // MonsterData가 들고 있는 고유 id를 키로 사용
            int id = (monsterData != null) ? monsterData.id : 0;

            // 코드 라이브러리 우선 사용
            var fromCode = MonsterSkill.GetMonsterSkill(id);
            if (fromCode != null && fromCode.Count > 0) return fromCode;

            // 폴백: MonsterData.assets에 들어있는 리스트
            if (monsterData != null && monsterData.skills != null && monsterData.skills.Count > 0)
                return monsterData.skills;
        }

        return Array.Empty<Skill>();
    }

    /// <summary>
    /// 영웅 Combatant 세팅
    /// </summary>
    public void InitHero(Job h)
    {
        side = Side.Hero;
        hero = h;
        baseSpeed = h.spd;
        maxHp = Mathf.Max(1, h.maxHp > 0 ? h.maxHp : h.hp);
        currentHp = Mathf.Clamp(h.hp, 0, maxHp);

        // 초기 배치: Job.loc(0=None이면 Front로 폴백)
        currentLoc = (Loc)Mathf.Clamp(h.loc, 0, 2);
        if (currentLoc == Loc.None) currentLoc = Loc.Front;

        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    /// <summary>
    /// 몬스터 Combatant 세팅
    /// </summary>
    public void InitMonster(MonsterData md)
    {
        side = Side.Enemy;
        monsterData = md;
        if (md != null)
        {
            baseSpeed = md.spd;
            currentHp = md.hp;
            maxHp = md.hp;

            currentLoc = monsterData.loc;
        }
        else
        {
            // 기본값
            baseSpeed = 5;
            currentHp = 10;
            maxHp = 30;

            currentLoc = Loc.Front;
        }
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    public static Combatant FindByHero(Job h)
        => h == null ? null
           : FindObjectsOfType<Combatant>(true)
               .FirstOrDefault(c => c.side == Side.Hero && c.hero == h);

    public static Combatant FindByMonster(MonsterData m)
        => m == null ? null
           : FindObjectsOfType<Combatant>(true)
               .FirstOrDefault(c => c.side == Side.Enemy && c.monsterData == m);

    // ======= 전투 진행 ======

    // 데미지 적용
    public void ApplyDamage(int amount)
    {
        int a = Mathf.Max(0, amount);
        currentHp = Mathf.Max(0, currentHp - a);
        if (side == Side.Hero && hero != null) hero.hp = currentHp; // DTO 동기화 (영웅일 때)
        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0 && !_dead)
            Die();
    }

    // 지속턴 1감소, 0이하 제거
    static void TickDict(IDictionary<BuffType, int> turns)
    {
        if (turns == null || turns.Count == 0) return;
        var keys = new List<BuffType>(turns.Keys);
        foreach (var k in keys)
        {
            turns[k] = turns[k] - 1;
            if (turns[k] <= 0) turns.Remove(k);
        }
    }

    // 지속 효과 영웅/몬스터 공통 처리
    public void TickStatuses()
    {
        if (hero != null)
        {
            Debug.Log($"[Tick] {DisplayName} (HeroDict)");

            // 영웅: JobData 내부 딕셔너리 직접 틱
            TickDict(hero.BuffsDict);
            TickDict(hero.DebuffsDict);
        }
        else
        {
            Debug.Log($"[Tick] {DisplayName} (MonsterDict)");

            // 몬스터: 기존 Combatant 딕셔너리 틱
            TickDict(_monsterBuffTurns);
            TickDict(_monsterDebuffTurns);
        }

        TickAbilityMods();
    }

    // ======== 능력치 버프/디버프 ========
    // 우선 적용 버프만 유지
    public bool TryAddAbilityModFirstWins(BuffType type, int value, int duration)
    {
        bool incomingIsBuff = value >= 0;
        for (int i = 0; i < _abilityMods.Count; i++)
        {
            var m = _abilityMods[i];
            if (m.type == type && (m.value >= 0) == incomingIsBuff)
            {
                // 같은 타입 & 같은 부호가 이미 존재 → 새로 온 건 무시(First wins)
                return false;
            }
        }

        _abilityMods.Add(new AbilityModEntry
        {
            type = type,
            value = value,
            remainingTurns = Mathf.Max(1, duration)
        });
        return true;
    }

    // 능력치 증감 적용
    private int SumAbility(BuffType t)
    {
        int s = 0;
        for (int i = 0; i < _abilityMods.Count; i++)
            if (_abilityMods[i].type == t) s += _abilityMods[i].value;
        return s;
    }

    // 능력치 속도 적용
    public int GetCurrentSpeed()
    {
        // baseSpeed + (Speed 버프/디버프 합)
        return Mathf.Max(0, baseSpeed + SumAbility(BuffType.Speed));
    }

    // 능력치 증감 턴 소모
    private void TickAbilityMods()
    {
        for (int i = _abilityMods.Count - 1; i >= 0; --i)
        {
            _abilityMods[i].remainingTurns--;
            if (_abilityMods[i].remainingTurns <= 0)
                _abilityMods.RemoveAt(i);
        }
    }

    public int RemoveAllDebuffs(bool alsoClearNegativeAbilityMods = true)
    {
        int removed = 0;

        // 1) 상태/태그형 '디버프'만 제거
        if (hero != null)
        {
            if (hero.DebuffsDict != null && hero.DebuffsDict.Count > 0)
            {
                removed += hero.DebuffsDict.Count;
                hero.DebuffsDict.Clear();
            }
            // ❌ hero.BuffsDict 는 절대 지우지 않음
        }
        else
        {
            if (_monsterDebuffTurns != null && _monsterDebuffTurns.Count > 0)
            {
                removed += _monsterDebuffTurns.Count;
                _monsterDebuffTurns.Clear();
            }
            // ❌ _monsterBuffTurns 는 절대 지우지 않음
        }

        // 2) 능력치 수정자 중 '음수(디버프)'만 제거
        if (alsoClearNegativeAbilityMods && _abilityMods != null && _abilityMods.Count > 0)
        {
            for (int i = _abilityMods.Count - 1; i >= 0; --i)
            {
                if (_abilityMods[i].value < 0) { _abilityMods.RemoveAt(i); removed++; }
            }
            // ❌ value >= 0 (버프)은 그대로 유지
        }

        Debug.Log($"[Cleanse] {DisplayName}: removed {removed} negative effects");
        return removed;
    }

    // 사망 처리
    private void Die()
    {
        if (_dead) return;
        _dead = true;

        // 사망 디버그
        Debug.Log($"[Death] {DisplayName} 사망 (Side={side}, HP={currentHp}/{maxHp})", this);

        try { OnDied?.Invoke(this); } catch { }

        // 아웃라인 제거
        var od = GetComponent<OutlineDuplicator>();
        if (od) od.EnableOutline(false);

        Destroy(gameObject);
    }

    // 체력 회복
    public void HealHp(int amount)
    {
        int a = Mathf.Max(0, amount);
        currentHp = Mathf.Min(maxHp, currentHp + a);
        if (side == Side.Hero && hero != null) hero.hp = currentHp;
        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    /// <summary>
    /// Tag = Party일 시 Hero / 아닐 시 Monster
    /// </summary>
    public void AutoInitByHierarchy(Job heroCandidate = null, MonsterData mdCandidate = null)
    {
        bool isParty = HasParentWithTag(transform, "Party");
        if (isParty) InitHero(heroCandidate ?? hero);
        else InitMonster(mdCandidate ?? monsterData);
    }

    // 상위에 특정 태그가 있는지 검사
    private static bool HasParentWithTag(Transform t, string tag)
    {
        while (t != null) { if (t.CompareTag(tag)) return true; t = t.parent; }
        return false;
    }

    // === Buff/CC 브리지: 영웅은 Job으로, 몬스터는 내부로(추후 확장) ===

    // 능력치 버프 수치 조정
    public void AddStatus(BuffType type, int duration)
    {
        if (duration <= 0) duration = 1;

        if (hero != null)
        {
            // 영웅: Job 내부 딕셔너리로 위임
            if (BuffGroups.IsDebuff(type))      // 버프/디버프 확인
                hero.AddDebuff(type, duration);
            else
                hero.AddBuff(type, duration);
            return;
        }

        if (BuffGroups.IsDebuff(type)) AddDebuff(type, duration);
        else AddBuff(type, duration);
    }

    public void AddBuff(BuffType type, int duration)
    {
        if (hero != null) { hero.AddBuff(type, duration); Debug.Log($"[Buff/Add] {DisplayName}: +{type} ({duration}T, HeroDict)"); return; }

        int cur = _monsterBuffTurns.TryGetValue(type, out var v) ? v : 0;
        _monsterBuffTurns[type] = Mathf.Max(cur, duration);

        Debug.Log($"[Buff/Add] {DisplayName}: +{type} ({duration}T, MonsterDict)");

    }

    public void AddDebuff(BuffType type, int duration)
    {
        if (duration <= 0) duration = 1;

        if (hero != null) { hero.AddDebuff(type, duration); Debug.Log($"[Debuff/Add] {DisplayName}: +{type} ({duration}T, HeroDict)"); return; }

        int cur = _monsterDebuffTurns.TryGetValue(type, out var v) ? v : 0;
        _monsterDebuffTurns[type] = Mathf.Max(cur, duration);
        Debug.Log($"[Debuff/Add] {DisplayName}: +{type} ({duration}T, MonsterDict)");
    }

    public bool HasBuff(BuffType type)
        => hero != null ? hero.HasBuff(type) : _monsterBuffTurns.ContainsKey(type);

    public bool HasDebuff(BuffType type)
        => hero != null ? hero.HasDebuff(type) : _monsterDebuffTurns.ContainsKey(type);
}
