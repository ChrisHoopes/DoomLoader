using UnityEngine;

namespace DoomLoader
{
    public interface Pokeable
    {
        bool Poke(GameObject caller);
        bool AllowMonsters();
    }
}
