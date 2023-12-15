using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class TargetPositionPublisher : MonoBehaviour
{
    // ROS Connector
    private ROSConnection ros;

    // Variables required for ROS communication
    public string topicName = "target_position";

    public GameObject robot;
    public GameObject target;

    /// <summary>
    ///
    /// </summary>
    void Start()
    {
        // Get ROS connection static instance
        ros = ROSConnection.instance;
    }

    public void Publish()
    {
        MPose target_point_msg = new MPose(
            new MPoint(target.transform.position.x,
                       target.transform.position.y,
                       target.transform.position.z),
            new MQuaternion(target.transform.rotation.x,
                            target.transform.rotation.y,
                            target.transform.rotation.z,
                            target.transform.rotation.w)
        );

        ros.Send(topicName, target_point_msg);
    }
}
