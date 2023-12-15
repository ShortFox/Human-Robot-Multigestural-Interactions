using System;
using UnityEngine;

namespace MQ.MultiAgent
{
    /// <summary>Base class for avatar controllers.
    public abstract class AvatarInputBase: MonoBehaviour
    {
        protected Transform headPose;
        protected Transform gazeTarget;
        protected Transform indexFinger;

        public event EventHandler Started;
        public event EventHandler Stopped;
        public event EventHandler Error;

        protected virtual void onStarted()
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void onStopped()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void onError()
        {
            Error?.Invoke(this, EventArgs.Empty);
        }

        public abstract void CenterPlayer();    //Center Player.
        public abstract bool IsRunning();

        public virtual void SetUp() {}
        public virtual void Initiate() {}
        public virtual void Run() {}
        public virtual void Reset() {}

        protected virtual void Awake()
        {
            var avatarTargets = transform.Find("AvatarTargets")?.gameObject;

            if (avatarTargets == null)
            {
                Debug.LogError("Missing `AvatarTargets` prefab. Couldn't find a gameobject named `AvatarTargets` in this avatar.");
            }

            headPose = avatarTargets.transform.Find("HeadPose")?.gameObject?.transform;

            if (headPose == null)
            {
                Debug.LogError("Missing `HeadPose` gameObject in `AvatarTargets` prefab.");
            }

            gazeTarget = avatarTargets.transform.Find("GazeTarget")?.gameObject?.transform;

            if (gazeTarget == null)
            {
                Debug.LogError("Missing `GazeTarget` gameObject in `AvatarTargets` prefab.");
            }

            indexFinger = avatarTargets.transform.Find("IndexFinger")?.gameObject?.transform;

            if (indexFinger == null)
            {
                Debug.LogError("Missing `IndexFinger` gameObject in `AvatarTargets` prefab.");
            }
        }
    }
}