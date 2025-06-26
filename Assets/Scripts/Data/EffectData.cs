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
    Addiction,  // 중독
    Bleeding,   // 출혈
    Burn,       // 화상
    Sign,       // 표식
    Faint,      // 기절
    Taunt       // 도발
}

[System.Serializable]
public class Debuff
{
    public DebuffType debuffType;

    public int duration;    // 지속 턴 수
    public string description;  // 설명
}

