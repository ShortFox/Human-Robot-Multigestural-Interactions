namespace MQ.MultiAgent
{
    using UnityEngine;
    using UnityEngine.UI;

    public class BoxTaskComponents : MonoBehaviour
    {
        public GameObject FixationObject;
        public GameObject DividingWall;
        public GameObject[] Blocks;
        public Text[] BlockFaces;
        public Material UnselectedColor;
        public Material CueColor;
        public Material IncorrectColor;
        public Material CorrectColor;
        public Material Black;
    }
}

