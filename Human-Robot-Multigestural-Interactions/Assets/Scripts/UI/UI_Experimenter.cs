namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.UI;
    using Tobii.Research.Unity;
    using System.Collections;

    public class UI_Experimenter : MonoBehaviour
    {
        //Instance is created for easy reference for calling DebugLog.
        public static UI_Experimenter Instance { get; private set; }

        #region Position HeadSet
        private GameObject _hmd_positionGuide;
        private GameObject HMD_PositionGuide
        {
            get
            {
                if (_hmd_positionGuide != null)
                {
                    return _hmd_positionGuide;
                }
                var positionGuide = GameObject.Find("HMD_Canvas");

                if (!positionGuide)
                {
                    positionGuide = GameObject.Find("HMD_Canvas(Clone)");
                    if (!positionGuide)
                    {
                        Debug.LogError("Failed to find HMD_Canvas");
                        return null;
                    }
                }
                _hmd_positionGuide = positionGuide;
                return _hmd_positionGuide;
            }

        }
        private bool _positionGuideActive;
        protected bool PositionGuideActive
        {
            get{ return _positionGuideActive; }
            set
            {
                _positionGuideActive = value;
                HMD_PositionGuide.SetActive(_positionGuideActive);
            }
        }
        #endregion
        #region Toggle Gaze
        private bool _gazeActive;
        protected bool GazeActive
        {
            get { return _gazeActive; }
            set
            {
                _gazeActive = value;
                VRGazeTrail.Instance.On = _gazeActive;
            }
        }
        #endregion
        #region Center Avatar
        private Player MyPlayer
        {
            get
            {
                var player = GameObject.FindGameObjectsWithTag("Player");

                if (player.Length == 0)
                {
                    Debug.LogError("Failed to find Player");
                    return null;
                }
                else if (player.Length > 1)
                {
                    Debug.LogError("Too many objects tagged as 'Player'");
                    return null;
                }
                else
                {
                    return player[0].GetComponent<Player>();
                }
            }
        }
        private SyncActors MyActor
        {
            get
            {
                try
                {
                    return MyPlayer.GetComponent<SyncActors>();
                }
                catch
                {
                    Debug.LogError("Error Finding Player's SyncActors Component");
                    return null;
                }
            }
        }
        #endregion
        #region Mirror Toggle
        private GameObject _mirrors;
        private GameObject Mirrors
        {
            get
            {
                if (_mirrors != null) return _mirrors;
                else
                {
                    _mirrors = GameObject.Find("Mirrors");
                    return _mirrors;
                }
            }
        }
        private bool _mirrorsActive = true;
        public bool MirrorsActive
        {
            get { return _mirrorsActive; }
            set
            {
                _mirrorsActive = value;
                Mirrors.SetActive(_mirrorsActive);
            }
        }
        private GameObject _taskComponents;
        private GameObject TaskComponents
        {
            get
            {
                if (_taskComponents != null) return _taskComponents;
                else
                {
                    _taskComponents = GameObject.Find("[Tasks Components]");
                    return _taskComponents;
                }
            }
        }
        private bool _taskComponentsActive = true;
        public bool TaskComponentsActive
        {
            get { return _taskComponentsActive; }
            set
            {
                _taskComponentsActive = value;
                TaskComponents.SetActive(_taskComponentsActive);
            }
        }
        #endregion
        #region Debug Logging
        [SerializeField] Text DebugLog;
        private float _logResetTimer;
        #endregion

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            PositionGuideActive = false;
            GazeActive = false;
            _logResetTimer = 3f;
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) TogglePositionScreen();
            if (Input.GetKeyDown(KeyCode.F2)) CalibrateEyes();
            if (Input.GetKeyDown(KeyCode.F3)) ToggleGaze();
            if (Input.GetKeyDown(KeyCode.C)) CenterAvatar();
            if (Input.GetKeyDown(KeyCode.M)) ToggleMirrors();
            // if (Input.GetKeyDown(KeyCode.X)) SyncExperimentPhase.Instance.SetPhase(3);   //X will force trial to end.
            //if (Input.GetKeyDown(KeyCode.P)) TogglePause();
            //if (Input.GetKeyDown(KeyCode.Space)) NextBlock();
        }
        #region Helper Functions
        public void CalibrateEyes()
        {
            VRCalibration.Instance.StartCalibration();
        }
        public void TogglePositionScreen()
        {
            PositionGuideActive = !PositionGuideActive;
            UpdateLog("Position Guide is " + (PositionGuideActive ? "On" : "Off"));
        }
        public void ToggleGaze()
        {
            GazeActive = !GazeActive;
            UpdateLog("Gaze View is "+(GazeActive ? "On" : "Off"));
        }
        public void CenterAvatar()
        {
            MyPlayer.CenterPlayer();
            //MyActor.TransmitCalibrationPoint(MyCamera.position);
        }
        public void ToggleMirrors()
        {
            MirrorsActive = !MirrorsActive;
            UpdateLog("Mirror View is " + (MirrorsActive ? "On" : "Off"));

            TaskComponentsActive = !MirrorsActive;
            //UpdateLog("Task Components are " + (TaskComponentsActive ? "On" : "Off"));

        }
        public void TogglePause()
        {
            //This function will pause all interaction (if participant needs to take a break).
            //Timescale.0 and maybe blackout the screen.
        }
        public void NextBlock()
        {
            //Proceed to next block of trials.
        }
        public void FlipXPosition()
        {
            PolhemusRead.Instance.FlipX = -1 * PolhemusRead.Instance.FlipX;
            UpdateLog("Polhemus X Rotation Flipped");
        }
        public void FlipYPosition()
        {
            PolhemusRead.Instance.FlipY = -1 * PolhemusRead.Instance.FlipY;
            UpdateLog("Polhemus Y Rotation Flipped");
        }
        public void FlipZPosition()
        {
            PolhemusRead.Instance.FlipZ = -1 * PolhemusRead.Instance.FlipZ;
            UpdateLog("Polhemus Z Rotation Flipped");
        }
        public static void UpdateLog(string text)
        {
            Instance.DebugLog.text = text;
            Instance.CancelInvoke("ResetLog");
            Instance.Invoke("ResetLog", Instance._logResetTimer);
        }
        void ResetLog()
        {
            Instance.DebugLog.text = "";
        }
        #endregion
    }

}

