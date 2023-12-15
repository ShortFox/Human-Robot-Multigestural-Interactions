using UnityEngine;

namespace RootMotion.FinalIK
{
    /// <summary>
    /// IK solver for head and eyes, where only eye and head positions are accounted for, ignoring intermediate joints.
    /// </summary>
    public class Playback_IKSolverLookAt : IKSolverLookAt
    {

        //Head Look target;
        [HideInInspector] public Vector3 Head_Target;
        //Eyes Look target;
        [HideInInspector] public Vector3 Eyes_Target;

        // Solving the spine hierarchy
        protected override void SolveSpine()
        {
            if (bodyWeight <= 0) return;
            if (spineIsEmpty) return;

            // Get the look at vectors for each bone
            Vector3 targetForward =
                (Head_Target + spineTargetOffset - spine[spine.Length - 1].transform.position).normalized;

            GetForwards(ref spineForwards, spine[0].forward, targetForward, spine.Length, clampWeight);

            // Rotate each bone to face their look at vectors
            for (int i = 0; i < spine.Length; i++)
            {
                spine[i].LookAt(spineForwards[i], bodyWeight * IKPositionWeight);
            }
        }

        // Solving the head rotation
        protected override void SolveHead()
        {
            if (headWeight <= 0) return;
            if (headIsEmpty) return;

            // Get the look at vector for the head
            Vector3 baseForward = spine.Length > 0 && spine[spine.Length - 1].transform != null
                ? spine[spine.Length - 1].forward
                : head.forward;

            Vector3 targetForward = Vector3.Lerp(
                    baseForward,
                    (Head_Target - head.transform.position).normalized,
                    headWeight * IKPositionWeight)
                .normalized;
            GetForwards(ref headForwards, baseForward, targetForward, 1, clampWeightHead);

            // Rotate the head to face its look at vector
            head.LookAt(headForwards[0], headWeight * IKPositionWeight);
        }

        // Solving the eye rotations
        protected override void SolveEyes()
        {
            if (eyesWeight <= 0) return;
            if (eyesIsEmpty) return;

            for (int i = 0; i < eyes.Length; i++)
            {
                // Get the look at vector for the eye
                Vector3 baseForward = head.transform != null ? head.forward : eyes[i].forward;
                GetForwards(
                    ref eyeForward,
                    baseForward,
                    (Eyes_Target - eyes[i].transform.position).normalized, 1, clampWeightEyes);

                // Rotate the eye to face its look at vector
                eyes[i].LookAt(eyeForward[0], eyesWeight * IKPositionWeight);
            }
        }
    }
}
