// SkillTargetHighlighter.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillTargetHighlighter : MonoBehaviour
{
    public static SkillTargetHighlighter Instance { get; private set; }

    [Header("Outline")]
    [Tooltip("Custom/Outline_InvertedHull_URP 머티리얼(필수)")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineWidth = 0.1f;

    // 풀링 겸 캐시
    private readonly List<OutlineDuplicator> _cache = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 스킬을 선택했을 때, 규칙에 맞는 대상만 하이라이트.
    /// </summary>
    public void HighlightForSkill(Combatant user, Skill skill)
    {
        // 아웃라인 머테리얼 확인
        if (!outlineMaterial) { Debug.LogWarning("[Highlighter] outlineMaterial 미할당"); return; }
        ClearAll();

        // Combatant 컴포넌트 수집
        var all = FindObjectsOfType<Combatant>(includeInactive: true);
        int enabled = 0;

        foreach (var c in all)
        {
            if (c == null || !c.IsAlive) continue;
            if (!SkillTargeting.IsCandidate(user, c, skill)) continue;

            var od = GetOrAddOutline(c.gameObject);
            if (!od) continue;
            od.outlineMaterial = outlineMaterial;
            od.SetProperties(outlineColor, outlineWidth);
            od.EnableOutline(true);
            enabled++;
        }
        Debug.Log($"[HL] enabled outlines: {enabled}");
    }

    /// <summary>
    /// 모든 아웃라인 끄기(턴 전환/스킬 취소 시 호출).
    /// </summary>
    public void ClearAll()
    {
        for (int i = _cache.Count - 1; i >= 0; i--)
        {
            var od = _cache[i];
            if (!od) { _cache.RemoveAt(i); continue; }
            od.EnableOutline(false);
        }
    }

    // ---------- 내부 유틸 ----------
    private OutlineDuplicator GetOrAddOutline(GameObject go)
    {
        var od = go.GetComponent<OutlineDuplicator>();
        if (!od)
        {
            od = go.AddComponent<OutlineDuplicator>();
            _cache.Add(od);
        }
        else if (!_cache.Contains(od)) _cache.Add(od);
        return od;
    }

    //=============== 대상 판정 ================
    // 적 / 아군 / 자신 필터
    private static bool IsValidByTarget(Combatant user, Combatant cand, Skill skill)
    {
        switch (skill.target)
        {
            case Target.Enemy: return cand.side != user.side;
            case Target.Ally: return cand.side == user.side;
            case Target.Self: return cand == user;
            default: return false;
        }
    }

    // Front / Back 필터
    private static bool IsValidByTargetLoc(Combatant cand, Skill skill)
    {
        if (skill.targetLoc == Loc.None) return true;

        if (cand.currentLoc == Loc.None) return false;

        return cand.currentLoc == skill.targetLoc;
    }

    // 범위(Single / Row / Entire) 처리
    private static bool IsValidByAreaPreselect(Combatant cand, Skill skill)
    {
        switch (skill.area)
        {
            case Area.Single:
                // 단일은 선택 전 프리뷰 단계에서 "선택 가능한 후보"만 밝히면 됨
                return true;

            case Area.Row:
                // Row는 '행 단위'로 맞춰야 하므로, targetLoc이 지정되면 해당 행만 하이라이트.
                // targetLoc이 None이면 두 행 모두 후보(선택 시 행 확정).
                if (skill.targetLoc == Loc.None) return true;

                if (cand.currentLoc == Loc.None) return false;
                return cand.currentLoc == skill.targetLoc;

            case Area.Entire:
                // Entire는 행 무관 전체 적용. 다만 target(적/아군/자신)만 필터.
                // 보통 targetLoc은 None으로 두지만 혹시 세팅했다면 무시하는 편이 자연스러움.
                return true;

            default:
                return true;
        }
    }
}
