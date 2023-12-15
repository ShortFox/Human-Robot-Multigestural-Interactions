using UnityEngine;
using RootMotion.FinalIK;


namespace MQ.MultiAgent
{
    /// <summary>
    /// Manage Icub's head and eyes using Final IK.
    /// </summary>
    [RequireComponent(typeof(IcubEyes))]
    [RequireComponent(typeof(FullBodyBipedIK))]
    public class IcubAvatarControl : MonoBehaviour
    {
        Playback_IKSolverLookAt customSolver;

        public Transform GazeTarget;
        public Transform HeadTransform;

        LookAtIK gaze;
        Vector3 gazeOrigin;
        Ray ray;
        RaycastHit lastHit;

        IcubEyes eyes;
        FullBodyBipedIK body;

        private void Start()
        {
            // Create solver for gaze control (head and eye movements) of iCub
            eyes = gameObject.GetComponent<IcubEyes>();
            body = gameObject.GetComponent<FullBodyBipedIK>();
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

        /// <summary>Calculate gaze location and paint a ray to represent it.</summary>
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
            }
        }
    }
}
