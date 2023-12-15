using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using RootMotion.FinalIK;

namespace MQ.MultiAgent.Box
{
    public class BoxTaskAIController : MQ.MultiAgent.AIController
    {
        protected Playback_IKSolverLookAt _playbackSolver;
        private GameObject _lowerArmTracker;

        private Task task;

        // TODO: Set up role and playback ID from TrialInfo
        // The role of the AI agent
        protected string role;
        protected string playbackID;

        //Trajectory data
        protected List<PlaybackInfo> _playbackData;

        // Playback Samplerate
        private float playbackSampleRate = 1 / 90f;
        // Holds index of Hand trajectory from file.
        private int _indx = 0;

        private float _lerpRate = 1f;

        public override void Initialize(Player Self)
        {
            base.Initialize(Self);

            if (Self.Eyes.Left != null && Self.Eyes.Right)
            {
                _myGaze.solver = new Playback_IKSolverLookAt
                {
                    head = new IKSolverLookAt.LookAtBone(Self.Eyes.HeadRoot),
                    eyes = new IKSolverLookAt.LookAtBone[2] {
                        new IKSolverLookAt.LookAtBone(Self.Eyes.Left),
                        new IKSolverLookAt.LookAtBone(Self.Eyes.Right)
                    },
                };
            }
            else
            {
                _myGaze.solver = new Playback_IKSolverLookAt
                {
                    head = new IKSolverLookAt.LookAtBone(Self.Eyes.HeadRoot)
                };
            }

            _myGaze.solver.SetLookAtWeight(1f, 0.5f, 1f, 1f);

            //Downcast to access Head_Target and Eyes_Target properties.
            //Need to set the Head and Eyes target directly. GazeTarget won't update.
            _playbackSolver = (Playback_IKSolverLookAt)_myGaze.solver;

            _lowerArmTracker = new GameObject();
            _lowerArmTracker.name = "LowerArmTracker";

            if (_myBody != null && _myBody.enabled)
            {
                _myBody.solver.OnPostUpdate += UpdateLowerArm;
                _myBody.solver.OnPostUpdate += UpdateGazeRay;
            }
        }

        ~BoxTaskAIController()
        {
            _myBody.solver.OnPostUpdate -= UpdateLowerArm;
            _myBody.solver.OnPostUpdate -= UpdateGazeRay;
        }

        private void UpdateLowerArm()
        {
            _lowerArmTracker.transform.position = _myBody.references.rightForearm.position;
            _lowerArmTracker.transform.rotation = _myBody.references.rightForearm.rotation;
        }

        private void UpdateGazeRay()
        {
            if (this.Self.Eyes.Left != null && this.Self.Eyes.Right != null)
            {
                _myGazeOrigin = (this.Self.Eyes.Left.position + this.Self.Eyes.Right.position) / 2;
            }
            _myRay = new Ray(_myGazeOrigin, EyesTarget - _myGazeOrigin);

            Debug.DrawRay(_myGazeOrigin, EyesTarget - _myGazeOrigin, Color.red);

            // Get Ray Information
            RaycastHit hit;
            if (Physics.Raycast(_myRay, out hit))
            {
                GazeTransformName = hit.transform.name;
            }
            else GazeTransformName = "";
        }

        private Player _partnerPlayer;                                             //The other players engaged with task.
        private Player PartnerPlayer
        {
            get
            {
                if (_partnerPlayer == null)
                {
                    if (Self.tag == "Player")   //If this is primary player
                    {
                        GameObject op = GameObject.FindGameObjectWithTag("OtherPlayer");
                        if (op != null)
                        {
                            _partnerPlayer = GameObject.FindGameObjectWithTag("OtherPlayer").GetComponent<Player>();
                        }
                    }
                    else if (Self.tag == "OtherPlayer") //If this is local AI partner.
                    {
                        _partnerPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
                    }
                    else
                    {
                        Debug.LogError("Error: Avatar tag not what expected.");
                    }
                }
                return _partnerPlayer;
            }
        }

        #region Event Arguments
        private EventArguments _handEvent;
        private EventArguments HandEvent
        {
            get
            {
                if (_handEvent == null) _handEvent = Self.HandInitCube.GetComponent<EventArguments>();
                return _handEvent;
            }
        }

        private EventArguments _fixationEvent;
        private EventArguments FixationEvent
        {
            get
            {
                if (_fixationEvent == null)
                {
                    GameObject t = GameObject.Find("FixationObject");
                    _fixationEvent = t.GetComponent<EventArguments>();
                }
                return _fixationEvent;
            }
        }

