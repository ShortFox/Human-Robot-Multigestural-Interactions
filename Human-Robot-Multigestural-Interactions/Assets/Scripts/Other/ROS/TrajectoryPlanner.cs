using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.IcubRos;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;


class MoveGroup : MonoBehaviour
{
    public void Initialize(string s_name) {
        ros = ROSConnection.instance;
        jointArticulations = new Dictionary<string, ArticulationBody>();
        directRotationRPY = new List<string>();
        group_name = s_name;
        rosServiceName = $"icub_moveit_{group_name}";
    }

    public string group_name;
    private ROSConnection ros;
    public string rosServiceName;
    private readonly float jointAssignmentWait = 0.1f;

    public Dictionary<string, ArticulationBody> jointArticulations;
    private List<string> directRotationRPY;
    private List<int> directRotationRPYSigns;

    /// <summary>
    ///     Get the current values of the robot's joint angles.
    /// </summary>
    /// <returns>iCubMoveitJoints</returns>
    public MMoveServiceRequest CurrentJointConfig()
    {
        MMoveServiceRequest request = new MMoveServiceRequest();
        List<double> joints = new List<double>();
        List<string> names = new List<string>();

        foreach (KeyValuePair<string, ArticulationBody> joint in jointArticulations) {
            joints.Add(joint.Value.xDrive.target * Mathf.Deg2Rad);
            names.Add(joint.Key);
        }

        request.link_names = names.ToArray();
        request.joint_positions = joints.ToArray();

        return request;
    }

    // Assign a joint to roll, pitch and yaw respectively.
    // jointOrder is an array of 3 strings with the names of the assigned joints for each movement.
    // Examples:
    // ["neck_1", "neck_2", "head"] -> "neck_1" drives roll, "neck_2" drives pitch, "head" drives yaw
    // ["neck_1", "", "head"] -> "neck_1" drives roll, "head" drives yaw
    public void SetRotationToJointRelation(string[] jointOrder, int[] signs = null) {
        directRotationRPY = jointOrder.ToList();
        if (signs != null) {
            directRotationRPYSigns = signs.ToList();
        } else {
            directRotationRPYSigns = new int[] {1, -1, 1}.ToList();
        }
    }

    float EulerToJoint(float angle, int sign) {
        angle = angle < 180 ? angle : angle - 360;
        angle *= sign;
        return angle;
    }
    // Controller for head and eye movements.
    // Simple rotation movements can be direcly mapped to rotation angles in the joint space of the head and eyes.
    public void FollowPose(Transform target_pose) {
        var rotation_euler = target_pose.rotation.eulerAngles;
        for (int i = 0; i < 3; i++ ){
            if (jointArticulations.ContainsKey(directRotationRPY[i])){
                var angle = target_pose.rotation.eulerAngles[i];
                var sign = directRotationRPYSigns[i];
                UpdateArticulationXDrive(jointArticulations[directRotationRPY[i]], EulerToJoint(angle, sign));
            }
        }
    }

    void UpdateArticulationXDrive(ArticulationBody articulation, float value)
    {
        var locXdrive = articulation.xDrive;
        locXdrive.target = Mathf.Max(Mathf.Min(value, locXdrive.upperLimit), locXdrive.lowerLimit);
        articulation.xDrive = locXdrive;
    }

