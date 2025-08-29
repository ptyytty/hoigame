using System.Collections;
using System.Collections.Generic;
using System;

public class JobUnitAdapter : IBattleUnit
{
    public Job Data { get; }                     // 원본 DTO
    public bool CanAct { get; private set; } = true;
    public bool Marked { get; private set; } = false;

    private readonly Dictionary<StatKind, TimedMod> _mods = new();
    private readonly Dictionary<string, DotInstance> _dots = new();
    private readonly Dictionary<string, int> _cc = new(); // "Stun","Taunt"
    private int _markRemaining = 0;
    private int _tempDamageBonus = 0;

    public JobUnitAdapter(Job dto) { Data = dto; }

    // IBattleUnit 구현
    public void AddTimedModifier(StatKind stat, int value, int turns)
    {
        if (_mods.TryGetValue(stat, out var m)) { m.Value += value; m.Remaining = Math.Max(m.Remaining, turns); }
        else _mods[stat] = new TimedMod { Stat = stat, Value = value, Remaining = turns };

        ApplyStatDelta(stat, value);
    }

    public void Heal(int amount) => Data.hp += Math.Max(0, amount);

    public void AddDot(string name, int dpt, int turns)
    {
        if (_dots.TryGetValue(name, out var d)) { d.DamagePerTurn += dpt; d.Remaining = Math.Max(d.Remaining, turns); }
        else _dots[name] = new DotInstance { Name = name, DamagePerTurn = dpt, Remaining = turns, Stacks = 1 };
    }

    public void ApplyCrowdControl(string name, int turns)
    {
        _cc[name] = Math.Max(_cc.TryGetValue(name, out var cur) ? cur : 0, turns);
        if (name == "Stun") CanAct = false;
    }

    public void ApplyMark(int turns) { Marked = true; _markRemaining = Math.Max(_markRemaining, turns); }

    public void Dispel(int count, bool onlyDebuff)
    {
        int removed = 0;
        removed += RemoveOne(_dots);
        if (removed >= count) return;
        removed += RemoveOne(_cc);
        if (removed >= count) return;

        foreach (var key in new List<StatKind>(_mods.Keys))
        {
            if (removed >= count) break;
            RevertStat(_mods[key]); _mods.Remove(key); removed++;
        }
        if (removed < count && _markRemaining > 0) { _markRemaining = 0; Marked = false; }
    }

    // 턴 종료 시 호출
    public void TickEndOfTurn()
    {
        int totalDot = 0;
        foreach (var key in new List<string>(_dots.Keys))
        {
            var d = _dots[key];
            totalDot += d.DamagePerTurn;
            if (--d.Remaining <= 0) _dots.Remove(key);
            else _dots[key] = d;
        }
        if (totalDot > 0) Data.hp -= totalDot;

        foreach (var key in new List<StatKind>(_mods.Keys))
        {
            var m = _mods[key];
            if (--m.Remaining <= 0) { RevertStat(m); _mods.Remove(key); }
            else _mods[key] = m;
        }

        foreach (var key in new List<string>(_cc.Keys))
        {
            if (--_cc[key] <= 0) _cc.Remove(key);
        }
        if (!_cc.ContainsKey("Stun")) CanAct = true;

        if (_markRemaining > 0 && --_markRemaining <= 0) Marked = false;
    }

    // 내부 유틸
    private void ApplyStatDelta(StatKind stat, int delta)
    {
        switch (stat)
        {
            case StatKind.Defense:    Data.def += delta; break;
            case StatKind.Resistance: Data.res += delta; break;
            case StatKind.Speed:      Data.spd += delta; break;
            case StatKind.Hit:        Data.hit += delta; break;
            case StatKind.Damage:     _tempDamageBonus += delta; break;
            case StatKind.Hp:         Data.hp += delta; break;
        }
    }
    private void RevertStat(TimedMod m) => ApplyStatDelta(m.Stat, -m.Value);

    private static int RemoveOne<TKey, TValue>(Dictionary<TKey, TValue> dict)
    {
        foreach (var key in new List<TKey>(dict.Keys)) { dict.Remove(key); return 1; }
        return 0;
    }

    public int CurrentDamageBonus => _tempDamageBonus;

    private struct TimedMod   { public StatKind Stat; public int Value; public int Remaining; }
    private struct DotInstance{ public string Name; public int DamagePerTurn; public int Remaining; public int Stacks; }
}