        private EventArguments[] _boxTouchEvent;
        private EventArguments[] BoxTouchEvent
        {
            get
            {
                if (_boxTouchEvent == null)
                {
                    GameObject[] objs = GameObject.FindGameObjectsWithTag("BlockObjects");
                    _boxTouchEvent = new EventArguments[objs.Length];
                    for (int i = 0; i<objs.Length;i++)
                    {
                        _boxTouchEvent[i] = objs[i].GetComponent<EventArguments>();
                    }

                    Debug.Log("Obtained EventArguments from " + _boxTouchEvent.Length + " Box Objects");
                }
                return _boxTouchEvent;
            }

        }
        #endregion

        #region Private Fields
        protected bool _isTaskRunning;                             //Flag to denote if task is being run.
        public override bool IsRunning() { return _isTaskRunning; }
        private TrialInfo _trialInfo;                            //Holder for current trial information.

        //Used for Playback_IKSolverLookAt
        private Vector3 _headTarget;
        private Vector3 _eyesTarget;
        private Vector3 EyesTarget
        {
            get { return _eyesTarget; }
            set
            {
                _eyesTarget = value;
                GazeLocat = _eyesTarget;
            }
        }

        private Transform _responseTarget;
        private Ray _myRay;
        private Vector3 _myGazeOrigin;

        #endregion

        #region Override Methods
        // Reset AI behaviour.
        public override void ResetController()
        {
            _indx = 0;

            //Security to ensure a coroutine isn't running.
            if (CurrentTrajectoryCoroutines != null && CurrentTrajectoryCoroutines.Count > 0)
            {
                CurrentTrajectoryCoroutines.ForEach(routine => Self.StopCoroutine(routine));
                CurrentTrajectoryCoroutines.Clear();
            }
        }

        // Called when trial is initated.
        public override void InitiateController()
        {
            // Added security to ensure these fields are reset properly
            _responseTarget = null;

            _partnerPlayer = PartnerPlayer;

            ActiveAI = false;
            _isTaskRunning = false;
            Self.IsActive = false;

            ResetController();

            if (!string.IsNullOrEmpty(playbackID))
            {
                _playbackData = TrialInfo.ReadPlaybackFile(playbackID);
            }

        }

        // Compute next state for AI Controller (hand, eyes and head).
        public override void RunController()
        {
            Self.IsActive = true;   //AI should run during trial
            ActiveAI = false;
            _isTaskRunning = true;
            CurrentTrajectoryCoroutines = new List<IEnumerator>();
        }

        // Called when trial is over
        public override void DeactivateController()
        {
            //Security to ensure a coroutine isn't running.
            if (CurrentTrajectoryCoroutines.Count > 0)
            {
                CurrentTrajectoryCoroutines.ForEach(routine => Self.StopCoroutine(routine));
                CurrentTrajectoryCoroutines.Clear();
            }

            //At end of trial, bring hand to initcube position.
            HandTarget = Self.HandInitCube.position;

            // Added security to ensure these fields are reset properly
            _responseTarget = null;
            _isTaskRunning = false;
            task.Done();
        }

        // Next State in Update frame.
        public override void UpdateState()
        {

            if (_isTaskRunning)  // This is true when Trial is running
            {
                // If no Coroutine running, select and play Trajectory.
                if (CurrentTrajectoryCoroutines.Count == 0)
                {
                    PlayTrajectory(_playbackData, _indx);
                }
            }

            //Update Gaze
            _playbackSolver.Head_Target = Vector3.Lerp(_playbackSolver.Head_Target, _headTarget, _lerpRate);
            _playbackSolver.Eyes_Target = Vector3.Lerp(_playbackSolver.Eyes_Target, EyesTarget, _lerpRate);

            //Update Hand
            Self.Hand.Position = Vector3.Lerp(Self.Hand.Position, HandTarget, _lerpRate);
            Self.Hand.Rotation = Quaternion.Lerp(Self.Hand.Rotation,
                                                 Quaternion.Euler(HandTargetRot.x, HandTargetRot.y, HandTargetRot.z),
                                                 _lerpRate);

        }
        #endregion

        #region Event Subscription Methods
        private void SetGazeTarget(Transform obj)
        {
            GazeTarget = obj.transform;
            _headTarget = GazeTarget.position;
            EyesTarget = GazeTarget.position;
        }
        private void SetHandTarget(Transform obj)
        {
            HandTarget = obj.position;
        }

        private void SetResponseTarget(Transform obj)
        {
            _responseTarget = obj;
        }

        public void SetTrialInfo(TrialInfo trial)
        {
            role = trial.RoleB;
            _playbackData = trial.PlaybackData;
        }
        #endregion

        #region Trajectory Playback Methods and Variables
        protected List<IEnumerator> CurrentTrajectoryCoroutines;  // Current playback coroutine being conducted

