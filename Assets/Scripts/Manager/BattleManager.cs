using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 전투 스크립트
public class BattleManager : MonoBehaviour
{
    [Header("Party Info")]
    private IReadOnlyList<Job> party;
    private List<Job> activeSequence;
    private int turnIndex = -1;

    [Header("UI Control")]
    [SerializeField] private UIManager uiManager;

    private HeroSkills heroSkills = new HeroSkills();

    void OnEnable()
    {
        EnemySpawner.OnBattleStart += HandleBattleStart;
    }

    private void HandleBattleStart(IReadOnlyList<Job> heroes, IReadOnlyList<GameObject> enemies)
    {
        party = heroes;

        BeginBattle(party);
    }

    public void BeginBattle(IEnumerable<Job> overrideParty = null)
    {
        if (overrideParty != null) party = overrideParty.Where(hero => hero != null).ToList();
        CheckSpeed();
        turnIndex = -1;
        Debug.Log("턴 순서: " + string.Join(" → ",
                activeSequence.Select((j, i) =>
                $"{i + 1}. {(j?.name_job ?? "null")} (SPD {j?.spd})")));
        NextTurn();
    }

    private void CheckSpeed()
    {
        activeSequence = party
                        .Where(hero => hero != null)          // 널 방지
                        .OrderByDescending(hero => hero.spd)  // SPD 내림차순
                        .ThenBy(hero => hero.id_job)          // 동률 시 id 비교
                        .ToList();
    }

    private void NextTurn()
    {
        if (activeSequence == null || activeSequence.Count == 0)
        {
            Debug.LogWarning("[Battle] No units in activeSequence.");
            return;
        }

        turnIndex = (turnIndex + 1) % activeSequence.Count; // 턴 무한 순회

        Job hero = activeSequence[turnIndex];

        if (hero == null)
        {
            Debug.LogWarning("[Battle] Hero null.");
            return;
        }

        Debug.Log($"[Turn] {hero.name_job}, SPD: {hero.spd}");

        ShowHeroSkill(hero);
    }

    // 스킬 표시
    private void ShowHeroSkill(Job hero)
    {
        var skills = heroSkills.GetHeroSkills(hero).ToList();

        if (skills.Count == 0)
        {
            Debug.LogError($"[SkillUI] {hero.name_job} has no skills");
            return;
        }

        uiManager.ShowSkills(hero, skills);     // UI 출력
    }
}