    /// <summary>
    ///     Execute the returned trajectories from the MoverService.
    ///
    ///     The expectation is that the MoverService will return four trajectory plans,
    ///         PreGrasp, Grasp, PickUp, and Place,
    ///     where each plan is an array of robot poses. A robot pose is the joint angle values
    ///     of the six robot joints.
    ///
    ///     Executing a single trajectory will iterate through every robot pose in the array while updating the
    ///     joint values on the robot.
    ///
    /// </summary>
    /// <param name="response"> MoverServiceResponse received from iCub_moveit mover service running in ROS</param>
    /// <returns></returns>
    private IEnumerator ExecuteTrajectories(MMoveServiceResponse response)
    {
        if (response.trajectories != null)
        {
            // For every trajectory plan returned
            for (int poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
            {
                var jointNames = response.trajectories[poseIndex].joint_trajectory.joint_names.ToList();
                // For every robot pose in trajectory plan
                for (int jointConfigIndex = 0; jointConfigIndex < response.trajectories[poseIndex].joint_trajectory.points.Length; jointConfigIndex++)
                {
                    var jointPositions = response.trajectories[poseIndex].joint_trajectory.points[jointConfigIndex].positions;
                    float[] result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    // Set the joint values for every joint
                    foreach ((int idx, string joint) in jointNames.Select((item, index) => (index , item)))
                    {
                        UpdateArticulationXDrive(jointArticulations[joint], result[idx]);
                    }
                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(jointAssignmentWait);
                }
            }
        }
    }

    public void RequestTrajectoryToPose(MPose pose) {
        MMoveServiceRequest request = CurrentJointConfig();
        request.target_pose = pose;
        ros.SendServiceMessage<MMoveServiceResponse>(rosServiceName, request, TrajectoryResponse);
    }

    public void RequestTrajectoryToOrientation(MQuaternion rotation) {
        RequestTrajectoryToPose(new MPose(new MPoint(), rotation));
    }

    void TrajectoryResponse(MMoveServiceResponse response)
    {
        if (response.trajectories.Length > 0)
        {
            Debug.Log("Trajectory returned.");
            StartCoroutine(ExecuteTrajectories(response));
        }
        else
        {
            Debug.LogError("No trajectory returned from MoverService.");
        }
    }

}



public class TrajectoryPlanner : MonoBehaviour
{
    // Hardcoded variables
    private readonly Vector3 pickPoseOffset = Vector3.up * 0.1f;

    // Accesible world objects
    public GameObject target;
    public GameObject headDrive;
    public GameObject eyeLDrive;
    public GameObject eyeRDrive;
    public float TargetPoseX;
    public float TargetPoseY;
    public float TargetPoseZ;
    public float TargetRotationX;
    public float TargetRotationY;
    public float TargetRotationZ;

    // Articulation Bodies
    private MoveGroup rightArmGroup;
    private MoveGroup headGroup;
    private MoveGroup leftEyeGroup;
    private MoveGroup rightEyeGroup;

    /// <summary>
    ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
    ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
    ///
    ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
    ///     execute the trajectories in a coroutine.
    /// </summary>
    public void RequestTrajectory()
    {
        rightArmGroup.RequestTrajectoryToPose(new MPose(
            target.transform.position.To<FLU>(),
            target.transform.rotation.To<FLU>()
        ));
    }

    void AddJointChain(ref Dictionary<string, ArticulationBody> res, string root, List<string> links)
    {
        foreach (string joint in links) {
            ArticulationBody articulation = gameObject.transform.Find(root += $"/{joint}").GetComponent<ArticulationBody>();
            // MoveIt does not handle fixed joints.
            if (articulation.jointType != ArticulationJointType.FixedJoint) {
                res.Add(joint, articulation);
            }
        }
    }

    void GetIcubArm(string arm, ref Dictionary<string, ArticulationBody> articulations)
    {
        // Articulations in the arm.
        List<string> joints = new List<string>(new string[] {
            $"{arm}_shoulder_1",
            $"{arm}_shoulder_2",
            $"{arm}_shoulder_3",
            $"{arm}_upper_arm",
            $"{arm}_elbow_1",
            $"{arm}_forearm",
            $"{arm}_wrist_1",
            $"{arm}_hand",
            $"{arm}_gripper",
            });

        string base_link = $"base_link/root_link/torso_1/torso_2/chest";

        AddJointChain(ref articulations, base_link, joints);
    }

    Dictionary<string, ArticulationBody> GetHeadArticulation()
    {
        List<string> joints = new List<string>(new string[] {"neck_1", "neck_2", "head"});
        string base_link = $"base_link/root_link/torso_1/torso_2/chest";
        Dictionary<string, ArticulationBody> res = new Dictionary<string, ArticulationBody>();
        AddJointChain(ref res, base_link, joints);
        return res;
    }

