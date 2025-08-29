public enum StatKind { Defense, Resistance, Speed, Hit, Damage, Hp }

public interface IBattleUnit
{
    void AddTimedModifier(StatKind stat, int value, int durationTurns);          //스탯 가산치
    void Heal(int amount);
    void AddDot(string name, int damageperTurn, int turns);
    void ApplyCrowdControl(string name, int turns);
    void ApplyMark(int turns);
    void Dispel(int count, bool onlyDebuff);                                     // 효과 제거
}
