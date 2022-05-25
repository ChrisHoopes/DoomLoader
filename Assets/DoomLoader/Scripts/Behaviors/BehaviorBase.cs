using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BehaviorBase
{
    public abstract void Tick();
    public virtual void Pain() { }
    public virtual void AlertByNoise() { }
    public virtual void Init() { }

    public virtual void ModifyDamage(ref int amount, DamageType damageType) { }

    public virtual bool alert { get; set; }
    public virtual bool attacking { get; set; }
    public bool deaf = false;

    public ThingController owner;

    public enum Behaviors
    {
        None,
        StupidWalker,
        FormerHumanTrooper,
        FormerHumanSergeant,
        Imp,
        Pinky,
        Spectre,
        BaronOfHell
    }

    public static BehaviorBase Instantiate(Behaviors behaviorType)
    {
        switch (behaviorType)
        {
            default:
            case Behaviors.None:
                return null;

            case Behaviors.StupidWalker:
                return new StupidWalkerBehavior();

            case Behaviors.FormerHumanTrooper:
                return new FormerHumanTrooperBehavior();

            case Behaviors.FormerHumanSergeant:
                return new FormerHumanSergeantBehavior();

            case Behaviors.Imp:
                return new ImpBehavior();

            case Behaviors.Pinky:
                return new PinkyBehavior();

            case Behaviors.Spectre:
                return new SpectreBehavior();

            case Behaviors.BaronOfHell:
                return new BaronBehavior();
        }
    }
}
