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
    public int baseSpeed;      // 기본 SPD
    public int maxHp;           // 최대 체력
    public int currentHp;
    public bool IsAlive => !_dead && currentHp > 0;
    public event Action<Combatant> OnDied;
    public event Action<int, int> OnHpChanged;   // current, max
    private bool _dead = false;

    [Header("영웅 데이터")]
    public Job hero;           // side == Hero 일 때 연결
                               // Job 구조체 안에 spd, hp, name_job 등이 있음

    [Header("몬스터 데이터")]
    public MonsterData monsterData;   // side == Enemy 일 때 연결

    /// <summary>
    /// 턴 정렬에 사용하는 spd (버프/디버프 적용 시 수정 가능)
    /// </summary>
    public int EffectiveSpeed => baseSpeed;

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
            // ✅ MonsterData가 들고 있는 고유 id를 키로 사용
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
        maxHp = h.hp;
        currentHp = h.hp;

        // 초기 배치: Job.loc(0=None이면 Front로 폴백)
        currentLoc = (Loc)Mathf.Clamp(h.loc, 0, 2);
        if (currentLoc == Loc.None) currentLoc = Loc.Front;

        OnHpChanged?.Invoke(currentHp, maxHp);
    }

    /// <summary>
    /// 초기화용: 몬스터 Combatant 세팅
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

    // 체력 감소 (데미지)
    public void ApplyDamage(int amount)
    {
        int a = Mathf.Max(0, amount);
        currentHp = Mathf.Max(0, currentHp - a);
        if (side == Side.Hero && hero != null) hero.hp = currentHp; // DTO 동기화 (영웅일 때)
        OnHpChanged?.Invoke(currentHp, maxHp);

        if (currentHp <= 0 && !_dead)
            Die();
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;

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
    /// Tag = Party일 시 Hero
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
    private readonly Dictionary<BuffType,int> _monsterBuffTurns = new();

    public void AddBuff(BuffType type, int duration)
    {
        if (hero != null) { hero.AddBuff(type, duration); return; }   // 영웅은 기존 Job 로직 사용
        // 몬스터는 간단히 지속 턴만 저장(수치 반영은 추후 확장)
        if (duration <= 0) duration = 1;
        _monsterBuffTurns[type] = Mathf.Max(_monsterBuffTurns.TryGetValue(type, out var cur) ? cur : 0, duration);
    }

    public bool HasBuff(BuffType type) =>
        hero != null ? hero.HasBuff(type) : _monsterBuffTurns.ContainsKey(type);
}
