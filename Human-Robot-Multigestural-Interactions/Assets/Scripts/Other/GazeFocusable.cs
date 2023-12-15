using UnityEngine;
using Tobii.G2OM;

namespace MQ.MultiAgent
{
    public class GazeFocusable : MonoBehaviour, IGazeFocusable
    {
        public bool Focused;

        //The method of the "IGazeFocusable" interface, which will be called when this object receives or loses focus
        public void GazeFocusChanged(bool hasFocus)
        {
            Focused = hasFocus;
        }
    }
}

