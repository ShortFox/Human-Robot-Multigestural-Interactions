using UnityEngine;
using Tobii.XR;

namespace MQ.MultiAgent
{
    public class VRInput : AvatarInputBase
    {
        [SerializeField] public GameObject xrRig;
        PolhemusController polhemusController;
        [HideInInspector] public Transform xrRigBase;
        [HideInInspector] public Transform xrRigCamera;
        [HideInInspector] public Transform polhemusTransform;

        protected override void Awake()
        {
            base.Awake();
            polhemusController = indexFinger.GetComponent<PolhemusController>();

            // This check makes optional the configuration of the Polhemus Controller via prefab.
            if (polhemusController == null)
            {
                Debug.Log($"Polhemus controller is missing for {gameObject.name}, attaching new controller.");
                polhemusController = indexFinger.gameObject.AddComponent<PolhemusController>();
            }

            xrRigBase = xrRig.transform;
            xrRigCamera = xrRig.transform.Find("Camera Offset").transform.Find("Main Camera").transform;
            polhemusTransform = indexFinger.transform;
        }

        private void Start()
        {
            polhemusController.enabled = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                CenterPlayer();
            }

            headPose.SetPositionAndRotation(xrRigCamera.position, xrRigCamera.rotation);
            var eyeTracking = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);
            // This check is only done to work with the HMD Simulator, since convergence is 0 for it.
            var convergence = eyeTracking.ConvergenceDistance == 0 ? 10 : eyeTracking.ConvergenceDistance;
            var pose = eyeTracking.GazeRay.Origin + eyeTracking.GazeRay.Direction * convergence;
            gazeTarget.position = pose;
        }

        public override void CenterPlayer()
        {
            //Center Body
            transform.position = new Vector3(
                xrRigCamera.transform.position.x,
                transform.position.y,
                xrRigCamera.transform.position.z);

            // Relocate camera
            xrRigBase.position = new Vector3(
                xrRigBase.transform.position.x,
                transform.position.y,
                xrRigBase.transform.position.z);

            polhemusController.Center(AvatarInfo.MainPlayerPosition);

            //Change position of InitCube
            Transform myInitCube = transform.Find("FingerCubeReady");

            if (myInitCube == null)
            {
                Debug.LogError(
                    "Missing `FingerCubeReady` object. " +
                    "Please add and configure a fingerCubeReady for this Avatar.");
                return;
            }

            myInitCube.position = AvatarInfo.MainPlayerPosition;
            myInitCube.gameObject.GetComponent<EventArguments>().DefaultPosition = myInitCube.position;
        }

        public override bool IsRunning()
        {
            return true;
        }
    }
}

