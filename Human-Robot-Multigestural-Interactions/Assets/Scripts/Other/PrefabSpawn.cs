using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class PrefabSpawn
    {
        public struct AbstractTransform
        {
            public AbstractTransform(Vector3 pos, Quaternion rot)
            {
                position = pos;
                rotation = rot;
            }
            public Vector3 position;
            public Quaternion rotation;
        }

        public static GameObject SpawnVRAvatar(
            Object prefab,
            Vector3 pos,
            Quaternion rot,
            Transform parent,
            GameObject xr_rig,
            bool active = true)
        {
            GameObject obj = GameObject.Instantiate(prefab, pos, rot, parent) as GameObject;
            MQ.MultiAgent.VRInput input = obj.GetComponent<MQ.MultiAgent.VRInput>();
            input.xrRig = xr_rig;
            obj.SetActive(active);
            return obj;
        }

        public static GameObject SpawnAIAvatar(
            Object prefab,
            Vector3 pos,
            Quaternion rot,
            Transform parent,
            Transform partner_head,
            bool active = true)
        {
            GameObject obj = GameObject.Instantiate(prefab, pos, rot, parent) as GameObject;
            MQ.MultiAgent.ReplayInput replay = obj.GetComponent<MQ.MultiAgent.ReplayInput>();
            replay.PartnerHead = partner_head;
            obj.SetActive(active);
            return obj;
        }

        /// <summary>
        /// This method returns the required locations for player and partner spawns given vr location in lab room.
        /// The locations are harcoded and calibrated to vr room.
        /// </summary>
        public static (AbstractTransform, AbstractTransform, bool) GetRoomPlayerLocations(Transform vr_headset)
        {
            //Find out where the partner should be placed in the room (opposite from headset)
            AbstractTransform[] _candidateOffets = new AbstractTransform[] {
                new AbstractTransform(new Vector3(0, 0.82f, 0.4f), Quaternion.Euler(new Vector3(0, 180, 0))),
                new AbstractTransform(new Vector3(0, 0.82f, -0.4f), Quaternion.Euler(new Vector3(0, 0, 0)))
                };

            AbstractTransform ownLocation;
            AbstractTransform partnerLocation;

            bool shouldRotate = false;

            if (Vector3.Distance(vr_headset.position, _candidateOffets[0].position) >
                Vector3.Distance(vr_headset.position, _candidateOffets[1].position))
            {
                ownLocation = _candidateOffets[1];
                partnerLocation = _candidateOffets[0];
            }
            else
            {
                ownLocation = _candidateOffets[0];
                partnerLocation = _candidateOffets[1];
                shouldRotate = true;
            }

            return (ownLocation, partnerLocation, shouldRotate);
        }

        public static void SetUpMainAvatar(GameObject vr_headset, Transform parent)
        {
            // We should destroy previous avatar, then spawn a new one.
            if (AvatarInfo.MainPlayer != null)
            {
                GameObject.DestroyImmediate(AvatarInfo.MainPlayer);
            }

            AvatarInfo.MainPlayer = PrefabSpawn.SpawnVRAvatar(
                    Resources.Load($"BoxTask/Avatars/{Settings.ParticipantAvatar}"),
                    AvatarInfo.MainPlayerPosition,
                    AvatarInfo.MainPlayerRotation,
                    parent,
                    vr_headset);
        }

        public static void SetUpPartnerAvatar(Transform parent, bool visible = false)
        {
            if (AvatarInfo.PartnerPlayer != null)
            {
                GameObject.DestroyImmediate(AvatarInfo.PartnerPlayer);
            }

            AvatarInfo.PartnerPlayer = PrefabSpawn.SpawnAIAvatar(
                Resources.Load($"BoxTask/Avatars/{Settings.PartnerAvatar}"),
                AvatarInfo.PartnerPlayerPosition,
                AvatarInfo.PartnerPlayerRotation,
                parent,
                AvatarInfo.MainPlayer.transform.Find("AvatarTargets/HeadPose"),
                visible);
        }
    }
}
