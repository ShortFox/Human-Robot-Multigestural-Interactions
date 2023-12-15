using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class PersistentData : MonoBehaviour
    {
        public string SelectedBelief { get { return Settings.Belief; }}

        private Transform _head;
        public Vector3 HeadPosition { get { return _head.position; }}
        public Quaternion HeadRotation { get { return _head.rotation; }}

        private EyeTrackerRecorder _recorder;
        public EyeTrackerRecorder Recorder { get { return _recorder; }}

        // Start is called before the first frame update
        void Awake()
        {
            GameObject headset = GameObject.FindWithTag("XR Rig");
            _head = headset.transform.Find("Camera Offset/Main Camera");
            _recorder = headset.GetComponent<EyeTrackerRecorder>();
        }
    }
}
