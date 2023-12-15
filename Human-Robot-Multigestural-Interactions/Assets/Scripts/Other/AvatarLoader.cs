using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class AvatarLoader : MonoBehaviour
    {
        public GameObject VRHeadset;

        private static AvatarLoader instance = null;
        public static AvatarLoader Instance
        {
            get
            {
                if ((object)instance == null)
                {
                    instance = (AvatarLoader)FindObjectOfType(typeof(AvatarLoader));

                    if (instance == null)
                    {
                        Debug.LogError(
                            "Missing AvatarLoader in open scenes. Please add an AvatarLoader and make sure it has " +
                            "been tagged as 'AvatarLoader'");
                    }
                }
                return instance;
            }
        }

        private bool initialized = false;

        void InitInfo()
        {
            Transform vr_pose = VRHeadset.transform.Find("Camera Offset/Main Camera").transform;
            var (ownLocation, partnerLocation, shouldRotateScene) = PrefabSpawn.GetRoomPlayerLocations(vr_pose);
            AvatarInfo.MainPlayerPosition = ownLocation.position;
            AvatarInfo.MainPlayerRotation = ownLocation.rotation;
            AvatarInfo.PartnerPlayerPosition = partnerLocation.position;
            AvatarInfo.PartnerPlayerRotation = partnerLocation.rotation;
            AvatarInfo.ShouldRotateScene = shouldRotateScene;
            initialized = true;
        }

        public void LoadMain()
        {
            if (!initialized)
            {
                InitInfo();
            }
            PrefabSpawn.SetUpMainAvatar(VRHeadset, transform);
        }

        public void LoadPartner(bool visible = false)
        {
            if (!initialized)
            {
                InitInfo();
            }
            PrefabSpawn.SetUpPartnerAvatar(transform, visible);
        }
    }
}
