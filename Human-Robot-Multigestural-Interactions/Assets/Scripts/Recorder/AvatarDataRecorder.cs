using System;
using System.Collections.Generic;
using UnityEngine;
using MQ.MultiAgent;

public class AvatarDataRecorder : MonoBehaviour, IRecordable
{
    public AvatarDataRecorder Recorder { get { return this; }}
    public string AvatarName;
    public Transform Head;
    public Transform Hand;  // For unknown reason, accessing the Hand for the AI partner does not update the position.
    public Transform LeftEye;
    public Transform RightEye;
    public Transform GazeTarget;

    Hand Hand_hand;

    Transform PolhemusPosition;
    bool RecordPolhemus = false;

    void Awake()
    {
        PolhemusPosition = gameObject.transform.Find("AvatarTargets").Find("IndexFinger");
        if (PolhemusPosition.gameObject.GetComponent<MQ.MultiAgent.PolhemusController>() != null)
        {
            RecordPolhemus = true;
        }
        Hand_hand = Hand.GetComponent<Hand>();
    }

    public int NumberOfColumnsNeeded { get { return RecordPolhemus ? 14 : 8; }}

    public string[] Record()
    {
        var GeneralData = new List<string>
        {
            Head.position.x.ToString("F4"),
            Head.position.y.ToString("F4"),
            Head.position.z.ToString("F4"),
            Head.eulerAngles.x.ToString("F4"),
            Head.eulerAngles.y.ToString("F4"),
            Head.eulerAngles.z.ToString("F4"),
            LeftEye.position.x.ToString("F4"),
            LeftEye.position.y.ToString("F4"),
            LeftEye.position.z.ToString("F4"),
            LeftEye.rotation.x.ToString("F4"),
            LeftEye.rotation.y.ToString("F4"),
            LeftEye.rotation.z.ToString("F4"),
            LeftEye.forward.x.ToString("F4"),
            LeftEye.forward.y.ToString("F4"),
            LeftEye.forward.z.ToString("F4"),
            RightEye.position.x.ToString("F4"),
            RightEye.position.y.ToString("F4"),
            RightEye.position.z.ToString("F4"),
            RightEye.rotation.x.ToString("F4"),
            RightEye.rotation.y.ToString("F4"),
            RightEye.rotation.z.ToString("F4"),
            RightEye.forward.x.ToString("F4"),
            RightEye.forward.y.ToString("F4"),
            RightEye.forward.z.ToString("F4"),
            GazeTarget.position.x.ToString("F4"),
            GazeTarget.position.y.ToString("F4"),
            GazeTarget.position.z.ToString("F4"),
            GazeObject(),
            Hand_hand.Position.x.ToString(),
            Hand_hand.Position.y.ToString(),
            Hand_hand.Position.z.ToString(),
            Hand_hand.Rotation.eulerAngles.x.ToString(),
            Hand_hand.Rotation.eulerAngles.y.ToString(),
            Hand_hand.Rotation.eulerAngles.z.ToString(),
            Hand_hand.ContactName,
            PointObject(),
        };

        if (RecordPolhemus)
        {
            var PolData = new List<string>
            {
                PolhemusPosition.position.x.ToString(),
                PolhemusPosition.position.y.ToString(),
                PolhemusPosition.position.z.ToString(),
                PolhemusPosition.rotation.eulerAngles.x.ToString(),
                PolhemusPosition.rotation.eulerAngles.y.ToString(),
                PolhemusPosition.rotation.eulerAngles.z.ToString(),
            };
            PolData.AddRange(GeneralData);
            return PolData.ToArray();
        }
        else
        {
            return GeneralData.ToArray();
        }
    }

    public string[] GetHeader()
    {
        var PolHeader = new List<string>
        {
            $"{AvatarName}_PolhPosX", $"{AvatarName}_PolhPosY", $"{AvatarName}_PolhPosZ",
            $"{AvatarName}_PolhEulerX", $"{AvatarName}_PolhEulerY", $"{AvatarName}_PolhEulerZ",
        };

        var GeneralHeader = new List<string>
        {
            $"{AvatarName}_HeadPosX", $"{AvatarName}_HeadPosY", $"{AvatarName}_HeadPosZ",
            $"{AvatarName}_HeadEulerX", $"{AvatarName}_HeadEulerY", $"{AvatarName}_HeadEulerZ",

            $"{AvatarName}_LeftEyePosX", $"{AvatarName}_LeftEyePosY", $"{AvatarName}_LeftEyePosZ",
            $"{AvatarName}_LeftEyeEulerX", $"{AvatarName}_LeftEyeEulerY", $"{AvatarName}_LeftEyeEulerZ",
            $"{AvatarName}_LeftEyeGazeVectorX", $"{AvatarName}_LeftEyeGazeVectorY", $"{AvatarName}_LeftEyeGazeVectorZ",

            $"{AvatarName}_RightEyePosX", $"{AvatarName}_RightEyePosY", $"{AvatarName}_RightEyePosZ",
            $"{AvatarName}_RightEyeEulerX", $"{AvatarName}_RightEyeEulerY", $"{AvatarName}_RightEyeEulerZ",
            $"{AvatarName}_RightEyeGazeVectorX", $"{AvatarName}_RightEyeGazeVectorY",
            $"{AvatarName}_RightEyeGazeVectorZ",

            $"{AvatarName}_GazeTargetPosX", $"{AvatarName}_GazeTargetPosY", $"{AvatarName}_GazeTargetPosZ",
            $"{AvatarName}_GazeTargetObject",

            $"{AvatarName}_HandPosX", $"{AvatarName}_HandPosY", $"{AvatarName}_HandPosZ",
            $"{AvatarName}_HandEulerX", $"{AvatarName}_HandEulerY", $"{AvatarName}_HandEulerZ",
            $"{AvatarName}_HandContactObj",
            $"{AvatarName}_HandPointObj"
        };

        if (RecordPolhemus)
        {
            PolHeader.AddRange(GeneralHeader);
            return PolHeader.ToArray();
        }
        else
        {
            return GeneralHeader.ToArray();
        }
    }

    string PointObject()
    {
        return RaycastHitObject(Hand.position, Hand.TransformDirection(Vector3.right));
    }

    string GazeObject()
    {
        Vector3 center = (LeftEye.position + RightEye.position) / 2;
        Vector3 direction = (GazeTarget.position - center).normalized;
        return RaycastHitObject(center, direction);
    }

    string RaycastHitObject(Vector3 origin, Vector3 direction)
    {

        // Create 20 cm offset so that colliders don't hit head for iCub

        string output = "";
        RaycastHit hitinfo;
        if (Physics.Raycast(origin+direction*0.2f, direction, out hitinfo, 20))
        {
            output = hitinfo.transform.name;
        }
        return output;
    }

    public string OnRecordingExceptionCaught(Exception e)
    {
        Debug.LogAssertion($"Exception occurred when attempting to record data of {AvatarName} avatar: {e.Message}");
        return e is ArgumentNullException ? "null" : "exception occured";
    }

    /// <inheritdoc/>
    public void OnGetHeaderExceptionCaught(Exception e, int failedHeaderNo)
    {
        Debug.LogAssertion($"While reading AvatarRecordable's header definition an exception occured: {e.Message}" +
            $" Fail id in header name will be {failedHeaderNo}.");
    }
}
