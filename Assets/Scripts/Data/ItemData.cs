// 사용 아이템 사용 시 효과
public enum ConsumeBuffType
{
    Damage,
    Heal,
    Remove,         // 디버프 제거
    Poison,
    Bleeding,
    Burn,
    Sign,           // 표식
    Faint,          // 기절
    Taunt,
    AbilityBuff,    // 능력치 버프
    AbilityDebuff,  // 능력치 디버프
    Special         // 특수 효과
}

// 장비 아이템 사용 시 효과
public enum EquipItemBuffType
{
    Hp,
    Def,
    Res,
    Spd,
    Hit,
    Dmg,
    Heal,
    Special
}

// 특수효과 목록
// Special과 연결
public enum SpecialBuffType
{
    BleedImmune,        // 화상 면역
    SpeedBoost,
    HealOverTime,
    FaintImmune         // 도발 면역
    // 확장 가능
}

[System.Serializable]
public class BuffEffect
{
    public ConsumeBuffType debuffType;
    public int duration;    // 지속 턴 수
    public string description;  // 설명

}