    Dictionary<string, ArticulationBody> GetEyeArticulation(string side) {
        List<string> joints = new List<string>(new string[] {$"eye_{side}_0", $"eye_{side}"});
        string base_link = $"base_link/root_link/torso_1/torso_2/chest/neck_1/neck_2/head";
        Dictionary<string, ArticulationBody> res = new Dictionary<string, ArticulationBody>();
        AddJointChain(ref res, base_link, joints);
        return res;
    }

    /// <summary>
    ///     Find all robot joints in Awake() and add them to the jointArticulationBodies array.
    ///     Find left and right finger joints and assign them to their respective articulation body objects.
    /// </summary>
    void Start()
    {
        rightArmGroup = gameObject.AddComponent<MoveGroup>();
        headGroup = gameObject.AddComponent<MoveGroup>();
        leftEyeGroup = gameObject.AddComponent<MoveGroup>();
        rightEyeGroup = gameObject.AddComponent<MoveGroup>();
        rightArmGroup.Initialize("RightArm");
        headGroup.Initialize("Head");
        leftEyeGroup.Initialize("LeftEye");
        rightEyeGroup.Initialize("RightEye");


        GetIcubArm("r", ref rightArmGroup.jointArticulations);

        headGroup.jointArticulations = GetHeadArticulation();
        headGroup.SetRotationToJointRelation(new string[] {"neck_1", "head", "neck_2"});
        leftEyeGroup.jointArticulations = GetEyeArticulation("l");
        leftEyeGroup.SetRotationToJointRelation(new string[] {"eye_l", $"eye_l_0", ""},
                                                new int[] {-1, -1, 0});
        rightEyeGroup.jointArticulations = GetEyeArticulation("r");
        rightEyeGroup.SetRotationToJointRelation(new string[] {"eye_r", $"eye_r_0", ""},
                                                 new int[] {-1, -1, 0});

        return;
    }

    void Update() {
        headGroup.FollowPose(headDrive.transform);
        leftEyeGroup.FollowPose(eyeLDrive.transform);
        rightEyeGroup.FollowPose(eyeRDrive.transform);
    }


    public void RunTests() {
        TestTargetPosition();
    }

    void TestTargetPosition() {
        var q = new Quaternion();
        q.eulerAngles = new Vector3(TargetRotationX, TargetRotationY, TargetRotationZ);
        rightArmGroup.RequestTrajectoryToPose(
            new MPose(
                new MPoint(TargetPoseX, TargetPoseY, TargetPoseZ),
                new MQuaternion(q.x, q.y, q.z, q.w)
            )
        );

        headGroup.RequestTrajectoryToOrientation(
            new MQuaternion(
                headDrive.transform.rotation.To<FLU>().x,
                headDrive.transform.rotation.To<FLU>().y,
                headDrive.transform.rotation.To<FLU>().z,
                headDrive.transform.rotation.To<FLU>().w
            )
        );

    }

    void PointBottomRight() {
        var q = new Quaternion();
        q.eulerAngles = new Vector3(90, 0, -180);
        rightArmGroup.RequestTrajectoryToPose(new MPose(
            new MPoint(-0.25, 0.21, 0),
            new MQuaternion(q.x, q.y, q.z, q.w)
        ));
    }

    void PointBottomLeft() {
        var q = new Quaternion();
        q.eulerAngles = new Vector3(90, 0, -180);
        rightArmGroup.RequestTrajectoryToPose(new MPose(
            new MPoint(-0.25, 0.0, 0),
            new MQuaternion(q.x, q.y, q.z, q.w)
        ));
    }

    void PointBottomRightExtend() {
        var q = new Quaternion();
        q.eulerAngles = new Vector3(135, 0, -135);
        rightArmGroup.RequestTrajectoryToPose(new MPose(
            new MPoint(-0.21, 0.35, 0),
            new MQuaternion(q.x, q.y, q.z, q.w)
        ));
    }
}