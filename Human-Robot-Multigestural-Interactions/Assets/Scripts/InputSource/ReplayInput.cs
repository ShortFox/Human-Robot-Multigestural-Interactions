using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

namespace MQ.MultiAgent
{
    public class ReplayInput : AvatarInputBase
    {
        private ReplayCalibration replayCalibration;
        private Transform fingerCubeReady;

        // The role of the AI agent
        protected string role;
        protected string playbackID;

        //Trajectory data
        protected List<Box.PlaybackInfo> _playbackData;

        // Playback Samplerate
        private float playbackSampleRate = 1 / 90f;
        // Holds index of Hand trajectory from file.
        private Hand _hand;
        private int _indx = 0;
        protected GameObject[] BlockTargets;
        public Transform PartnerHead;

        protected bool _waitingForInitiator = false;

        protected bool _isTaskRunning;
        protected List<IEnumerator> CurrentTrajectoryCoroutines;  // Current playback coroutine being conducted

        public override bool IsRunning() { return _isTaskRunning; }

        public event EventHandler ReplayEnded;

        public void OnReplayEnded()
        {
            ReplayEnded?.Invoke(this, EventArgs.Empty);
        }

        protected override void Awake()
        {
            base.Awake();
            replayCalibration = transform
                .Find("ReplayCalibrationObject")?
                .gameObject?
                .GetComponent<ReplayCalibration>();

            if (replayCalibration == null)
            {
                Debug.LogError(
                    "Missing `ReplayCalibrationObject` object. " +
                    "Please add and configure a ReplayCalibrationObject for this Avatar.");
            }

            fingerCubeReady = transform.Find("FingerCubeReady")?.transform;

            if (fingerCubeReady == null)
            {
                Debug.LogError(
                    "Missing `fingerCubeReady` object. " +
                    "Please add and configure a fingerCubeReady for this Avatar.");
            }

            _hand = transform.Find("AvatarTargets").Find("IndexFinger").GetComponent<Hand>();
        }

        private void UpdateState(Box.PlaybackInfo step)
        {
            replayCalibration.Calibrate(ref step);
            indexFinger.SetPositionAndRotation(step.HandPos, Quaternion.Euler(step.HandRot));
            headPose.SetPositionAndRotation(step.HeadPos, Quaternion.Euler(step.HeadRot));
            gazeTarget.position = step.GazePos;
        }

        public override void SetUp()
        {
            BlockTargets = GameObject.Find("[Box Task Components]").gameObject.GetComponent<BoxTaskComponents>().Blocks;
            foreach (GameObject block in BlockTargets)
            {
                ReactiveCube cube = block.GetComponent<ReactiveCube>();
                cube.BoxCollisionTrigger += (sender, args) => {
                    if (args.hitObject != indexFinger && _waitingForInitiator)
                    {
                        StartCoroutine(RunResponderTrajectory(cube.transform));
                        _waitingForInitiator = false;
                    }
                };
            }
        }

        // Called when trial is initated.
        public override void Initiate()
        {
            // Added security to ensure these fields are reset properly
            _responseTarget = null;

            _isTaskRunning = false;

            ResetController();

            if (!string.IsNullOrEmpty(playbackID))
            {
                _playbackData = Box.TrialInfo.ReadPlaybackFile(playbackID);
                UpdateState(_playbackData[0]);
            }
        }

        public override void Run()
        {
            _isTaskRunning = true;
        }

