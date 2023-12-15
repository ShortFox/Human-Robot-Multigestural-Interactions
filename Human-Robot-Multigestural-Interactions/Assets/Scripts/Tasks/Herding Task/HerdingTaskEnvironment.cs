namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    //Take from Matt's study
    //Use properties to auto find components .. give debug logerror if can't find.
    public class HerdingTaskEnvironment : TaskController
    {
        public static HerdingTaskEnvironment Instance { get; private set; }
        public HerdControl HerdFunctions;
        private SyncExperimentPhase SYNC;

        private Transform[] _readyCubes;
        private Transform[] ReadyCubes
        {
            get
            {
                if (_readyCubes == null)
                {
                    _readyCubes = GetTransforms(GameObject.FindGameObjectsWithTag("InitCube"));
                }
                return _readyCubes;
            }
        }

        private Transform[] GetTransforms(GameObject[] objs)
        {
            Transform[] output = new Transform[objs.Length];

            for (int i = 0; i < objs.Length; i++)
            {
                output[i] = objs[i].transform;
            }
            return output;
        }

        #region Private Fields
        float _trialLength = 60f;
        float _experimentLength = 30f * 60f;
        float _experimentStartTime;
        float _trialsLeft = 6;


        string _taskName;
        string _trialID;
        int _trialNum;
        int _blockNum;

        bool _partnerVisible;

        #endregion

        #region Properties
        public override string TaskName { get { return _taskName; } }            //Name of task
        public override string TrialID { get { return _trialID; } }             //Unique identifier for trial
        public override int TrialNum { get { return _trialNum; } }               //Trial num.
        public override int BlockNum { get { return _blockNum; } }               //Block num.

        public override bool PartnerVisible {get { return _partnerVisible; }}
        #endregion

        public HerdingTaskEnvironment(Task Environment) : base(Environment)
        {
            Instance = this;

            _taskName = "HerdTask";
            _trialID = "7Sheep";
            _blockNum = 0;
            _trialNum = 0;      //0 because trialNum will increase when trial initiates.
            GameObject obj = GameObject.Find("[Tasks Components]");
            obj.transform.Find("[Herding Task Components]").gameObject.SetActive(true);

            SYNC = SyncExperimentPhase.Instance;
            //SYNC.TaskEnvironment.MyTask.Done();     //Forces sheep to disapear.
            HerdFunctions = HerdControl.Instance;

            Debug.Log("Implemented HerdingTask");
        }

        public override string Header
        {
            get
            {
                string output = "";

                output += "TaskName, TrialID, TrialNum, BlockNum,Contained,UnityTime,";
                output += HerdFunctions.GetHeader();
                return output;
            }
        }

        public override string LatestDataString()
        {
            string output = "";

            output += TaskName + "," + TrialID + "," + TrialNum.ToString() + "," + BlockNum.ToString() + "," + HerdFunctions.Contained.ToString() + "," + Time.timeSinceLevelLoad + ",";
            output += HerdFunctions.LatestDataString();

            return output;
        }

        public override void EventsListen()
        {
            OnInitiateTrial += SetUp;
            OnRunTrial += RunTrial;
            OnDoneTrial += CleanUp;
        }

        public override void EventsStopListening()
        {
            OnInitiateTrial -= SetUp;
            OnRunTrial -= RunTrial;
            OnDoneTrial -= CleanUp;
        }

        #region Override Methods
        public override void Start()
        {
            throw new System.NotImplementedException();
        }
        public override void Initiate()
        {
            RaiseEventInitiateTrial();
            _trialNum++;

            _partnerVisible = false;
            if (_trialNum == 1) _experimentStartTime = Time.timeSinceLevelLoad;

            //Run();
        }

        public override void Run()
        {
            RaiseEventRunTrial();
            _partnerVisible = true;
        }

        public override void Done()
        {
            RaiseEventDoneTrial();
            _partnerVisible = false;
        }
        public override void Finished()
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Event Methods
        private void SetUp()
        {
            if (CurrentCoroutine != null) Environment.StopCoroutine(CurrentCoroutine);
            CurrentCoroutine = IWaitForStart();
            Environment.StartCoroutine(CurrentCoroutine);
        }

        private void RunTrial()
        {
            if (CurrentCoroutine != null) Environment.StopCoroutine(CurrentCoroutine);
            CurrentCoroutine = IRunTrial();
            Environment.StartCoroutine(CurrentCoroutine);
        }

        private void CleanUp()
        {
            if (CurrentCoroutine != null) Environment.StopCoroutine(CurrentCoroutine);
            CurrentCoroutine = IAmDone();
            Environment.StartCoroutine(CurrentCoroutine);
        }
        #endregion

        #region Coroutines
        IEnumerator IWaitForStart()
        {
            HerdFunctions.HideSheep();
            HerdFunctions.ShowField();
            SetTransformsYValue(ReadyCubes, HerdControl.Instance.Field.position.y);             //ReadyCube.position = new Vector3(ReadyCube.position.x, HerdControl.Instance.Field.position.y, ReadyCube.position.z);
            yield return new WaitForSeconds(3f);

            while (!SyncHerdExperiment.Instance.jointReadyTouch)
            {
                yield return null;
            }

            SYNC.ExperimentPhase = 2;
        }

        IEnumerator IRunTrial()
        {
            SetTransformsYValue(ReadyCubes, -100f);                                             //ReadyCube.position = new Vector3(ReadyCube.position.x, -100f, ReadyCube.position.z);

            HerdFunctions.ActivateSheep();

            float timeStart = Time.timeSinceLevelLoad;
            while (Time.timeSinceLevelLoad - timeStart <= _trialLength)
            {
                if (SYNC.ExperimentPhase != 2)
                {
                    Debug.Log("This is the value I have right now: " + SYNC.ExperimentPhase);
                    break;
                }
                yield return null;
            }

            SYNC.ExperimentPhase = 3;
        }
        IEnumerator IAmDone()
        {
            HerdFunctions.HideSheep();
            HerdFunctions.HideField();

            yield return new WaitForSeconds(3f);

            if (HerdControl.Instance.Score > 60 * Mathf.Floor(1 / Time.fixedDeltaTime) * 0.7f)      //Need to check if this works
            {
                _trialsLeft--;
                Debug.Log("Trial success! Trials Left: " + _trialsLeft + " Score: " + HerdControl.Instance.Score + " Score max: " + (_trialLength * Mathf.Floor(1 / Time.fixedDeltaTime)).ToString());
            }
            else
            {
                Debug.Log("Trial failure :(. Trials left: " + _trialsLeft);
            }

            if (((Time.timeSinceLevelLoad - _experimentStartTime) > _experimentLength) || _trialsLeft <= 0)
            {
                SYNC.ExperimentPhase = 4;       //Experiment is over.
            }
            else
            {
                SYNC.ExperimentPhase = 1;       //Go to initiate trial.
            }
        }
        #endregion

        #region Helper Methods
        private void SetTransformsYValue(Transform[] objs, float y_pos)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].position = new Vector3(objs[i].position.x, y_pos, objs[i].position.z);
            }
        }

        #endregion
    }
}
