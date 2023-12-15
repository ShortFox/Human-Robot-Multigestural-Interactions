namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class TaskController
    {
        public Task Environment;

        public int Phase = 0;
        protected List<IEnumerator> _currentCoroutines;
        protected List<IEnumerator> CurrentCoroutines
        {
            get
            {
                if (_currentCoroutines == null) _currentCoroutines = new List<IEnumerator>();
                return _currentCoroutines;
            }
        }
        protected IEnumerator CurrentCoroutine;

        //Properties of TaskController.
        public abstract string TaskName { get; }            //Name of task
        public abstract string TrialID { get; }             //Unique identifier for trial
        public abstract int TrialNum { get; }               //Trial num.
        public abstract int BlockNum { get; }               //Block num.

        public abstract bool PartnerVisible { get; }

        public abstract string Header { get; }          //Task-relevent data header.
        public abstract string LatestDataString();     //Task-relevant data.

        public TaskController(Task environment)
        {
            Environment = environment;
        }

        #region Events
        //Event to signal experiment preperation
        public delegate void InitiateExperimentAction();
        public static event InitiateExperimentAction OnInitiateExperiment;

        //Event to signal trial preparation.
        public delegate void InitiateTrialAction();
        public static event InitiateTrialAction OnInitiateTrial;

        //Event to signal trial is currently running.
        public delegate void RunTrialAction();
        public static event RunTrialAction OnRunTrial;

        //Event to signal trial is done.
        public delegate void DoneTrialAction();
        public static event DoneTrialAction OnDoneTrial;

        //Event to signal trial is done.
        public delegate void DoneExperimentAction();
        public static event DoneExperimentAction OnDoneExperiment;
        #endregion

        public abstract void Start();               //Set up experiment
        public abstract void Initiate();            //Set up task.
        public abstract void Run();                 //Run task.
        public abstract void Done();                //Clean up task.
        public abstract void Finished();            //Finish experiment.

        protected void RaiseEventInitiateExperiment()
        {
            OnInitiateExperiment();
        }

        protected void RaiseEventInitiateTrial()
        {
            OnInitiateTrial();
        }

        protected void RaiseEventRunTrial()
        {
            OnRunTrial();
        }

        protected void RaiseEventDoneTrial()
        {
            OnDoneTrial();
        }

        protected void RaiseEventFinishExperiment()
        {
            OnDoneExperiment();
        }

        public abstract void EventsListen();             //Set up Event Listeners.
        public abstract void EventsStopListening();      //Close Event Liseners.
        public void SetPhase(int phase)
        {
            Phase = phase;
        }
    }
}