        private IEnumerator RunTrajectory(List<Box.PlaybackInfo> data, int startIndx, bool abort = false)
        {
            float startTime = Time.timeSinceLevelLoad;

            _waitingForInitiator = abort;

            var d = data[0];

            for (int idx = startIndx; idx < data.Count; idx++)
            {
                d = data[idx];

                if (Time.timeSinceLevelLoad - startTime >= playbackSampleRate)
                {
                    UpdateState(d);
                    startTime = Time.timeSinceLevelLoad;
                }

                if ( abort && d.Moving)
                {
                    break;
                }

                _indx = idx;
                yield return null;
            }

            if (!abort)
            {
                replayCalibration.Calibrate(ref d);
                LaunchCoroutine(RunDMP_Hand(_hand, d.HandPos));
            }

            //Initiate Post Trajectory Game Logic.
            float timeStamp = Time.timeSinceLevelLoad;

            float delay = RandomGaussian.Sample(0.5f, 0.1f, 3.5f);
            yield return new WaitForSeconds(delay);

            // After delay, look at partner's head.
            LookAtPartner();

            if (!abort)
            {
                OnReplayEnded();
            }
        }

        #region DMP Control
        // System variables for integration
        private float dt { get { return Time.deltaTime; } }             //timestep for integration

        //State Dynamics Parameters
        private float damping = 10f;                                    //Velocity dampening term
        private float stiffness = 25f;                                  //Velocity stiffness term

        private float damping_eyes = 16f;                                    //Velocity dampening term
        private float stiffness_eyes = 64f;                                  //Velocity stiffness term

        void DMP_iteration(ref Vector3 pos,
                           ref Vector3 vel,
                           ref Vector3 acc,
                           Vector3 target,
                           float damping,
                           float stiffness)
        {
            Vector3 noise = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f),
                            UnityEngine.Random.Range(-1.0f, 1.0f),
                            UnityEngine.Random.Range(-1.0f, 1.0f));

