namespace MQ.MultiAgent
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Player : MonoBehaviour
    {
        #region Fields
        public Transform Head { get; protected set; }   //For some reason, accessing this value outside of this script does not give correct position.
        public Camera Camera { get; protected set; }
        public Eyes Eyes { get; protected set; }
        public Hand Hand { get; protected set; }
        public Transform HandInitCube { get; protected set; }

        private int _trialIdx;

        public int Side;
        public Vector3 FixationCalibrationPoint;
        public bool IsLocalPlayer;     //LocalPlayer tag

        #endregion

        #region Properties
        public bool IsActive;
        #endregion

        private PlayerController _myController;      //Controller associated with this object.
        public PlayerController Controller { get { return _myController; } }

        public void SetUp(PlayerController controller, int side)
        {
            Debug.Log("Setting up Player");

            Side = side;

            Eyes = transform.Find("Avatar").GetComponent<Eyes>();
            Head = Eyes.HeadRoot;
            Hand = transform.Find("AvatarTargets").transform.Find("IndexFinger").GetComponent<Hand>();
            HandInitCube = transform.Find("FingerCubeReady");

            Eyes.IsAI = controller is AIController;       //Flag if Eyes are AI-controlled.

            if (_myController != null) Debug.LogError("ERROR: Player Controller already defined");

            _myController = controller;
            _myController.Initialize(this);

            IsActive = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                //Centering Player Body
                CenterPlayer();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (Controller is AIController)
                {
                    AIController aiController = (AIController)Controller;
                    aiController.ResetController();
                }
            }

            if (_myController == null)
            {
                return;
            }

            if (_myController.IsRunning())
            {
                _myController.UpdateState();
            }
        }

        public void UpdateHeadTarget(Transform newHead)
        {
            Head = newHead;
        }

        public void CenterPlayer()
        {
            Controller.CenterPlayer();
        }



        #region Head Data Structure
        private string _headHeader;
        public string HeadHeader
        {
            get
            {
                if (_headHeader != null) return _headHeader;
                else
                {
                    _headHeader = "";
                    _headHeader += "HeadX,HeadY,HeadZ,HeadEulerX,HeadEulerY,HeadEulerZ";
                }
                return _headHeader;
            }
        }
        public string LatestHeadDataString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5}",
                Head.position.x,
                Head.position.y,
                Head.position.z,
                Head.eulerAngles.x,
                Head.eulerAngles.y,
                Head.eulerAngles.z);
        }
        #endregion
    }
}
