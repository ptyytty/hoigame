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
    [SerializeField] private UIManager uIManager;


    private HeroSkills heroSkills = new HeroSkills();
    

    void Start()
    {
        party = PartyBridge.Instance?.ActiveParty;

        CheckSpeed();
    }

    private void CheckSpeed()
    {
        activeSequence = party
    .Where(hero => hero != null)          // 널 방지
    .OrderByDescending(hero => hero.spd)  // SPD 내림차순
    .ThenBy(hero => hero.id_job)          // 동률 시 id 비교
    .ToList();
    }

    private void NextTurn(){
        if(activeSequence == null || activeSequence.Count == 0){
            Debug.LogWarning("[Battle] No units in activeSequence.");
            return;
        }

        turnIndex = (turnIndex + 1) % activeSequence.Count; // 턴 무한 순회

        Job hero = activeSequence[turnIndex];

        if(hero == null){
            Debug.LogWarning("[Battle] Hero null.");
            return;
        }

        Debug.Log($"[Turn] {hero.name_job}, SPD: {hero.spd}");

        ShowHeroSkill(hero);
    }

    private void ShowHeroSkill(Job hero){
        var skills = heroSkills.GetHeroSkills(hero).ToList();

        if(skills.Count == 0){
            Debug.LogError($"[SkillUI] {hero.name_job} has no skills");
            return;
        }

        
    }
}
