using UnityEngine;

public class Door31Controller : MonoBehaviour, Pokeable
{
    public SlowOneshotDoorController sectorController;

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

        if (sectorController.CurrentState == SlowOneshotDoorController.State.Closed)
        {
            sectorController.CurrentState = SlowOneshotDoorController.State.Opening;
            return true;
        }

        return false;
    }

    public bool AllowMonsters()
    {
        return false;
    }
}
