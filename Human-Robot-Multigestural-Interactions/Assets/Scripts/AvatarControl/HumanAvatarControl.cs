
using UnityEngine;
using RootMotion.FinalIK;
using Tobii.XR;

namespace MQ.MultiAgent
{

    /// <summary>
    /// Handles Extraction of Eye Gaze and Eye Movement Smoothing + Blinking
    /// </summary>
    [RequireComponent(typeof(Eyes))]
    [RequireComponent(typeof(VRIK))]
    public class HumanAvatarControl : MonoBehaviour
    {
        public Transform GazeTarget;
        public Transform HeadTransform;

        Playback_IKSolverLookAt customSolver;

        Eyes eyes;
        VRIK body;
        LookAtIK gaze;
        Vector3 gazeOrigin;
        Ray ray;
        RaycastHit lastHit;

        [Header("Blend Shapes")]

        [SerializeField, Tooltip("Body Skinned Mesh Renderer for blend shapes.")]
        private SkinnedMeshRenderer _bodyBlendShape;
        private const int BlendShapeLeftEyeLid = 0;
        private const int BlendShapeRightEyeLid = 1;

        [SerializeField, Tooltip("Facial blend shapes animation curve.")]
        private AnimationCurve _blendShapeAnimationCurve;

        [SerializeField, Tooltip("Facial blend shape animation time.")]
        private float _blendShapeAnimationTimeSeconds = 0.05f;

        public float eyeVerticalLowerLimit = -95;
        public float eyeVerticalHigherLimit = 95;
        public float eyeHorizontalLimit = 95;

        // Running blend shapes animation values.
        private float _blinkL;
        private float _blinkR;
        private float _animationProgressL;
        private float _animationProgressR;

        // Catch change in state flags.
        protected bool _leftEyeClosed;
        protected bool _rightEyeClosed;


        [Header("Eye Behaviour values")]

        [SerializeField, Tooltip("Cross eye correction for models that look cross eyed. +/-20 degrees.")]
        [Range(-20.0f, 20.0f)]
        private float _crossEyeCorrection;

        private Vector3 _leftSmoothDampVelocity;
        private Vector3 _rightSmoothDampVelocity;
        private float _leftLidSmoothDampVelocity;
        private float _rightLidSmoothDampVelocity;

        // Gaze direction fall-back.
        private Vector3 _previousSmoothedDirectionL = Vector3.forward;
        private Vector3 _previousSmoothedDirectionR = Vector3.forward;
        protected Vector3 _lastGoodDirectionL = Vector3.forward;
        protected Vector3 _lastGoodDirectionR = Vector3.forward;

        private const float CrossEyedCorrectionFactor = 100;
        private const float CrossEyednessSnarlTriggerValue = 0.3f;
        private const float AngularNoiseCompensationFactor = 800;

        private Camera myCamera;            //Camera attached to player.
        protected Transform _cameraTransform; //Camera's transform.

        //Edit these below.


        private void Awake()
        {
            // Create solver for gaze control (head and eye movements) of the avatar.
            eyes = gameObject.GetComponent<Eyes>();
            body = gameObject.GetComponent<VRIK>();
            gaze = gameObject.AddComponent<LookAtIK>();

            gaze.solver = new Playback_IKSolverLookAt
            {
                head = new IKSolverLookAt.LookAtBone(eyes.HeadRoot),
                eyes = new IKSolverLookAt.LookAtBone[2] {
                    new IKSolverLookAt.LookAtBone(eyes.Left),
                    new IKSolverLookAt.LookAtBone(eyes.Right)
                },
            };

            gaze.solver.SetLookAtWeight(1f, 0.5f, 1f, 1f);

            // TODO: I don't know why this is done, so I called the variable `customSolver` to remember that it was made
            // in this project. We should figure out wat it does and if we can remove it.
            customSolver = (Playback_IKSolverLookAt)gaze.solver;

            if (body != null && body.enabled)
            {
                body.solver.OnPostUpdate += UpdateGaze;
            }
        }

