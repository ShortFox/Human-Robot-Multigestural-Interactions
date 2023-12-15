namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class HumanController : PlayerController
    {
        #region Private Fields
        protected PolhemusController _polController;
        protected EyeController _eyeController;
        #endregion

        public override void Initialize(Player Self)
        {
            base.Initialize(Self);
            if (Self.tag == "Player" && Self.Camera.enabled == false) Self.Camera.enabled = true;

            _polController = Self.Hand.GetComponent<PolhemusController>();       //This will be called for both players ... will need to fix.
            _polController.enabled = true;
            _eyeController = Self.Eyes.GetComponent<EyeController>();
            _eyeController.enabled = true;                              //EyeController is on by default.
            Self.Eyes.Left = _eyeController.LeftEye;
            Self.Eyes.Right = _eyeController.RightEye;
        }

        public override void UpdateState()
        {
            Self.Hand.Position = _polController.Position;
            Self.Hand.Rotation = _polController.Rotation;

            //Self.Eyes.GazedLocat = _eyeController.GazedLocat;
            Self.Eyes.GazedTransform = _eyeController.GazedTransform;
        }

        public override void CenterPlayer()
        {
            Self.Side = Mathf.RoundToInt(Mathf.Sign(Self.Camera.transform.position.z));
            Self.FixationCalibrationPoint = Self.Camera.transform.position;
            Self.FixationCalibrationPoint.x = 0f;
            Self.FixationCalibrationPoint.z = 0f;

            //Center Body
            var Avatar = Self.transform.Find("Avatar");
            Avatar.position = new Vector3(Self.Camera.transform.position.x, Avatar.position.y, Self.Camera.transform.position.z);
            Vector3 rotation = Vector3.zero;
            rotation.y = Self.Camera.transform.eulerAngles.y;
            Avatar.eulerAngles = rotation;

            //Center Hand
            Vector3[] _candidateOffets = new Vector3[] { new Vector3(0, 0.8f, 0.3f), new Vector3(0, 0.8f, -0.3f) };

            Vector3 offsetLocat = Vector3.zero;
            float closestDist = float.MaxValue;

            foreach (Vector3 offsetPos in _candidateOffets)
            {
                if (Vector3.Distance(Self.Camera.transform.position, offsetPos) < closestDist)
                {
                    offsetLocat = offsetPos;
                    closestDist = Vector3.Distance(Self.Camera.transform.position, offsetPos);
                }
            }
            Vector3 _offset = offsetLocat;

            _polController.Center(_offset);

            //Change position of InitCube
            Transform myInitCube = Self.transform.Find("FingerCubeReady");

            myInitCube.position = offsetLocat;
            myInitCube.gameObject.GetComponent<EventArguments>().DefaultPosition = myInitCube.position;


        }

        public override bool IsRunning()
        {
            return true;
        }

        public override void Initiate()
        {
        }

        public override void RunController()
        {
        }


    }
}