            acc = -damping * vel - stiffness * (pos - target) + noise;
            pos = pos + vel * dt;
            vel = vel + acc * dt;
        }

        //Run DMP Model for Hand
        private IEnumerator RunDMP_Hand(Hand effector, Vector3 target)
        {
            Vector3 pos = effector.Position;
            Vector3 vel = effector.Velocity;
            Vector3 acc = Vector3.zero;

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                DMP_iteration(ref pos, ref vel, ref acc, target, damping, stiffness);
                indexFinger.transform.position = pos;
                yield return null;
            }
        }

        //Run DMP Model for Eyes/Head
        private IEnumerator RunDMP_Eyes(Vector3 initPos, Transform target)
        {
            Vector3 pos = initPos;
            Vector3 vel = Vector3.zero;
            Vector3 acc = Vector3.zero;

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                DMP_iteration(ref pos, ref vel, ref acc, target.position, damping_eyes, stiffness_eyes);
                gazeTarget.transform.position = pos;
                yield return null;
            }
        }

        // Rotates head, but once initial head target is reached, it does not update.
        private IEnumerator RunDMP_Head(Transform initHead, Transform target)
        {
            Vector3 pos = initHead.forward;
            Vector3 vel = Vector3.zero;
            Vector3 acc = Vector3.zero;

            Vector3 targetPos = (target.position - initHead.position).normalized;
            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                DMP_iteration(ref pos, ref vel, ref acc, targetPos, damping_eyes, stiffness_eyes);
                // Find the vector pointing from our position to the target
                var direction = pos;

                // Assing the rotation that follows the direction we need
                headPose.transform.rotation = Quaternion.LookRotation(direction);
                yield return null;
            }
        }

        // Old
        // TODO: Currently this updates the head movement by changing location of gaze point for head.
        // Needs to be updated to change the head position and rotation instead.
        /*
        private IEnumerator RunDMP_Head(Vector3 initPos, Transform target)
        {
            Vector3 pos = initPos;
            Vector3 vel = Vector3.zero;
            Vector3 acc = Vector3.zero;

            //Note time
            float startTime = Time.timeSinceLevelLoad;

            while (true)
            {
                DMP_iteration(ref pos, ref vel, ref acc, target.position, damping_eyes, stiffness_eyes);
                // Find the vector pointing from our position to the target
                var direction = (pos - headPose.transform.position).normalized;

                // Assing the rotation that follows the direction we need
                headPose.transform.rotation = Quaternion.LookRotation(direction);
                yield return null;
            }
        }
        */
        #endregion

        public void LaunchCoroutine(IEnumerator c)
        {
            CurrentTrajectoryCoroutines.Add(c);
            StartCoroutine(CurrentTrajectoryCoroutines[CurrentTrajectoryCoroutines.Count - 1]);
        }

        void StopCoroutines()
        {
            if (CurrentTrajectoryCoroutines?.Count > 0)
            {
                CurrentTrajectoryCoroutines.ForEach(routine => StopCoroutine(routine));
                CurrentTrajectoryCoroutines.Clear();
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////
        // TODO: From here on, most stuff needs to be reviewed and potentially removed.

        #region Private Fields
        private Transform _responseTarget;

        #endregion

        #region Override Methods
        // Reset AI behaviour.
        public void ResetController()
        {
            //Security to ensure a coroutine isn't running.
            StopCoroutines();

            _indx = 0;
            CurrentTrajectoryCoroutines = new List<IEnumerator>();
        }

        // Called when trial is over
        public void DeactivateController()
        {
            //Security to ensure a coroutine isn't running.
            StopCoroutines();

            //At end of trial, bring hand to initcube position.
            indexFinger.transform.position = fingerCubeReady.position;

            // Added security to ensure these fields are reset properly
            _responseTarget = null;
            _isTaskRunning = false;
            onStopped();
        }

        // Next State in Update frame.
        private void Update()
        {
            if (_isTaskRunning)
            {
                if (CurrentTrajectoryCoroutines == null || CurrentTrajectoryCoroutines.Count == 0)
                {
                    ResetController();
                    if (role == "Initiator")
                    {
                        LaunchCoroutine(RunTrajectory(_playbackData, _indx));
                    }
                    else if (role == "Responder")
                    {
                        LaunchCoroutine(RunTrajectory(_playbackData, _indx, true));
                    }
                    else
                    {
                        Debug.LogError("Error: AI Role not what is expected");
                    }

                    if (CurrentTrajectoryCoroutines.Count > 1)
                    {
                        Debug.LogError("More coroutines in list than expected");
                    }
                }
            }
        }

        #endregion

        #region Event Subscription Methods
        private void SetResponseTarget(Transform obj)
        {
            _responseTarget = obj;
        }

        public void SetTrialInfo(Box.TrialInfo trial)
        {
            role = trial.RoleB;
            _playbackData = trial.PlaybackData;
            playbackID = trial.PlaybackID;
        }
        #endregion

        #region Trajectory Playback Methods and Variables

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
            StopCoroutines();
            LaunchCoroutine(RunTrajectory(_playbackData, _indx));
        }

        private void LookAtPartner()
        {
            LaunchCoroutine(RunDMP_Head(headPose.transform, PartnerHead));
            LaunchCoroutine(RunDMP_Eyes(gazeTarget.transform.position, PartnerHead));
        }

        #region Appropriate Responder Trajectory Selection
        private List<Box.PlaybackInfo> ReadResponderFile(string targetCube)
        {
            DirectoryInfo directory = new DirectoryInfo(
                Application.streamingAssetsPath + "/Trajectories/Responder/" + targetCube);

            FileInfo[] trajectFiles = directory.GetFiles("*.csv");  // File Information

            // Select a file randomly in sub-folder
            string file = trajectFiles[UnityEngine.Random.Range(0,trajectFiles.Length)].FullName;  //Get full path name of trajectory

            List<Box.PlaybackInfo> output = new List<Box.PlaybackInfo>();

            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();          //Remove header.
                while (!sr.EndOfStream)
                {
                    output.Add(new Box.PlaybackInfo(sr.ReadLine().Split(',')));
                }
            }
            return output;
        }

        public override void CenterPlayer() {}

        public override void Reset()
        {
            ResetController();
        }

        #endregion
        #endregion
    }
}