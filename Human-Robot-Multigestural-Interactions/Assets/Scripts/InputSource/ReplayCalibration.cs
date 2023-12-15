using MQ.MultiAgent.Box;
using UnityEngine;

/// <summary>
/// The Playback trajectories were once recorded via global coordinates.
/// By using this script in combination with the predefined prefab "Playback Calibration Object" you can move the parent object such
/// that the head and cubes match. You can change position, rotation and scale, but watch out with non-uniform scaling which leads
/// to stretched or squeezed space. Afterwards refer to this object and call <see cref="Calibrate"/> to transform the trajectory sample into
/// the relative space.
/// !! Please note that rotations might be inaccurate if this object is scaled non-uniformly (uniformly scaled would be: scale_x == scale_y == scale_z) !!
/// </summary>
public class ReplayCalibration : MonoBehaviour
{
    /// <summary>
    /// Makes the PlaybackInfo instance relative to the calibration objects position, rotation and scale.
    /// </summary>
    /// <param name="info">the info object to calibrate</param>
    public void Calibrate(ref PlaybackInfo info)
    {
        // Create transformation matrix for position, rotation and scale (transform.InverseTransformPoint(point) does not seem to account for scale.
        var transformationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale); 
        
        // Multiply all relevant fields with the obtained matrix
        info.HeadPos = transformationMatrix.MultiplyPoint3x4(info.HeadPos);
        info.HeadPosX = info.HeadPos.x;
        info.HeadPosY = info.HeadPos.y;
        info.HeadPosZ = info.HeadPos.z;
        
        // Multiplying two Quaternions q1 * q2 results in q1 being rotated by the rotation of q2.  
        info.HeadRot = (transform.rotation * Quaternion.Euler(info.HeadRot)).eulerAngles;
        info.HeadRotX = info.HeadRot.x;
        info.HeadRotY = info.HeadRot.y;
        info.HeadRotZ = info.HeadRot.z;
        
        info.HeadTarget = transformationMatrix.MultiplyPoint3x4(info.HeadTarget);
        
        info.HandPos = transformationMatrix.MultiplyPoint3x4(info.HandPos);
        info.HandPosX = info.HandPos.x;
        info.HandPosY = info.HandPos.y;
        info.HandPosZ = info.HandPos.z;

        // Multiplying two Quaternions q1 * q2 results in q1 being rotated by the rotation of q2.  
        info.HandRot = (transform.rotation * Quaternion.Euler(info.HandRot)).eulerAngles;;
        info.HandRotX = info.HandRot.x;
        info.HandRotY = info.HandRot.y;
        info.HandRotZ = info.HandRot.z;

        if (info.GazePos != null){
            info.GazePos = transformationMatrix.MultiplyPoint3x4(info.GazePos);
            info.GazePosX = info.GazePos.x;
            info.GazePosY = info.GazePos.y;
            info.GazePosZ = info.GazePos.z;
        }
        else // Not sure if necessary but better save than sorry
            Debug.Log("Found not existing gaze pos");
    }

    
    private void OnDrawGizmosSelected()
    {
        // Draws all children of the calibration object (Cubes and Head) if selected in the hierarchy
        foreach (Transform child in this.transform)
        {
            if (child.name.Equals("Head"))
                Gizmos.DrawWireSphere(child.position, child.lossyScale.x);
            else // "name" contains "Cube"
                Gizmos.DrawWireCube(child.position, child.lossyScale);
        }
    }
}