        private void PlayTrajectory(List<PlaybackInfo> data, int indx)
        {
            _lerpRate = 1f;
            if (CurrentTrajectoryCoroutines == null)
            {
                CurrentTrajectoryCoroutines = new List<IEnumerator>();
            }

            //Security to ensure a coroutine isn't running.
            if (CurrentTrajectoryCoroutines.Count > 0)
            {
                CurrentTrajectoryCoroutines.ForEach(routine => Self.StopCoroutine(routine));
                CurrentTrajectoryCoroutines.Clear();
            }

            if (role == "Initiator")
            {
                CurrentTrajectoryCoroutines.Add(RunInitiatorTrajectory(data, indx));
            }
            else if (role == "Responder")
            {
                CurrentTrajectoryCoroutines.Add(WaitForInitiator(data, indx));
            }
            else
            {
                Debug.LogError("Error: AI Role not what is expected");
            }
            if (CurrentTrajectoryCoroutines.Count > 1) Debug.LogError("More coroutines in list than expected");
            Self.StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);
        }

        //If this AI is initiator, then play the initiator file.
        private IEnumerator RunInitiatorTrajectory(List<PlaybackInfo> data, int startIndx)
        {
            //Debug.Log("Starting Initiator Behavior");
            float startTime = Time.timeSinceLevelLoad;

            //This while loop will run until coroutine is killed.
            for (int idx = startIndx; idx < data.Count; idx++)
            {
                if (Time.timeSinceLevelLoad - startTime >= playbackSampleRate)
                {
                    PlaybackInfo step = data[idx];
                    RunHandTrajectory(step);
                    RunEyesTrajectory(step);
                    startTime = Time.timeSinceLevelLoad;
                }

                _indx = idx;
                yield return null;
            }

            _isTaskRunning = false;

            onStopped();
            yield break;
        }

        //Randomly select a trajectory from cube subfolder which will then be lerped.
        private IEnumerator WaitForInitiator(List<PlaybackInfo> data, int startIndx)
        {
            float startTime = Time.timeSinceLevelLoad;

            //This will loop until movement is about to be initialized, or if this coroutine is killed
            for (int idx = startIndx; idx < data.Count; idx++)
            {
                if (Time.timeSinceLevelLoad - startTime >= playbackSampleRate)
                {
                    PlaybackInfo step = data[idx];
                    RunHandTrajectory(step);
                    RunEyesTrajectory(step);
                    startTime = Time.timeSinceLevelLoad;
                }

                if (data[idx].Moving) break;

                _indx = idx;
                yield return null;
            }

            onStopped();
            _isTaskRunning = false;
            yield break;
        }