        private void UpdateGaze()
        {
            var headTarget = (HeadTransform.rotation * Vector3.forward) + HeadTransform.position;
            customSolver.Head_Target = headTarget;

            if (eyes.Left != null && eyes.Right != null)
            {
                gazeOrigin = (eyes.Left.position + eyes.Right.position) / 2;
                ray = new Ray(gazeOrigin, GazeTarget.position - gazeOrigin);

                Debug.DrawRay(gazeOrigin, GazeTarget.position - gazeOrigin, Color.red);

                // Get Ray Information
                Physics.Raycast(ray, out lastHit);
                customSolver.Eyes_Target = GazeTarget.position;

                // Open/Close eyes.
                var eyeTrackingDataLocal = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.Local);
                AnimateEyeLids(eyeTrackingDataLocal.IsLeftEyeBlinking, eyeTrackingDataLocal.IsRightEyeBlinking);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Clamp vertical gaze angles - needs to be done in degrees.
        /// </summary>
        /// <param name="gazeDirection">Direction vector of the gaze.</param>
        /// <param name="lowerLimit">The lower clamp limit in degrees.</param>
        /// <param name="upperLimit">The upper clamp limit  in degrees.</param>
        /// <returns>The gaze direction clamped between the two degree limits.</returns>
        private static Vector3 ClampVerticalGazeAngles(Vector3 gazeDirection, float lowerLimit, float upperLimit)
        {
            var angleRad = Mathf.Atan(gazeDirection.y / gazeDirection.z);
            var angleDeg = angleRad * Mathf.Rad2Deg;

            var y = Mathf.Tan(upperLimit * Mathf.Deg2Rad) * gazeDirection.z;
            if (angleDeg > upperLimit)
            {
                gazeDirection = new Vector3(gazeDirection.x, y, gazeDirection.z);
            }

            y = Mathf.Tan(-lowerLimit * Mathf.Deg2Rad) * gazeDirection.z;
            if (angleDeg < -lowerLimit)
            {
                gazeDirection = new Vector3(gazeDirection.x, y, gazeDirection.z);
            }

            return gazeDirection;
        }

        /// <summary>
        /// Clamp horizontal gaze angles - needs to be done in degrees.
        /// </summary>
        /// <param name="gazeDirection">Direction vector of the gaze.</param>
        /// <param name="limit">The limit to clamp to in degrees.</param>
        /// <returns>The clamped gaze direction.</returns>
        private static Vector3 ClampHorizontalGazeAngles(Vector3 gazeDirection, float limit)
        {
            var angleRad = Mathf.Atan(gazeDirection.x / gazeDirection.z);
            var angleDeg = angleRad * Mathf.Rad2Deg;

            var x = Mathf.Tan(limit * Mathf.Deg2Rad) * gazeDirection.z;
            if (angleDeg > limit)
            {
                gazeDirection = new Vector3(x, gazeDirection.y, gazeDirection.z);
            }

            if (angleDeg < -limit)
            {
                gazeDirection = new Vector3(-x, gazeDirection.y, gazeDirection.z);
            }

            return gazeDirection;
        }

        /// <summary>
        /// Animate facial expression blend shapes.
        /// </summary>
        /// <param name="newDirectionL">The gaze direction for the left eye.</param>
        /// <param name="newDirectionR">The gaze direction for the right eye.</param>
        /// <param name="eyeDataLeft">Eye data for the left eye.</param>
        /// <param name="eyeDataRight">Eye data for the right eye.</param>
        protected virtual void AnimateEyeLids(bool leftEyeClosed, bool rightEyeClosed)
        {
            var leftEyeBlinkValue = leftEyeClosed ? 100 : 0;
            var rightEyeBlinkValue = rightEyeClosed ? 100 : 0;

            // If eye openness has changed reset animation curve.
            if (leftEyeClosed != _leftEyeClosed)
            {
                _animationProgressL = 0;
            }
            if (rightEyeClosed != _rightEyeClosed)
            {
                _animationProgressR = 0;
            }

            _blinkL = Mathf.Lerp(_blinkL, leftEyeBlinkValue, _blendShapeAnimationCurve.Evaluate(_animationProgressL));
            _blinkR = Mathf.Lerp(_blinkR, rightEyeBlinkValue, _blendShapeAnimationCurve.Evaluate(_animationProgressR));

            _bodyBlendShape.SetBlendShapeWeight(BlendShapeLeftEyeLid, _blinkL);
            _bodyBlendShape.SetBlendShapeWeight(BlendShapeRightEyeLid, _blinkR);

            _leftEyeClosed = leftEyeClosed;
            _rightEyeClosed = rightEyeClosed;

            _animationProgressL += Time.deltaTime / _blendShapeAnimationTimeSeconds;
            _animationProgressR += Time.deltaTime / _blendShapeAnimationTimeSeconds;
        }

        #endregion
    }
}
