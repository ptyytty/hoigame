public enum ConsumeBuffType
{
    Damage,
    Heal,
    Remove,     // 디버프 제거
    Poison,
    Bleeding,
    Burn,
    Sign,
    Faint,
    Taunt,
    AbilityBuff,
    AbilityDebuff,
    Special
}

[System.Serializable]
public class DebuffEffect
{
    public ConsumeBuffType debuffType;

    public int duration;    // 지속 턴 수
    public string description;  // 설명

}