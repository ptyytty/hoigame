using System;
using UnityEngine;

/// <summary>
/// 아이템 효과를 통일한 데이터 전용 포맷
/// - 런타임에선 ItemEffectBridge가 SkillEffect로 변환해서 공용 파이프라인으로 태움
/// - 장비의 지속 효과는 persistent=true 로 표시(착용 중 on/off)
/// </summary>
[Serializable]
public class ItemEffectSpec
{
    public EffectOp op;            // 효과 종류
    public BuffType stat;          // AbilityMod/ApplyDebuff에서 쓰는 대상 스탯/버프
    public int value;              // 정수 값(± 가능)
    public int duration;           // 지속 턴
    public bool percent;           // Heal이 퍼센트 기반일 때
    public float rate;             // percent=true일 때 비율(0.1=10%)
    public bool persistent;        // true면 장비형 지속 효과
    public string specialKey;      // 특수 효과 식별자(면역, 턴 훅 등 자유)
    public float probability = 1f; // 디버프 부여 확률
}

/// <summary> 효과 종류 </summary>
public enum EffectOp
{
    Damage,
    Heal,
    AbilityMod,    // 능력치 증감(+/-)
    ApplyDebuff,   // Poison/Bleeding/Burn/Sign/Faint/Taunt 등
    Cleanse,       // 지정 디버프 제거(여기선 통합 제거로 사용)
    Special        // 자유 훅(onApply/onRemove)
}