        private void AlertResponse(Transform cube)
        {
            _lerpRate = 0.15f;

            // Stop any current coroutine
            if (CurrentTrajectoryCoroutines.Count > 0)
            {
                CurrentTrajectoryCoroutines.ForEach(routine => Self.StopCoroutine(routine));
                CurrentTrajectoryCoroutines.Clear();
            }

            CurrentTrajectoryCoroutines.Add(RunResponderTrajectory(cube));
            Self.StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);
        }

        //Randomly select a trajectory from cube subfolder which will then be lerped.
        private IEnumerator RunResponderTrajectory(Transform cube)
        {
            float delay = RandomGaussian.Sample(1f, 0.1f, 3.5f);
            yield return new WaitForSeconds(delay);

            //Convert Cube to TargetLocation Number
            string cubeLocation = cube.name.Substring(cube.name.Length-1);
            int cubeNumber = int.Parse(cubeLocation);

            //Check if Initiator pointed to intended object
            if (_playbackData[0].TargetLocation != cubeNumber)
            {
                cubeLocation = "Cube" + cubeNumber.ToString();

                //Correct object is not selected, need to call new response trajectory

                // Reset index
                _indx = 0;

                _playbackData = ReadResponderFile(cubeLocation);
            }

            // Move index to where file is "moving"
            for (int i = 0; i < _playbackData.Count; i++)
            {
                if (_playbackData[i].Moving)
                {
                    _indx = i;
                    break;
                }
            }

            //Play responder trial.
            CurrentTrajectoryCoroutines.Add(RunInitiatorTrajectory(_playbackData, _indx));
            Self.StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);
        }

        private void RunHandTrajectory(PlaybackInfo info)
        {
            try { HandTarget = info.HandPos; } catch { }
            try { HandTargetRot = info.HandRot; } catch { }

            //Flip values if trajectory is not on same side as this AI player.

            if (Mathf.Sign(info.HeadPosZ) * Self.Side == 1)
            {
                HandTarget.x *= -1;
                HandTarget.z *= -1;
                HandTargetRot.y += 180;
            }

            return;
        }

        private void RunEyesTrajectory(PlaybackInfo info)
        {

            try { _myEyeController.GazeValid = info.GazeValid; } catch { }  //Check if this is closing AI's eyes.

            if (!(float.IsNaN(info.HeadTarget.x) || float.IsNaN(info.HeadTarget.y) || float.IsNaN(info.HeadTarget.z)))
            {
                try { _headTarget = info.HeadTarget; } catch { }
            }

            if (!(float.IsNaN(info.GazePosX) || float.IsNaN(info.GazePosY) || float.IsNaN(info.GazePosZ)))
            {
                try { if (_myEyeController.GazeValid) EyesTarget = info.GazePos; } catch { }
            }

            //Flip values if trajectory is not on same side as this AI player.
            if (Mathf.Sign(info.HeadPosZ) * Self.Side == 1)
            {
                _headTarget.x *= -1;
                _eyesTarget.x *= -1;
                _headTarget.z *= -1;
                _eyesTarget.z *= -1;
                EyesTarget = _eyesTarget;
            }

            return;
        }

        private void LookAtPartner()
        {
            //Debug.Log("Looking at partner");
            OtherPlayer partner = new OtherPlayer(GameObject.FindGameObjectWithTag("Player"));

            CurrentTrajectoryCoroutines.Add(RunDMP_Head(_headTarget,partner.Head));
            Self.StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);

            CurrentTrajectoryCoroutines.Add(RunDMP_Eyes(EyesTarget, partner.Head));
            Self.StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);
        }

        #region DMP Control
        // System variables for integration
        private float dt { get { return Time.deltaTime; } }             //timestep for integration

        //State Dynamics Parameters
        private float damping = 10f;                                    //Velocity dampening term
        private float stiffness = 25f;                                  //Velocity stiffness term

        private float damping_eyes = 16f;                                    //Velocity dampening term
        private float stiffness_eyes = 64f;                                  //Velocity stiffness term

        //Run DMP Model for Hand
        private IEnumerator RunDMP_Hand(Hand effector, Vector3 target)
        {
            Vector3 Rt = effector.Position;                         //Position
            Vector3 dRdt = effector.Velocity;                       //Velocity
            Vector3 dRvdt = Vector3.zero;                           //Acceleration

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                Vector3 noise = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f));
                dRvdt = -damping * dRdt - stiffness * (Rt - target) + noise;

                Rt = Rt + (dRdt * dt);
                dRdt = dRdt + (dRvdt * dt);

                HandTarget = Rt;
                yield return null;
            }
        }

        //Run DMP Model for Eyes/Head
        private IEnumerator RunDMP_Eyes(Vector3 initPos, Transform target)
        {
            Vector3 Rt = initPos;                                   //Position
            Vector3 dRdt = Vector3.zero;                            //Velocity
            Vector3 dRvdt = Vector3.zero;                           //Acceleration

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                Vector3 noise = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f));
                dRvdt = -damping_eyes * dRdt - stiffness_eyes * (Rt - target.position) + noise;

                Rt = Rt + (dRdt * dt);
                dRdt = dRdt + (dRvdt * dt);

                EyesTarget = Rt;
                _myEyeController.GazeValid = true;

                yield return null;
            }
        }

        private IEnumerator RunDMP_Head(Vector3 initPos, Transform target)
        {
            Vector3 Rt = initPos;                                   //Position
            Vector3 dRdt = Vector3.zero;                            //Velocity
            Vector3 dRvdt = Vector3.zero;                           //Acceleration

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                Vector3 noise = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f),
                                            UnityEngine.Random.Range(-1.0f, 1.0f));
                dRvdt = -damping * dRdt - stiffness * (Rt - target.position) + noise;

                Rt = Rt + (dRdt * dt);
                dRdt = dRdt + (dRvdt * dt);

                _headTarget = Rt;

                yield return null;
            }
        }
        #endregion

        #region Appropriate Responder Trajectory Selection
        private List<PlaybackInfo> ReadResponderFile(string targetCube)
        {
            DirectoryInfo directory = new DirectoryInfo(
                Application.streamingAssetsPath + "/Trajectories/Responder/" + targetCube);

            FileInfo[] trajectFiles = directory.GetFiles("*.csv");  // File Information

            // Select a file randomly in sub-folder
            string file = trajectFiles[UnityEngine.Random.Range(0,trajectFiles.Length)].FullName;  //Get full path name of trajectory

            List<PlaybackInfo> output = new List<PlaybackInfo>();

            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();          //Remove header.
                while (!sr.EndOfStream)
                {
                    output.Add(new PlaybackInfo(sr.ReadLine().Split(',')));
                }
            }
            return output;
        }

        public override void Initiate()
        {
        }

        #endregion
        #endregion
    }
}