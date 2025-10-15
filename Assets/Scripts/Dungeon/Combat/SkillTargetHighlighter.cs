// SkillTargetHighlighter.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// [역할] 스킬 프리뷰 시 "규칙에 맞는 후보들"만 아웃라인 ON,
///        취소/턴전환 시 전부 OFF.
/// </summary>
public class SkillTargetHighlighter : MonoBehaviour
{
    public static SkillTargetHighlighter Instance { get; private set; }

    [Header("Outline (Provider fallback)")]
    [SerializeField] private Material outlineMaterial;    // 비워도 Provider에서 자동 확보
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField] private float outlineWidth = 0.08f;

    // 역할: 하이라이트를 건 대상 캐시(끄기용)
    private readonly List<OutlineDuplicator> _cache = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!outlineMaterial) outlineMaterial = OutlineMaterialProvider.GetShared();
    }

    /// <summary>역할: 스킬 프리뷰 시작 시 후보들만 아웃라인 켜기</summary>
    public void HighlightForSkill(Combatant user, Skill skill)
    {
        if (!EnsureMaterial()) { ClearAll(); return; }

        ClearAll();

        var all = FindObjectsOfType<Combatant>(includeInactive: true);
        foreach (var c in all)
        {
            if (c == null || !c.IsAlive) continue;
            if (!SkillTargeting.IsCandidate(user, c, skill)) continue;

            var od = GetOrAddOutline(c.gameObject);
            od.outlineMaterial = outlineMaterial;
            od.SetProperties(outlineColor, outlineWidth);
            od.EnableOutline(true);
        }
    }

    /// <summary>역할: 턴 전환/스킬 취소 시 모든 아웃라인 끄기</summary>
    public void ClearAll()
    {
        for (int i = _cache.Count - 1; i >= 0; i--)
        {
            var od = _cache[i];
            if (!od) { _cache.RemoveAt(i); continue; }
            od.EnableOutline(false);
        }
    }

    // ── 내부 유틸 ──────────────────────────────
    bool EnsureMaterial()
    {
        if (outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported) return true;
        outlineMaterial = OutlineMaterialProvider.GetShared();
        return outlineMaterial && outlineMaterial.shader && outlineMaterial.shader.isSupported;
    }

    OutlineDuplicator GetOrAddOutline(GameObject go)
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
}
