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
        Debug.Log("[HL] Awake set Instance");
    }

    /// <summary>
    /// 스킬을 선택했을 때, 규칙에 맞는 대상만 하이라이트.
    /// </summary>
    public void HighlightForSkill(Combatant user, Skill skill)
    {
        if (!outlineMaterial) { Debug.LogWarning("[Highlighter] outlineMaterial 미할당"); return; }
        ClearAll();

        var all = FindObjectsOfType<Combatant>(includeInactive: true);
        Debug.Log($"[HL] candidates: {all.Length}, user={user?.name}");

        int enabledCount = 0;
        foreach (var c in all)
        {
            if (!c || !c.IsAlive) continue;
            if (!IsValidByTarget(user, c, skill)) continue;
            if (!IsValidByAreaPlaceholder(user, c, skill)) continue; // Row/Entire 구조가 생기면 이곳 확장

            var od = GetOrAddOutline(c.gameObject);
            if (od == null) continue;

            od.outlineMaterial = outlineMaterial; // 동적 주입 허용
            od.SetProperties(outlineColor, outlineWidth);
            od.EnableOutline(true);
        }
        Debug.Log($"[HL] enabled outlines: {enabledCount}");
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

    // 대상 처리
    private static bool IsValidByTarget(Combatant user, Combatant cand, Skill skill)
    {
        switch (skill.target)
        {
            case Target.Enemy: return cand.side != user.side;
            case Target.Ally: return cand.side == user.side && cand != user;
            case Target.Self: return cand == user;
            default: return false;
        }
    }

    // 범위 처리
    // TODO: 자리/열 정보가 생기면 여기에 실제 Row/Entire 판정 로직을 추가
    private static bool IsValidByAreaPlaceholder(Combatant user, Combatant cand, Skill skill)
    {
        // 지금은 Row/Entire도 "선택 가능한 모든 대상"을 하이라이트
        // Single도 일단은 후보 전부 하이라이트(선택 시 1명을 집어주면 됨)
        return true;
    }

    // (선택) 외부에서 Job만 넘어오는 경우를 대비
    public void HighlightForSkill(Job actingHero, Skill skill)
    {
        var user = FindObjectsOfType<Combatant>(true)
                   .FirstOrDefault(c => c.side == Side.Hero && c.hero == actingHero);
        if (!user) { Debug.LogWarning("[Highlighter] Acting hero Combatant 못찾음"); return; }
        HighlightForSkill(user, skill);
    }
}
