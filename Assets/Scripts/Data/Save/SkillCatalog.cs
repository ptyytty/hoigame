using System.Collections.Generic;
using System.Linq;

public static class SkillCatalog
{
    private static bool built;
    private static Dictionary<int, List<Skill>> byHero;  // heroId -> 스킬 3개

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
            { 6, skills.ninzaSkills },
            { 7, skills.GeneralWizardSkills }, // 필요 시 Fire/Ice/Electric로 바뀌는 직업은 정책 결정
            { 8, skills.ShamanSkills },
            { 9, skills.BomberSkills },
            { 10, skills.ClericSkills },
            { 11, skills.DoctorSkills }
        };

        built = true;
    }

    /// <summary>
    /// 해당 영웅(heroId)의 모든 스킬 ID를 반환 (정규화/표시용)
    /// </summary>
    public static IReadOnlyList<int> GetHeroSkillIds(int heroId)
    {
        BuildOnce();
        var list = GetHeroSkills(heroId);
        // 스킬 ID 중복 방지
        return list.Select(s => s.skillId).Distinct().ToList();
    }

    public static IReadOnlyList<Skill> GetHeroSkills(int heroId)
    {
        BuildOnce();
        return byHero.TryGetValue(heroId, out var list) ? list : (IReadOnlyList<Skill>)System.Array.Empty<Skill>();
    }

    public static Skill GetSkill(int heroId, int localSkillId)
    {
        var list = GetHeroSkills(heroId);
        return list.FirstOrDefault(s => s.skillId == localSkillId);
    }
}
