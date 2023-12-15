namespace MQ.MultiAgent
{
    using UnityEngine;
    using Tobii.Research.Unity;
    using Tobii.XR;
    using Tobii.G2OM;

    /// <summary>
    /// Class for easy access to eye-related information. Eye control is handled in eyeController and aiController.
    /// </summary>
    public class Eyes : MonoBehaviour
    {
        public Transform HeadRoot;

        public Transform Left;
        public Vector3 LeftPos { get { return Left.localPosition; } }
        public Vector3 LeftRot { get { return Left.localEulerAngles; } }

        public Transform Right;
        public Vector3 RightPos { get { return Right.localPosition; } }
        public Vector3 RightRot { get { return Right.localEulerAngles; } }

        public Vector3 GazeLeftEye { get { return Left.forward; } }
        public Vector3 GazeRightEye { get { return Right.forward; } }

        private Vector3 _gazeLocat;
        public Vector3 GazedLocat
        {
            get
            {
                if (!IsAI)
                {
                    // _gazeLocat = Target.LatestHitPoint;
                    return _gazeLocat;
                }
                else return _gazeLocat;
            }
            set
            {
                if (!IsAI) return;
                else _gazeLocat = value;
            }
        }

        private Transform _gazedTransform;
        public Transform GazedTransform
        {
            get { return _gazedTransform; }
            set
            {
                if (!IsAI)
                {
                    try
                    {
                        _gazedTransform = Target.LatestHitObject;
                    }
                    catch
                    {
                        Debug.LogError("Unable to set Eyes.GazedTransform");
                    }
                }
                else _gazedTransform = value;
            }
        }
        private string _gazeTransformName;
        public string GazedTransformName
        {
            get
            {
                if (!IsAI)
                {
                    _gazeTransformName = (_gazedTransform == null) ? "" : _gazedTransform.name;
                }
                return _gazeTransformName;
            }
            set
            {
                _gazeTransformName = value;
            }
        }

        private VRGazeTrail _target;
        private VRGazeTrail Target
        {
            get
            {
                if (_target != null) return _target;
                else _target = VRGazeTrail.Instance;

                return _target;
            }
        }

        public bool IsAI;           //Flag to state if these eyes are AI or human-controlled. Temporary solution.

        //Properties
        public IVRGazeData LatestEyesData
        {
            get
            {
                if (IsAI)
                {
                    Debug.LogError("Error: Accessing Tobii data although this player is AI.");
                    return null;
                }
                else
                {
                    return VREyeTracker.Instance.LatestProcessedGazeData;
                }
            }
        }

        public Transform LatestGazeG2OM
        {
            get
            {
                if (IsAI)
                {
                    Debug.LogError("Error: Accessing Tobii data although this player is AI.");
                    return null;
                }
                return TobiiXR.FocusedObjects.Count == 0 ? null : TobiiXR.FocusedObjects[0].GameObject.transform;
            }
        }

        private string _header;
        public string Header
        {
            get
            {
                if (_header != null) return _header;
                else
                {
                    _header = "";
                    _header += "PoseX,PoseY,PoseZ,PoseEulerX,PoseEulerY,PoseEulerZ,";
                    _header += "LeftGazeOriginX,LeftGazeOriginY,LeftGazeOriginZ,LeftGazeDirectionX,LeftGazeDirectionY,LeftGazeDirectionZ,";
                    _header += "LeftPupilDiameter,LeftPupilValid,";
                    _header += "RightGazeOriginX,RightGazeOriginY,RightGazeOriginZ,RightGazeDirectionX,RightGazeDirectionY,RightGazeDirectionZ,";
                    _header += "RightPupilDiameter,RightPupilValid,";
                    _header += "CombinedGazeOriginX,CombinedGazeOriginY,CombinedGazeOriginZ,CombinedGazeDirectionX,CombinedGazeDirectionY,CombinedGazeDirectionZ,";
                    _header += "GazedObject,GazedPointX,GazedPointY,GazedPointZ,";
                    _header += "GazedObjG2OM,GazeValid,EyeTimeStamp";
                    return _header;
                }
            }
        }

        public string LatestDataString()
        {
            string output = "";

            //If Human Eyes.
            if (!IsAI)
            {
                IVRGazeData data = LatestEyesData;
                string gazedG2OM = LatestGazeG2OM == null ? "" : LatestGazeG2OM.name;


                //Get Gaze and Gazed Object.
                output += string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34}",
                                    data.Pose.Position.x.ToString("F4"),
                                    data.Pose.Position.y.ToString("F4"),
                                    data.Pose.Position.z.ToString("F4"),
                                    data.Pose.Rotation.eulerAngles.x.ToString("F4"),
                                    data.Pose.Rotation.eulerAngles.y.ToString("F4"),
                                    data.Pose.Rotation.eulerAngles.z.ToString("F4"),
                                    data.Left.GazeOrigin.x.ToString("F4"),
                                    data.Left.GazeOrigin.y.ToString("F4"),
                                    data.Left.GazeOrigin.z.ToString("F4"),
                                    data.Left.GazeDirection.x.ToString("F4"),
                                    data.Left.GazeDirection.y.ToString("F4"),
                                    data.Left.GazeDirection.z.ToString("F4"),
                                    data.Left.PupilDiameter.ToString("F4"),
                                    data.Left.PupilDiameterValid.ToString(),
                                    data.Right.GazeOrigin.x.ToString("F4"),
                                    data.Right.GazeOrigin.y.ToString("F4"),
                                    data.Right.GazeOrigin.z.ToString("F4"),
                                    data.Right.GazeDirection.x.ToString("F4"),
                                    data.Right.GazeDirection.y.ToString("F4"),
                                    data.Right.GazeDirection.z.ToString("F4"),
                                    data.Right.PupilDiameter.ToString("F4"),
                                    data.Right.PupilDiameterValid.ToString(),
                                    data.CombinedGazeRayWorld.origin.x.ToString("F4"),
                                    data.CombinedGazeRayWorld.origin.y.ToString("F4"),
                                    data.CombinedGazeRayWorld.origin.z.ToString("F4"),
                                    data.CombinedGazeRayWorld.direction.x.ToString("F4"),
                                    data.CombinedGazeRayWorld.direction.y.ToString("F4"),
                                    data.CombinedGazeRayWorld.direction.z.ToString("F4"),
                                    GazedTransformName,
                                    GazedLocat.x.ToString("F4"),
                                    GazedLocat.y.ToString("F4"),
                                    GazedLocat.z.ToString("F4"),
                                    gazedG2OM,
                                    (data.CombinedGazeRayWorldValid ? 1 : 0).ToString(),
                                    data.TimeStamp.ToString("F4"));
            }
            //else if it is AI
            else
            {
                //Need to verify if these two statements are correct. Need to compare to what IVRGazeData Output is.
                Vector3 CombinedPos = (Left.transform.position + Right.transform.position) / 2;
                Vector3 CombinedDirection = (Left.TransformDirection(LeftRot) + Right.TransformDirection(RightRot)) / 2;

                output += string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33},{34}",
                                    "IsAI", "IsAI", "IsAI", "IsAI", "IsAI", "IsAI",
                                    LeftPos.x.ToString("F4"),
                                    LeftPos.y.ToString("F4"),
                                    LeftPos.z.ToString("F4"),
                                    LeftRot.x.ToString("F4"),
                                    LeftRot.y.ToString("F4"),
                                    LeftRot.z.ToString("F4"),
                                    "IsAI","IsAI",
                                    RightPos.x.ToString("F4"),
                                    RightPos.y.ToString("F4"),
                                    RightPos.z.ToString("F4"),
                                    RightRot.x.ToString("F4"),
                                    RightRot.y.ToString("F4"),
                                    RightRot.z.ToString("F4"),
                                    "IsAI","IsAI",
                                    CombinedPos.x.ToString("F4"),
                                    CombinedPos.y.ToString("F4"),
                                    CombinedPos.z.ToString("F4"),
                                    CombinedDirection.x.ToString("F4"),
                                    CombinedDirection.y.ToString("F4"),
                                    CombinedDirection.z.ToString("F4"),
                                    GazedTransformName,
                                    GazedLocat.x.ToString("F4"),
                                    GazedLocat.y.ToString("F4"),
                                    GazedLocat.z.ToString("F4"),
                                    "IsAI","IsAI","IsAI");
            }
            return output;
        }

        private void Awake()
        {
            HeadRoot = this.transform.Find("mixamorig:Hips").Find("mixamorig:Spine").Find("mixamorig:Spine1").Find("mixamorig:Spine2").Find("mixamorig:Neck").Find("mixamorig:Head");

            Left = HeadRoot.Find("LeftEye");
            Right = HeadRoot.Find("RightEye");
        }
    }
}

