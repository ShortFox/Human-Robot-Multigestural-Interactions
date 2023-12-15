using System;
using Tobii.XR;
using Tobii.Research.Unity;
using UnityEngine;

public class EyeTrackerRecorder : MonoBehaviour, IRecordable
{
    public EyeTrackerRecorder Recorder { get { return this; }}
    public int NumberOfColumnsNeeded { get { return 24; }}

    public string[] Record()
    {
        var gazeData = VREyeTracker.Instance.LatestProcessedGazeData;

        Vector3 gazePoint = Vector3.positiveInfinity;
        string gazeObjName = null;

        if (gazeData.CombinedGazeRayWorldValid)
        {
            Ray ray = gazeData.CombinedGazeRayWorld;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                gazePoint = hit.point;
                gazeObjName = hit.transform.name;
            }
        }

        return new string[]
        {
            gazeData.Pose.Position.x.ToString("F4"),
            gazeData.Pose.Position.y.ToString("F4"),
            gazeData.Pose.Position.z.ToString("F4"),
            gazeData.Pose.Rotation.eulerAngles.x.ToString("F4"),
            gazeData.Pose.Rotation.eulerAngles.y.ToString("F4"),
            gazeData.Pose.Rotation.eulerAngles.z.ToString("F4"),
            gazeData.Left.GazeOrigin.x.ToString("F4"),
            gazeData.Left.GazeOrigin.y.ToString("F4"),
            gazeData.Left.GazeOrigin.z.ToString("F4"),
            gazeData.Left.GazeDirection.x.ToString("F4"),
            gazeData.Left.GazeDirection.y.ToString("F4"),
            gazeData.Left.GazeDirection.z.ToString("F4"),
            gazeData.Left.PupilDiameter.ToString("F4"),
            gazeData.Left.PupilDiameterValid.ToString(),
            gazeData.Right.GazeOrigin.x.ToString("F4"),
            gazeData.Right.GazeOrigin.y.ToString("F4"),
            gazeData.Right.GazeOrigin.z.ToString("F4"),
            gazeData.Right.GazeDirection.x.ToString("F4"),
            gazeData.Right.GazeDirection.y.ToString("F4"),
            gazeData.Right.GazeDirection.z.ToString("F4"),
            gazeData.Right.PupilDiameter.ToString("F4"),
            gazeData.Right.PupilDiameterValid.ToString(),
            gazeData.CombinedGazeRayWorld.origin.x.ToString("F4"),
            gazeData.CombinedGazeRayWorld.origin.y.ToString("F4"),
            gazeData.CombinedGazeRayWorld.origin.z.ToString("F4"),
            gazeData.CombinedGazeRayWorld.direction.x.ToString("F4"),
            gazeData.CombinedGazeRayWorld.direction.y.ToString("F4"),
            gazeData.CombinedGazeRayWorld.direction.z.ToString("F4"),
            gazeObjName,
            gazePoint.x.ToString("F4"),
            gazePoint.y.ToString("F4"),
            gazePoint.z.ToString("F4"),
            (gazeData.CombinedGazeRayWorldValid ? 1 : 0).ToString(),
            gazeData.TimeStamp.ToString("F4"),
        };
    }

    public string[] GetHeader()
    {
        return new string[]
        {
            "PoseX", "PoseY", "PoseZ",
            "PoseEulerX", "PoseEulerY", "PoseEulerZ",
            "LeftGazeOriginX", "LeftGazeOriginY", "LeftGazeOriginZ",
            "LeftGazeDirectionX", "LeftGazeDirectionY", "LeftGazeDirectionZ",
            "LeftPupilDiameter",
            "LeftPupilValid",
            "RightGazeOriginX", "RightGazeOriginY", "RightGazeOriginZ",
            "RightGazeDirectionX", "RightGazeDirectionY", "RightGazeDirectionZ",
            "RightPupilDiameter",
            "RightPupilValid",
            "CombinedGazeOriginX", "CombinedGazeOriginY", "CombinedGazeOriginZ",
            "CombinedGazeDirectionX", "CombinedGazeDirectionY", "CombinedGazeDirectionZ",
            "GazedObject",
            "GazedPointX", "GazedPointY", "GazedPointZ",
            "GazeValid",
            "EyeTimeStamp",
        };
    }

    public string OnRecordingExceptionCaught(Exception e)
    {
        Debug.LogAssertion($"Exception occurred when attempting to record VR Headset Info: {e.Message}");
        return e is ArgumentNullException ? "null" : "exception occured";
    }

    /// <inheritdoc/>
    public void OnGetHeaderExceptionCaught(Exception e, int failedHeaderNo)
    {
        Debug.LogAssertion($"While reading EyeTrackerRecordable's header definition an exception occured: {e.Message}" +
            $" Fail id in header name will be {failedHeaderNo}.");
    }
}
