public enum EffectType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Special
}

public enum DebuffType
{
    Addiction,
    Bleeding,
    Burn,
    Sign,
    Faint,
    Taunt
}

[System.Serializable]
public class Debuff
{
    public DebuffType debuffType;

    public int duration;    // 지속 턴 수
    public string description;  // 설명
}