using System.Collections.Generic;
using System.Linq;
using System;

// 런타임에서 영웅별 스킬 조회
public static class SkillCatalog
{
    private static bool built;
    private static Dictionary<int, List<Skill>> byHero;  // heroId -> 스킬 3개 딕셔너리

    private static void BuildOnce()
    {
        if (built) return;
        var skills = new HeroSkills(); // 파일에 있는 클래스

        byHero = new Dictionary<int, List<Skill>>
        {
            { 0, skills.holyKnightSkills },
            { 1, skills.warriorSkills },
            { 2, skills.woodcutterSkills },
            { 3, skills.boxerSkills },
            { 4, skills.archerSkills },
            { 5, skills.hunterSkills },
            // { 6, skills.ninzaSkills },
            // { 7, skills.GeneralWizardSkills }, // Fire/Ice/Electric
            // { 8, skills.ShamanSkills },
            // { 9, skills.BomberSkills },
            { 6, skills.ClericSkills },
            { 7, skills.DoctorSkills }
        };

        built = true;
    }

    /// <summary>
    /// 해당 영웅(ownHeroId)의 모든 스킬 ID를 반환 (정규화/표시용)
    /// </summary>
    public static IReadOnlyList<int> GetHeroSkillIds(int ownHeroId)
    {
        BuildOnce();
        var list = GetHeroSkills(ownHeroId);
        // 스킬 ID 중복 방지
        return list.Select(s => s.skillId).Distinct().ToList(); // s = 원소 s의 skillId return
    }

    // 해당 영웅(ownHeroId)의 스킬들 조회
    public static IReadOnlyList<Skill> GetHeroSkills(int ownHeroId)
    {
        BuildOnce();
        // ownHeroId 찾으면 true, 값 list에 대입 / 아니면 false 
        bool found = byHero.TryGetValue(ownHeroId, out var list);  // list = List<Skill>
        // list / 캐시된 빈 배열 반환
        return found ? list : Array.Empty<Skill>();
    }

    public static Skill GetSkill(int heroId, int localSkillId)
    {
        var list = GetHeroSkills(heroId);
        return list.FirstOrDefault(s => s.skillId == localSkillId);
    }
}
