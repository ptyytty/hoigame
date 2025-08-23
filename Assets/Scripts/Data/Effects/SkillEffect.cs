using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class SkillEffect : MonoBehaviour
{
    public int duration;
    public abstract void Apply(Job user, Job target);
}

public class DamageEffect : SkillEffect
{
    public int damage;
    public override void Apply(Job user, Job target)
    {
        target.hp -= damage;
        
    }
}

public class AbilityBuff : SkillEffect
{
    public int value;
    public BuffType ability;
    public override void Apply(Job user, Job target)
    {
        
    }

}

public class SpecialSkillEffect : SkillEffect
{
    public Action<Job, Job> onApply;

    public override void Apply(Job user, Job target)
    {
        onApply?.Invoke(user, target);
    }
}

public abstract class DebuffEffect : SkillEffect
{
    public float probability = 1f;
    public abstract BuffType DebuffType { get; }
    public override void Apply(Job user, Job target)
    {
        if (UnityEngine.Random.value <= probability)
        {
            ApplyDebuff(user, target);
        }
    }

    protected abstract void ApplyDebuff(Job user, Job target);
}

public class PoisonEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Poison;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddBuff(DebuffType, duration);
    }
}

public class BleedingEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Bleeding;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddBuff(DebuffType, duration);
    }
}

public class BurnEffect : DebuffEffect
{
    public int value;
    public override BuffType DebuffType => BuffType.Burn;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddBuff(DebuffType, duration);
    }
}

public class SignEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Sign;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.Marked = true;
        target.AddBuff(DebuffType, duration);
    }
}

public class FaintEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Faint;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.CanAct = false;
        target.AddBuff(DebuffType, duration);
    }
}

public class TauntEffect : DebuffEffect
{
    public override BuffType DebuffType => BuffType.Taunt;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.ForcedTarget = user;
        target.AddBuff(DebuffType, duration);
    }
}