using System;
using System.Collections.Generic;
using UnityEngine;
using Tobii.G2OM;

namespace MQ.MultiAgent
{
    public class ReactiveCube : MonoBehaviour, IGazeFocusable
    {
        public event EventHandler<CubeTriggerEventArgs> BoxCollisionTrigger;
        public event EventHandler BoxGazeTrigger;

        [HideInInspector]
        public bool Focused;

        [HideInInspector]
        public bool InContact;

        public void Start()
        {
        }

        //The method of the "IGazeFocusable" interface, which will be called when this object receives or loses focus
        public void GazeFocusChanged(bool hasFocus)
        {
            Focused = hasFocus;
            BoxGazeTrigger?.Invoke(this, EventArgs.Empty);
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.GetComponent<CubeTrigger>() != null)
            {
                BoxCollisionTrigger?.Invoke(this, new CubeTriggerEventArgs(other.gameObject, true));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.GetComponent<CubeTrigger>() != null)
            {
                BoxCollisionTrigger?.Invoke(this, new CubeTriggerEventArgs(other.gameObject, false));
            }
        }
    }

    public class CubeTriggerEventArgs : EventArgs
    {
        public GameObject hitObject { get; set; }

        public bool enterEvent;

        public CubeTriggerEventArgs(GameObject obj, bool enter)
        {
            hitObject = obj;
            enterEvent = enter;
        }
    }
}
