using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MQ.MultiAgent.Box
{
    public abstract class BoxTask : Task
    {
        public static BoxTask Instance {get; private set;}
        public BoxTaskComponents BoxTaskComp;

        public GameObject TaskMessage;

        protected List<AvatarInputBase> controllers;

        private GameObject VRHeadset;

        protected GameObject MyPlayer;

        protected VRInput PlayerInput;

        protected GameObject PartnerPlayer;

#region Box Task Related Information
        public Transform TargetObj { get { return _targetObj.transform; } }

    #region Data Structures
        protected TrialInfo _trialInfo;
        public TrialInfo CurrentTrialInfo
        {
            get { return _trialInfo; }
        }
    #endregion

    #region CommonTask Objects
        //Need to find these items in script.
        protected GameObject FixationObject { get { return BoxTaskComp.FixationObject; } }
        public GameObject DividingWall { get { return BoxTaskComp.DividingWall; } }
        protected GameObject[] Blocks { get { return BoxTaskComp.Blocks; } }
        private Text[] BlockFaces { get { return BoxTaskComp.BlockFaces; } }
        protected string _blockObjectsTag = "BlockObjects";
        protected Material UnselectedColor { get { return BoxTaskComp.UnselectedColor; } }
        protected Material CueColor { get { return BoxTaskComp.CueColor; } }
        protected Material IncorrectColor { get { return BoxTaskComp.IncorrectColor; } }
        protected Material CorrectColor { get { return BoxTaskComp.CorrectColor; } }
        protected Material Black { get { return BoxTaskComp.Black; } }

        private GameObject[] _readyCubes;
        protected GameObject[] ReadyCubes
        {
            get
            {
                if (_readyCubes == null)
                {
                    _readyCubes = new GameObject[] {
                        MyHandInitCube,
                        OtherHandInitCube,
                    };
                }
                return _readyCubes;
            }
        }
    #endregion

    #region ReachTask Objects
        protected GameObject MyHandInitCube;
        protected bool myInitCubeTouched = false;
        protected GameObject MyHand;
        protected GameObject OtherHandInitCube;
        private bool otherInitCubeTouched = true;
        protected GameObject OtherHand;

    #endregion

    #region Experiment Related and Misc.
        //Time before new trial is presented.
        private float _interTrialPeriod = 1f;

        //Feedback time once participant makes response.
        protected float _trialFeedbackLength = 0.5f;

        //Maximum trial time length before timeout.
        [SerializeField]
        [Tooltip("Maximum time allowed for each trial (s).")]
        protected float _trialTimeoutLength = 10.0f;

        //Time that needs to pass before trial initiates.
        private float _trialInitLag { get { return UnityEngine.Random.Range(1f, 2f); } }

        //Distance from Hand's initial position in ReachTask to register as intended reach.
        protected float _intentDistance = 0.05f;

        //Target object for trial.
        protected GameObject _targetObj;

        //Dictionary for easy access to change block faces.
        protected Dictionary<TrialInfoHeader, Text> _blockFaceDict;

        //List that stores shuffled trials.
        protected List<TrialInfo> Trials;

        //Number of trials resulting in incorrect response;
        protected int _errorNumber = 0;
    #endregion
#endregion

#region Override Fields and Properties
        protected int _trialNum;
        protected int _blockNum;

        //Unique identifier for trial
        public override string TrialID { get { return _trialInfo.TrialID.ToString(); } }

        //Current trial index.
        public override int TrialNum { get { return _trialNum; } }

        //Number of trials.
        public int ExperimentLen { get { return Trials != null ? Trials.Count : 0; }}

        //Block num.
        public override int BlockNum { get { return _blockNum; } }

#endregion

#region Exposed experiment data
        public string TargetObject
        {
            get
            {
                return _targetObj != null ? _targetObj.name : "Not selected";
            }
        }

        public string TargetObjectNum
        {
            get
            {
                return _targetObj != null ? _targetObj.name.Substring(_targetObj.name.Length - 1) : "Not selected";
            }
        }

        public Vector3 TargetPosition
        {
            get
            {
                return _targetObj != null ? _targetObj.transform.position : Vector3.zero;
            }
        }

        public string SelectedBelief { get { return Settings.Belief; } }

#endregion

#region Event Related
        Dictionary<GameObject, GameObject> lastContactObjects;
        GameObject lastGazedObject;

#endregion

#region Override Methods

        /// <summary>
        /// The awake method is responsible for looking for and making accessible all objects for this task.
        /// All configuration that is required for derivates of this task (specific parameters, recording setup...)
        /// should be set overriding methods in the relevant subclasses.
        /// </summary>
        public virtual void Awake()
        {
            Instance = this;

            VRHeadset = GameObject.FindWithTag("XR Rig");

            MyPlayer = AvatarInfo.MainPlayer;
            MyHandInitCube = MyPlayer.transform.Find("FingerCubeReady").gameObject;
            MyHand = MyPlayer.transform.Find("AvatarTargets/IndexFinger").gameObject;
            PlayerInput = MyPlayer.GetComponent<VRInput>();

            PartnerPlayer = AvatarInfo.PartnerPlayer;
            OtherHandInitCube = PartnerPlayer.transform.Find("FingerCubeReady").gameObject;
            OtherHand = PartnerPlayer.transform.Find("AvatarTargets/IndexFinger").gameObject;

            // Avatar movement controller setup
            controllers = new List<AvatarInputBase>{
                MyPlayer.GetComponent<AvatarInputBase>(),
                PartnerPlayer.GetComponent<AvatarInputBase>()
            };

            if (AvatarInfo.ShouldRotateScene)
            {
                BoxTaskComp.transform.Rotate(0, 180, 0);
            }
        }

        protected IEnumerator LoadTrialsAsync()
        {
            // Trial setup
            // Trial list needs to run asynchronously to avoid freezing the application.
            var t = System.Threading.Tasks.Task.Run(() => {
                LoadTrials();
            });
            yield return new WaitUntil(() => t.IsCompleted);
        }

        protected virtual void LoadTrials()
        {
            Trials = Settings.Trials;
            _trialNum = Settings.TrialIdx;
        }

        protected virtual void InitRecording() { }

        protected virtual void LoadNextScene() { }

        public override IEnumerator SetUp()
        {
            TurnOn(DividingWall);
            LoadingIndicator.Instance.SetAnimation(true);
            _trialNum = 0;
            yield return StartCoroutine(LoadTrialsAsync());

            if (Trials.Count == 0)
            {
                Phase = TaskPhase.Finished;
            }

            // Setup init cube callbacks
            MyHandInitCube.GetComponent<ReactiveCube>().BoxCollisionTrigger += (sender, args) => {
                if (args.hitObject == MyHand)
                {
                    myInitCubeTouched = args.enterEvent;
                }
            };

            OtherHandInitCube.GetComponent<ReactiveCube>().BoxCollisionTrigger += (sender, args) => {
                if (args.hitObject == OtherHand)
                {
                    otherInitCubeTouched = args.enterEvent;
                }
            };

            // Task component setup
            BoxTaskComp = GameObject.Find("[Box Task Components]").gameObject.GetComponent<BoxTaskComponents>();

            _blockFaceDict = new Dictionary<TrialInfoHeader, Text>
            {
                { TrialInfoHeader.B1_val_A, BlockFaces[0] },
                { TrialInfoHeader.B2_val_A, BlockFaces[1] },
                { TrialInfoHeader.B3_val_A, BlockFaces[2] },
                { TrialInfoHeader.B3_val_B, BlockFaces[3] },
                { TrialInfoHeader.B2_val_B, BlockFaces[4] },
                { TrialInfoHeader.B1_val_B, BlockFaces[5] },
            };

            // Setup Block callbacks
            foreach (GameObject block in Blocks)
            {
                ReactiveCube cube = block.GetComponent<ReactiveCube>();
                cube.BoxCollisionTrigger += (sender, args) => OnBoxTouch((ReactiveCube)sender, args);
                cube.BoxGazeTrigger += (sender, args) => OnGazeTrigger((ReactiveCube)sender);
            }
            // Add extra callback to mark collision for AI (in case trajectory ends and didn't touch the collider)
            var ai = PartnerPlayer.GetComponent<ReplayInput>();
            if (ai != null)
            {
                ai.ReplayEnded += delegate {
                    lastContactObjects[OtherHand] = _targetObj;
                };
            }

            foreach(AvatarInputBase controller in controllers)
            {
                controller.SetUp();
            }

            GameObject.FindWithTag("XRUI")?.GetComponent<LoadingMessage>()?.HideMessage();
            PartnerPlayer.SetActive(true);

            float delay = RandomGaussian.Sample(1.0f, 0.5f, 3.5f);
            yield return new WaitForSeconds(delay);
            LoadingIndicator.Instance.SetAnimation(false);

            while (TaskMessage.activeSelf) {
                yield return new WaitForSeconds(0.2f);
            }

            Phase++;
            yield break;
        }

        public override IEnumerator Initiate()
        {

            // Activate task components
            BoxTaskComp.gameObject.SetActive(true);

            myInitCubeTouched = false;
            otherInitCubeTouched = true;

            lastGazedObject = null;
            lastContactObjects = new Dictionary<GameObject, GameObject>();

            _trialInfo = Trials[_trialNum];


            // Initialize controllers
            foreach(AvatarInputBase controller in controllers)
            {
                if (controller is ReplayInput ai_controller)
                {
                    ai_controller.SetTrialInfo(_trialInfo);
                }
                controller.Initiate();
            }

            /// Task components
            TurnOn(DividingWall);
            TurnOn(ReadyCubes);
            TurnOff(Blocks);
            ///

            // Pre-task initialization behavior.
            float timeElapsed = 0;
            while (timeElapsed < _trialInitLag)
            {
                if (InInitPosition())
                {
                    timeElapsed += Time.deltaTime;
                }
                else
                {
                    timeElapsed = 0;
                }
                yield return null;
            }

            /// Task components
            ChangeColor(MyHandInitCube, CorrectColor);
            TurnOn(FixationObject);
            ///

            GazeFocusable fixation = FixationObject.GetComponent<GazeFocusable>();
            while (!fixation.Focused)
            {
                yield return null;
            }

            // 500 msec delay before trial starts. This is to give time for participant to view the "Ready" Fixation
            timeElapsed = 0;
            while (timeElapsed < 0.5f)
            {
                timeElapsed += Time.deltaTime;
                yield return null;
            }


            Phase++;
            yield return null;
        }

        public override IEnumerator Run()
        {
            yield return null;
        }

        public override IEnumerator Done()
        {
            /// Task components
            TurnOn(DividingWall);
            ResetBlockFaces();
            ChangeColor(Blocks, UnselectedColor);
            ChangeColor(MyHandInitCube, Black);
            ///

            yield return new WaitForSeconds(_interTrialPeriod);

            foreach(AvatarInputBase controller in controllers)
            {
                controller.Reset();
            }

            _trialNum++;



            if (_trialNum < Trials.Count)
            {
                Phase = TaskPhase.Initiate;
            }
            else
            {
                Phase++;
            }
            yield return null;
        }

        public override IEnumerator Finished()
        {
            /// Task components
            TurnOff(DividingWall);
            TurnOff(FixationObject);
            TurnOn(Blocks);
            TurnOn(ReadyCubes);
            ///

            PartnerPlayer.GetComponent<ReplayInput>()?.DeactivateController();
            BoxTaskComp.gameObject.SetActive(false);

            LoadNextScene();

            yield return null;
        }
#endregion

#region Tasks

        protected void InitTask()
        {
            //Select target object.
            _targetObj = Blocks[_trialInfo.TargetLocation - 1];

            TurnOff(DividingWall);
            TurnOff(FixationObject);
            TurnOn(Blocks);

            InitRecording();
        }

        #endregion

        #region Helper Methods

        protected IEnumerator TimeoutCheck()
        {
            float startTime = Time.timeSinceLevelLoad;
            while(Time.timeSinceLevelLoad-startTime < _trialTimeoutLength)
            {
                yield return null;
            }
            StopCoroutine(_currentCoroutine);
            // Provide Feedback to Participants if trial Times out
            ChangeColor(_targetObj, IncorrectColor);
            if (Phase < TaskPhase.Done)
            {
                Phase = TaskPhase.Done;
            }
        }

        protected void SetBlockFaces()
        {
            string[] trialArray = _trialInfo.Record();
            foreach (var item in _blockFaceDict)
            {
                item.Value.text = trialArray[(int)item.Key];
            }
        }

        void ResetBlockFaces()
        {
            foreach (var item in _blockFaceDict.Values)
            {
                item.text = "";
            }
        }

        protected void TurnOff(GameObject[] objects)
        {
            foreach (GameObject obj in objects) TurnOff(obj);
        }

        public void TurnOff(GameObject obj)
        {
            obj.SetActive(false);
        }

        protected void TurnOn(GameObject[] objects)
        {
            foreach (GameObject obj in objects) TurnOn(obj);
        }

        protected void TurnOn(GameObject obj)
        {
            obj.SetActive(true);
        }

        protected void ChangeColor(GameObject obj, Material NewColor)
        {
            obj.GetComponent<Renderer>().material = NewColor;
        }

        protected void ChangeColor(GameObject[] objects, Material NewColor)
        {
            foreach (GameObject obj in objects) ChangeColor(obj, NewColor);
        }
        #endregion

        protected void CheckPlayerIntent()
        {
            //Wait for hand hit on a block object.
            Vector3 initPos = PlayerInput.polhemusTransform.position;
            if (Vector3.Distance(initPos, PlayerInput.polhemusTransform.position) > _intentDistance)
            {
                Phase = TaskPhase.Initiate;
            }
        }

        /// <summary>Handle touch events by storing the last hit object and who hit it.</summary>
        void OnBoxTouch(ReactiveCube sender, CubeTriggerEventArgs e)
        {
            lastContactObjects[e.hitObject] = sender.gameObject;
        }

        /// <summary>
        ///     Store gaze trigger events. Unlike with collision triggers, this can only be done by MyPlayer, so
        ///     there is no need to store who was the triggering object.
        /// </summary>
        void OnGazeTrigger(ReactiveCube sender)
        {
            lastGazedObject = sender.gameObject;
        }

        protected GameObject GetLastContactedCube(GameObject obj)
        {
            GameObject cube;
            return lastContactObjects.TryGetValue(obj, out cube) ? cube : null;
        }

        protected GameObject GetLastGazedCube()
        {
            return lastGazedObject;
        }

        private bool InInitPosition()
        {
            return myInitCubeTouched && otherInitCubeTouched;
        }
    }
}
