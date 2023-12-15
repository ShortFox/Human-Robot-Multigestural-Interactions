using UnityEngine;

/// <summary>
/// To store information that should stay throuh scenes. This includes:
///
/// MainPlayer:
///   Player GameObject, so we don't need to respawn it on each scene from scratch.
///
/// PartnerPlayer:
///   The partner will have to be located across the room from the main player.
///   This GameObject could include both a real player connected through the network or an AI agent.
///
/// </summary>
public static class AvatarInfo
{
    public static Vector3 MainPlayerPosition;
    public static Quaternion MainPlayerRotation;
    public static GameObject MainPlayer;

    public static Vector3 PartnerPlayerPosition;
    public static Quaternion PartnerPlayerRotation;
    public static GameObject PartnerPlayer;

    public static bool ShouldRotateScene;
}
