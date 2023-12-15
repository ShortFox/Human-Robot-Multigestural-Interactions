namespace MQ.MultiAgent
{
    using UnityEngine;

    public class HumanAvatar: Avatar
    {
        new public Transform Body
        {
            get { return this.transform.Find("Avatar").Find("mixamorig:Hips").Find("mixamorig:Spine"); }
        }
        new public Transform Face
        {
            get
            {
                return Body
                        .Find("mixamorig:Spine1")
                        .Find("mixamorig:Spine2")
                        .Find("mixamorig:Neck")
                        .Find("mixamorig:Head");
            }
        }
        new public Transform Shoulder
        {
            get
            {
                return Body
                        .Find("mixamorig:Spine1")
                        .Find("mixamorig:Spine2")
                        .Find("mixamorig:RightShoulder")
                        .Find("mixamorig:RightArm");
            }
        }
        new public Transform Hand
        {
            get
            {
                return Shoulder
                        .Find("mixamorig:RightForeArm")
                        .Find("mixamorig:RightHand")
                        .Find("mixamorig:RightHandIndex1");
            }
        }
    }
}