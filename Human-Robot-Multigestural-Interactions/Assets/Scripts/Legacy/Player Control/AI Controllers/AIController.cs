namespace MQ.MultiAgent
{
    using UnityEngine;
    using RootMotion.FinalIK;

    /// <summary>
    /// This class handles all the AI-Related Behaviours. This class should be placed on the relevant player's "Avatar" Transform.
    /// </summary>
    public abstract class AIController : PlayerController
    {
        protected bool ActiveAI;

        protected FullBodyBipedIK _myBody;
        protected EyeController _myEyeController;
        protected LookAtIK _myGaze;                    //https://www.youtube.com/watch?v=5DlTjasmTLk a link about LookAtIK setup.

        private Transform _gazeTarget;
        public Transform GazeTarget
        {
            get { return _gazeTarget; }
            set
            {
                _gazeTarget = value;
                Self.Eyes.GazedTransform = _gazeTarget;     //Set Gazed object to Eyes.
                Self.Eyes.GazedTransformName = _gazeTarget.name;
            }
        }

        public override bool IsRunning()
        {
            return true;
        }
        private string _gazeTransformName;
        public string GazeTransformName
        {
            get
            {
                _gazeTransformName = GazeTarget.name;
                return _gazeTransformName;
            }
            set
            {
                _gazeTransformName = value;
                Self.Eyes.GazedTransform = null;
                Self.Eyes.GazedTransformName = _gazeTransformName;
            }
        }
        private Vector3 _gazeLocat;
        public Vector3 GazeLocat
        {
            get { return _gazeLocat; }
            set
            {
                _gazeLocat = value;
                Self.Eyes.GazedLocat = _gazeLocat;
            }
        }
        protected Vector3 HandTarget;
        protected Vector3 HandTargetRot;

        public override void Initialize(Player Self)
        {
            base.Initialize(Self);

            Transform avatar = Self.transform.Find("Avatar");
            HandTarget = Self.HandInitCube.position;
            HandTargetRot = Self.Hand.Rotation.eulerAngles;

            try
            {
                avatar.GetComponent<VRIK>().enabled = false;
            }
            catch
            {
                Debug.LogWarning("Unable to locate VRIK. This most likely means the AIController is on the wrong transform.");
            }

            try
            {
                _myBody = avatar.GetComponent<FullBodyBipedIK>();
                _myBody.enabled = true;
                _myBody.solver.headMapping.maintainRotationWeight = 1f;     //Maintain head rotation after IK solver so that LookIK functions properly.
            }
            catch
            {
                Debug.LogWarning("Unable to locate FullBodyBipedIK. This most likely means the AIController is on the wrong transform.");
            }

            ActiveAI = false;

            _myGaze = avatar.gameObject.AddComponent<LookAtIK>();
            //_myGaze.solver.headWeight = 0.8f; Defined in child classes.

            _myEyeController = avatar.GetComponent<EyeController>();
            _myEyeController.IsAI = true;

            EventsListen();
        }

        protected virtual void EventsListen()
        {
            TaskController.OnInitiateTrial += InitiateController;      //Prepare controller
            TaskController.OnRunTrial += RunController;                //Run controller
            TaskController.OnDoneTrial += DeactivateController;        //Deactive controller
        }

        protected virtual void EventsStopListening()
        {
            TaskController.OnInitiateTrial -= InitiateController;
            TaskController.OnRunTrial -= RunController;
            TaskController.OnDoneTrial -= DeactivateController;
        }


        public abstract void ResetController();     //Reset AI Controller.

        public abstract void InitiateController();

        public abstract void DeactivateController();

        //Update state when Controller is Active.
        public override void UpdateState()
        {
            //If Gaze target is ever null, then have Avatar look forward. This is default behavior unless LookIK gets changed (such as in Playback_IKSolverLookAt)
            if (GazeTarget == null)
            {
                _myGaze.solver.IKPosition = Self.Head.forward * 3;
                _myGaze.solver.IKPosition.y = Self.Head.position.y;
            }
            else _myGaze.solver.target = GazeTarget;        //This need to be edited so that solver.target is Lerped. Read through docs?

            if (HandTarget != null) Self.Hand.Position = HandTarget;     //If HandTarget defined, move hand to that target.
            if (HandTargetRot != null) Self.Hand.Rotation = Quaternion.Euler(HandTargetRot);
        }

        public override void CenterPlayer()
        {
            if (Self.IsLocalPlayer) Self.Side = Mathf.RoundToInt(Mathf.Sign(Self.transform.Find("Camera").position.z));
            else { Self.Side = Mathf.RoundToInt(Mathf.Sign(Camera.main.transform.position.z)) * -1; }

            Self.Hand.Rotation = Quaternion.Euler(new Vector3(0, 90*Self.Side, 0));            //Adjust Finger rotation so that finger is pointing straight.
            HandTargetRot = Self.Hand.Rotation.eulerAngles;

            //Center Body
            var Avatar = Self.transform.Find("Avatar");
            Avatar.position = new Vector3(0, Avatar.position.y, 0.75f * Self.Side);
            Vector3 rotation = Vector3.zero;
            rotation.y = Self.Side == 1 ? 180 : 0;      //Fix
            Avatar.eulerAngles = rotation;

            //Center Hand
            Vector3[] _candidateOffets = new Vector3[] { new Vector3(0, 0.8f, 0.3f), new Vector3(0, 0.8f, -0.3f) };

            Vector3 offsetLocat = Vector3.zero;
            float closestDist = float.MaxValue;

            foreach (Vector3 offsetPos in _candidateOffets)
            {
                if (Vector3.Distance(Avatar.position, offsetPos) < closestDist)
                {
                    offsetLocat = offsetPos;
                    closestDist = Vector3.Distance(Avatar.position, offsetPos);
                }
            }
            //Change position of InitCube
            Transform myInitCube = Self.transform.Find("FingerCubeReady");

            Self.Hand.Position = offsetLocat;
            HandTarget = offsetLocat;
            myInitCube.position = offsetLocat;
            myInitCube.gameObject.GetComponent<EventArguments>().DefaultPosition = myInitCube.position;
        }
    }
}
