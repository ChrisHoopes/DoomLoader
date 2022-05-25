using UnityEngine;

public class Door1Controller : MonoBehaviour, Pokeable
{
    public SlowRepeatableDoorController sectorController;

    public int requiresKeycard = -1;

    public bool Poke(GameObject caller)
    {
        if (requiresKeycard != -1)
        {
            PlayerInfo p = caller.GetComponent<PlayerInfo>();
            if (p == null)
                return false;

            if (!p.Keycards[requiresKeycard])
                return false;
        }

        if (sectorController.CurrentState != SlowRepeatableDoorController.State.Open &&
            sectorController.CurrentState != SlowRepeatableDoorController.State.Opening)
        {
            sectorController.CurrentState = SlowRepeatableDoorController.State.Opening;
            sectorController.waitTime = 4;
            return true;
        }

        return false;
    }

    public bool AllowMonsters()
    {
        return requiresKeycard == -1;
    }
}