using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public abstract class DebuffEffect : SkillEffect
{
    public float probability = 1f;
    public abstract SkillDebuffType DebuffType { get; }
    public override void Apply(Job user, Job target)
    {
        if (Random.value <= probability)
        {
            ApplyDebuff(user, target);
        }
    }

    protected abstract void ApplyDebuff(Job user, Job target);
}

public class PoisonEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Poison;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddDebuff(DebuffType, duration);
    }
}

public class BleedingEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Bleeding;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddDebuff(DebuffType, duration);
    }
}

public class BurnEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Burn;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.AddDebuff(DebuffType, duration);
    }
}

public class SignEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Sign;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.Marked = true;
        target.AddDebuff(DebuffType, duration);
    }
}

public class FaintEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Faint;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.CanAct = false;
        target.AddDebuff(DebuffType, duration);
    }
}

public class TauntEffect : DebuffEffect
{
    public override SkillDebuffType DebuffType => SkillDebuffType.Taunt;

    protected override void ApplyDebuff(Job user, Job target)
    {
        target.ForcedTarget = user;
        target.AddDebuff(DebuffType, duration);
    }
}