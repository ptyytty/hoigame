using System.Collections.Generic;
using System.Linq;

public static class SkillTargeting
{
    // ====== 공개 API ======

    // 하이라이트/클릭 후보 공통 판정 (프리뷰 단계용)
    public static bool IsCandidate(Combatant user, Combatant cand, Skill skill)
        => IsValidByTarget(user, cand, skill)
        && IsValidByTargetLoc(cand, skill)
        && IsValidByAreaPreselect(cand, skill);

    // 실제 시전 시 적용 대상 계산 (클릭된 대상 click 기준)
    // allCombatants: 현재 전투의 모든 Combatant (살아있는 것만 넘겨도 OK)
    public static List<Combatant> GetExecutionTargets(
        Combatant user, Combatant click, IEnumerable<Combatant> allCombatants, Skill skill)
    {
        if (!IsValidByTarget(user, click, skill)) return new();     // 잘못 클릭
        if (!IsValidByTargetLoc(click, skill)) return new();

        // 공통 필터: 진영 + 위치
        var pool = allCombatants.Where(c => c != null && c.IsAlive)
                                .Where(c => IsValidByTarget(user, c, skill))
                                .Where(c => IsValidByTargetLoc(c, skill));

        switch (skill.area)
        {
            case Area.Single:
                return new() { click };

            case Area.Row:
                // 행 확정 규칙:
                // - targetLoc이 지정되어 있으면 그 행
                // - targetLoc이 None이면 '클릭한 대상의 currentLoc'이 행
                var row = (skill.targetLoc == Loc.None) ? click.currentLoc : skill.targetLoc;
                if (row == Loc.None) return new();
                return pool.Where(c => c.currentLoc == row).ToList();

            case Area.Entire:
                // 같은 진영 전체(행 무관)
                return pool.ToList();

            default:
                return new() { click };
        }
    }

    // ====== 내부 규칙 ======

    // 진영 필터 (Hero/Enemy)
    private static bool IsValidByTarget(Combatant user, Combatant cand, Skill skill)
    {
        return skill.target switch
        {
            Target.Enemy => cand.side != user.side,
            Target.Ally  => cand.side == user.side,
            Target.Self  => cand == user,
            _            => false
        };
    }

    // 위치 필터 (Front/Back/None)
    private static bool IsValidByTargetLoc(Combatant cand, Skill skill)
    {
        if (skill.targetLoc == Loc.None) return true;
        if (cand.currentLoc == Loc.None) return false;
        return cand.currentLoc == skill.targetLoc;
    }

    // 범위 필터 (Single/Row/Entire)
    private static bool IsValidByAreaPreselect(Combatant cand, Skill skill)
    {
        switch (skill.area)
        {
            case Area.Single:
                return true; // 단일: 진영/위치만 맞으면 후보

            case Area.Row:
                // targetLoc 지정 ⇒ 해당 행만 후보
                // targetLoc None ⇒ 두 행 모두 후보(클릭 시 그 행으로 확정)
                if (skill.targetLoc == Loc.None) return true;
                if (cand.currentLoc == Loc.None) return false;
                return cand.currentLoc == skill.targetLoc;

            case Area.Entire:
                return true; // 행 무관

            default:
                return true;
        }
    }
}
