namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;
    using Tobii.Research.Unity;

    [RequireComponent(typeof(NetworkIdentity))]
    public class SyncActors : NetworkBehaviour
    {
        #region Networked Movement Variables
        //Avatar's Body
        [SyncVar] private Vector3 posAvatar;
        [SyncVar] private Quaternion rotAvatar;

        //Camera
        [SyncVar] private Vector3 posCamera;
        [SyncVar] private Quaternion rotCamera;

        //Finger
        [SyncVar] private Vector3 posFinger;
        [SyncVar] private Quaternion rotFinger;

        //InitCube
        [SyncVar] private Vector3 posReadyCube;

        //Eyes are Handled in EyeController

        //LeftEye
        [SyncVar] public Vector3 gazeLeftEye;           //Local Vector of Gaze Direction.
        [SyncVar] public bool closedLeftEye;            //Flag if left eye is closed.
        [SyncVar] public Vector3 rotLeftEye;            //Local rotation of left eye.

        //RightEye
        [SyncVar] public Vector3 gazeRightEye;          //Local Vector of Gaze Direction.
        [SyncVar] public bool closedRightEye;           //Flag if right eye is closed.
        [SyncVar] public Vector3 rotRightEye;           //Local rotation of right eye.

        //Gaze
        [SyncVar] public string gazeObject;             //Object being gazed
        [SyncVar] public Vector3 gazePoint;             //Point of object gaze.

        //Touch
        [SyncVar] public string touchObject;
        [SyncVar] public string pointObject;

        //Experiment Relevant Variables.
        [SyncVar] public bool readyState;               //Flag indicating if player is ready for multiplayer.

        [SyncVar] public float localTime;               //Local Time for player.
        //public double ServerTime { get { return NetworkTime.time; } }
        //public double ServerRRT { get { return NetworkTime.rtt; } }
        #endregion

        public OtherPlayer partnerPlayer;
        public bool partnerFound;

        private bool _aiControllable;
        public bool AI_Controllable
        {
            get { return _aiControllable; }
            set
            {
                //if (isLocalPlayer) return;
                _aiControllable = value;
            }
        }

        #region Private Fields
        private Transform myAvatar;
        private Transform myCamera;
        public Transform myHead;
        private Transform myHeadBone;
        public Transform myFinger;                  //This is updated in Update Loop, below.
        public Transform myInitCube;
        private Hand HAND;
        private Eyes EYES;

        private SyncExperimentPhase SYNC;
        #endregion

        private void Awake()
        {
            myAvatar = this.transform.Find("Avatar");
            myCamera = this.transform.Find("Camera");
            myHead = myCamera.Find("Head");
            myHeadBone = this.transform.Find("Avatar").Find("mixamorig:Hips").Find("mixamorig:Spine").Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:Neck").Find("mixamorig:Head");
            myFinger = this.transform.Find("Finger");
            myInitCube = this.transform.Find("FingerCubeReady");
            HAND = myFinger.GetComponent<Hand>();
            EYES = myAvatar.GetComponent<Eyes>();
        }
        private void Start()
        {
            syncInterval = 0f;

            SYNC = SyncExperimentPhase.Instance;
            SYNC.enabled = true;

            AI_Controllable = TitleScreenData.IsAI;
            if (isLocalPlayer) return;

            // If this is AI_Controllable then add AI components.
            if (AI_Controllable)
            {
                if (this.GetComponent<Player>() == null)
                {
                    Player MyPlayer = gameObject.AddComponent<Player>();
                    // MyPlayer.SetUp(TitleScreenData.Task, Mathf.RoundToInt(Mathf.Sign(Camera.main.transform.position.z)) * -1, TitleScreenData.IsAI, false);
                }
            }
        }

        private void Update()
        {
            //Removed || AI_Controllable from below if statement as AI and localplayer were competing for value. need to test
            if (isLocalPlayer)
            {
                TransmitVariables();
                //TransmitReadiness();
            }
            if (!isLocalPlayer)
            {
                if (partnerFound)
                {
                    if (myAvatar.gameObject.activeInHierarchy)
                    {
                        myAvatar.gameObject.SetActive(false);
                        myFinger.gameObject.SetActive(false);
                        myInitCube.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (!myAvatar.gameObject.activeInHierarchy)
                    {
                        myAvatar.gameObject.SetActive(true);
                        myFinger.gameObject.SetActive(true);
                        myInitCube.gameObject.SetActive(true);
                    }
                }

                // Force partners to be able to see each other
                if (!AI_Controllable && !myAvatar.gameObject.activeInHierarchy)
                {
                    myAvatar.gameObject.SetActive(true);
                    myFinger.gameObject.SetActive(true);
                    myInitCube.gameObject.SetActive(false);
                }

                if (myAvatar.gameObject.activeInHierarchy && !AI_Controllable) UpdateVariables();
                return;
            }
        }

        #region Server Commands
        //Server runs this.
        [Command]
        void CmdAvatarToServer(Vector3 pos, Quaternion rot)
        {
            posAvatar = pos;
            rotAvatar = rot;
        }
        [Command]
        void CmdCameraToServer(Vector3 pos, Quaternion rot, Vector3 forward, Vector3 up)
        {
            //Unsure if this isn't working exactly because VRIK and FullbodyIK are treated differently, or if the offset is not correct
            //posCamera = pos+forward*0.15f+up*.05f;      //This reflects how far ahead Camera object is from rootbone. Probably good idea to not have this hardcoded. (Take The Vector3.Distance between headbone and camera for example on wake).
            posCamera = pos;
            rotCamera = rot;
        }
        [Command]
        void CmdFingerToServer(Vector3 pos, Quaternion rot)
        {
            posFinger = pos;
            rotFinger = rot;
        }
        [Command]
        void CmdReadyCubeToServer(Vector3 pos)
        {
            posReadyCube = pos;
        }
        [Command]
        void CmdLeftEyeToServer(Vector3 gaze, bool closed, Vector3 rot)
        {
            gazeLeftEye = gaze;
            closedLeftEye = closed;
            rotLeftEye = rot;
        }
        [Command]
        void CmdRightEyeToServer(Vector3 gaze, bool closed, Vector3 rot)
        {
            gazeRightEye = gaze;
            closedRightEye = closed;
            rotRightEye = rot;
        }
        [Command]
        void CmdGazeToServer(string name, Vector3 point)
        {
            gazeObject = name;
            gazePoint = point;
        }
        [Command]
        void CmdTouchPointToServer(string nameT, string nameP)
        {
            touchObject = nameT;
            pointObject = nameP;
        }
        [Command]
        void CmdTimeToServer(float time)
        {
            localTime = time;
        }
        [Command]
        void CmdStateToServer(bool state)
        {
            readyState = state;
        }
        #endregion

        #region Client Messages
        //Client message to Server.
        [ClientCallback]
        void TransmitVariables()
        {
            CmdAvatarToServer(myAvatar.position, myAvatar.rotation);

            if (EYES.IsAI && Vector3.Distance(posCamera,myHeadBone.position) < 0.2f)
            {
                //if AI, lerp to position.
                CmdCameraToServer(Vector3.Lerp(posCamera,myHeadBone.position,0.1f), Quaternion.Lerp(rotCamera,myHeadBone.rotation,0.1f), myHeadBone.forward, myHeadBone.up);
            }
            else
            {
                CmdCameraToServer(myHeadBone.position, myHeadBone.rotation, myHeadBone.forward, myHeadBone.up);
            }

            CmdFingerToServer(myFinger.position, myFinger.rotation);
            CmdReadyCubeToServer(myInitCube.position);
            CmdLeftEyeToServer(gazeLeftEye, closedLeftEye, rotLeftEye);
            CmdRightEyeToServer(gazeRightEye, closedRightEye, rotRightEye);

            try
            {
                CmdGazeToServer(EYES.GazedTransform.name, EYES.GazedLocat);
            }
            catch
            {
                CmdGazeToServer("", Vector3.positiveInfinity);
            }
            try
            {
                //Define it locally and then send a command to server (this works if the SyncActor is an AI).
                touchObject = HAND.ContactName;
                pointObject = HAND.PointName;
                CmdTouchPointToServer(HAND.ContactName, HAND.PointName);
            }
            catch
            {
                Debug.LogError("Error: CmdTouchPointToServer failed");
                CmdTouchPointToServer("", "");
            }
            CmdTimeToServer(Time.timeSinceLevelLoad);
        }
        [ClientCallback]
        void TransmitReadiness()
        {
            CmdStateToServer(readyState);
        }
        #endregion

        #region Helper Methods
        protected virtual void UpdateVariables()
        {
            myAvatar.position = posAvatar;
            myAvatar.rotation = rotAvatar;
            myHead.position = posCamera;
            myHead.rotation = rotCamera;
            myFinger.position = posFinger;
            myFinger.rotation = rotFinger;
            myInitCube.position = posReadyCube;
        }
        #endregion
    }
}
