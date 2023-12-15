using System;
using UnityEngine;

namespace MQ.MultiAgent
{
    /// <summary>Base class for avatar controllers.
    public abstract class PlayerController: MonoBehaviour
    {
        protected Player Self;

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

        public virtual void Initialize(Player self)
        {
            Self = self;
        }

        public abstract void UpdateState();     //Update states of controller.
        public abstract void CenterPlayer();    //Center Player.
        public abstract bool IsRunning();

        public abstract void Initiate();
        public abstract void RunController();
    }
}