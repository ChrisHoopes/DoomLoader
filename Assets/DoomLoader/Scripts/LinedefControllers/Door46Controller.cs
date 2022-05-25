using UnityEngine;

public class Door46Controller : MonoBehaviour, Damageable
{
    public SlowOneshotDoorController sectorController;

    public bool Dead { get { return false; } }
    public bool Bleed { get { return false; } }

    public void Damage(int value, DamageType damageType = DamageType.Generic, GameObject attacker = null)
    {
        if (attacker != null)
            if (attacker.GetComponent<PlayerThing>() != null)
                if (sectorController.CurrentState == SlowOneshotDoorController.State.Closed)
                    sectorController.CurrentState = SlowOneshotDoorController.State.Opening;
    }

    public void Impulse(Vector3 direction, float force) { }
}
