using UnityEngine;

public static class ExtensionMethods
{
    public static Object Instantiate(
        this Object thisObj,
        Object original,
        Vector3 position,
        Quaternion rotation,
        GameObject xr_rig)
    {
        GameObject avatar = Object.Instantiate(original, position, rotation) as GameObject;
        MQ.MultiAgent.VRInput input = avatar.GetComponent<MQ.MultiAgent.VRInput>();
        input.xrRig = xr_rig;
        return avatar;
    }
}
