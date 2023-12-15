using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MQ.MultiAgent
{
    public class CenterPlayer : MonoBehaviour
    {
        [SerializeField] Button _centerPlayerButton;

        // Start is called before the first frame update
        void Start()
        {
            _centerPlayerButton.onClick.AddListener(delegate {
                if (AvatarInfo.MainPlayer != null)
                {
                    AvatarInfo.MainPlayer.GetComponent<VRInput>()?.CenterPlayer();
                }
            });
        }
    }
}
