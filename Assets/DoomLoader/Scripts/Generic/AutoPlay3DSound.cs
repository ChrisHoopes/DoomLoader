using UnityEngine;

namespace DoomLoader
{
    public class AutoPlay3DSound : MonoBehaviour 
    {
        public string soundName;
        public float minDistance;

        void Start()
        {
            GameManager.Create3DSound(transform.position, soundName, minDistance);
        }
    }
}
