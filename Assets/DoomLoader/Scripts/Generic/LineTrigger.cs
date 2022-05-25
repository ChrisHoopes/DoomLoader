using System;
using UnityEngine;

namespace DoomLoader
{
    public class LineTrigger : MonoBehaviour 
    {
        public Action<Collider> TriggerAction;

        void OnTriggerEnter(Collider other)
        {
            TriggerAction.Invoke(other);
        }
    }
}
