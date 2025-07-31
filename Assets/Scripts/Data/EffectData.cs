// 스킬 타입
public enum SkillType
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Special
}

// 스킬 사용 시 디버프
public enum SkillDebuffType
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
    public SkillDebuffType debuffType;

    public int duration;    // 지속 턴 수
    public string description;  // 설명
}

