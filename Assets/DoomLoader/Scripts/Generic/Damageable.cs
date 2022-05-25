using UnityEngine;

public enum DamageType
{
    Generic,
    ImpFire,
    BaronFire,
    Explosion,
    Environment
}

public interface Damageable
{
    bool Dead { get; }
    bool Bleed { get; }
    void Damage(int amount, DamageType damageType = DamageType.Generic, GameObject attacker = null);
    void Impulse(Vector3 direction, float force);
}