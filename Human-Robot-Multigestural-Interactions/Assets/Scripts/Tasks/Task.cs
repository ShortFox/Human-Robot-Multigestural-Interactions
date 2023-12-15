namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class Task : MonoBehaviour
    {
        public string Name;                 //Name of the task.

        private TaskPhase _phase = TaskPhase.Start;

        protected Coroutine _currentCoroutine;

        protected TaskPhase Phase {
            set
            {
                _phase = value;
                switch (_phase)
                {
                    case TaskPhase.Start:
                        _currentCoroutine = StartCoroutine(SetUp());
                        break;
                    case TaskPhase.Initiate:
                        _currentCoroutine = StartCoroutine(Initiate());
                        break;
                    case TaskPhase.Run:
                        _currentCoroutine = StartCoroutine(Run());
                        break;
                    case TaskPhase.Done:
                        _currentCoroutine = StartCoroutine(Done());
                        break;
                    case TaskPhase.Finished:
                        _currentCoroutine = StartCoroutine(Finished());
                        break;
                    default:
                        Debug.LogError("Error. ExperimentPhase Not What Expected. Value received: " + Phase);
                        break;
                }
            }
            get
            {
                return _phase;
            }
        }

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

        public void Initialize(TaskType task)
        {
            switch (task)
            {
                case TaskType.Box:
                    Name = "Box Task";
                    break;
                case TaskType.Herd:
                    Name = "Herd Task";
                    break;
                default:
                    Debug.LogError("Error: Task not from list of available in program.");
                    break;
            }
            enabled = true;         //Enable component following initialization.
        }

        protected virtual void Start()
        {
            Phase = TaskPhase.Start;
            enabled = false;        //Disable on start-up.
        }

        public abstract IEnumerator SetUp();               //Set up experiment
        public abstract IEnumerator Initiate();            //Set up task.
        public abstract IEnumerator Run();                 //Run task.
        public abstract IEnumerator Done();                //Clean up task.
        public abstract IEnumerator Finished();            //Finish experiment.
    }

    public enum TaskPhase
    {
        Start,
        Initiate,
        Run,
        Done,
        Finished,
    }

}
