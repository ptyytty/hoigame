using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class ConsumeBuffDic
{
    private static readonly Dictionary<string, string> specialEffectTexts = new Dictionary<string, string>
    {
        // 예시: specialKey → 표시하고 싶은 한글 설명
        { "Immune_Burn", "화상 면역" },
        { "Immune_Faint", "기절 면역" }
        // 자유롭게 확장 가능
    };

    /// <summary>
    /// 효과 연산자 한글화 (UI 표기용)
    /// </summary>
    public static string ToKorean(this EffectOp op)
    {
        switch (op)
        {
            case EffectOp.Damage: return "스킬 데미지";
            case EffectOp.Heal: return "회복량";
            case EffectOp.AbilityMod: return "능력치 증가";
            case EffectOp.ApplyDebuff: return "상태 이상";
            case EffectOp.Cleanse: return "디버프 제거";
            case EffectOp.Special: return "특수 효과";
            default: return op.ToString();
        }
    }

    

    /// <summary>
    /// BuffType → 한글화 (아이템/스킬 공통)
    /// </summary>
    public static string ToKorean(this BuffType type)
    {
        switch (type)
        {
            // ────────────── Buff 계열 ──────────────
            case BuffType.Defense:    return "방어";
            case BuffType.Resistance: return "저항";
            case BuffType.Speed:      return "민첩";
            case BuffType.Hit:        return "명중";
            case BuffType.Damage:     return "피해량";
            case BuffType.Heal:       return "회복량";
            case BuffType.Remove:     return "디버프 제거";

            // ────────────── Debuff 계열 ──────────────
            case BuffType.Poison:     return "중독";
            case BuffType.Bleeding:   return "출혈";
            case BuffType.Burn:       return "화상";
            case BuffType.Sign:       return "표식";
            case BuffType.Faint:      return "기절";
            case BuffType.Taunt:      return "도발";

            default:                  return type.ToString();
        }
    }

    /// <summary>
    /// 단일 효과를 아이템 정보창에 맞는 한 줄 텍스트로 변환
    /// </summary>
    public static string BuildEffectLine(this ItemEffectSpec e)
    {
        string line = "- ";

        switch (e.op)
        {
            case EffectOp.Damage:
            {
                string dir = e.value >= 0 ? "증가" : "감소";
                line += $"스킬 데미지 {Mathf.Abs(e.value)} {dir}";
                break;
            }
            case EffectOp.Heal:
            {
                if (e.percent)
                    line += $"회복 {e.rate * 100f:0.#}%";
                else
                    line += $"회복 {e.value}";
                break;
            }
            case EffectOp.AbilityMod:
            {
                string dir = e.value >= 0 ? "증가" : "감소";
                line += $"{e.stat.ToKorean()} {Mathf.Abs(e.value)} {dir}";
                break;
            }
            case EffectOp.ApplyDebuff:
            {
                string dur = e.duration > 0 ? $" {e.duration}턴" : "";
                string prob = e.probability < 1f ? $" (확률 {e.probability * 100f:0.#}%)" : "";
                line += $"{e.stat.ToKorean()} 부여{dur}{prob}";
                break;
            }
            case EffectOp.Cleanse:
            {
                line += "디버프 제거";
                break;
            }
            case EffectOp.Special:
            {
                if (!string.IsNullOrWhiteSpace(e.specialKey))
                {
                    // 사전에 등록된 한글 문구가 있으면 사용
                    if (specialEffectTexts.TryGetValue(e.specialKey, out string text))
                        line += text;
                    else
                        // 없으면 원래 키 그대로 표시 (디버깅용)
                        line += $"특수 효과: {e.specialKey}";
                }
                else
                {
                    line += "특수 효과";
                }
                break;
            }
            default:
                line += e.op.ToString();
                break;
        }

        return line;
    }

    /// <summary>
    /// 여러 효과를 줄바꿈으로 합쳐서 UI에 넣기
    /// </summary>
    public static string BuildEffectSummary(this IEnumerable<ItemEffectSpec> effects)
    {
        if (effects == null) return string.Empty;
        return string.Join("\n", effects.Select(BuildEffectLine));
    }
}
