namespace MQ.MultiAgent
{
    class IcubEyes: Eyes {
        private void Awake()
        {
            if (HeadRoot is null) {
                HeadRoot = this.transform.Find("base_link/root_link/torso_1/torso_2/chest/neck_1/neck_2/head");
            }
            if (Left is null) {
                Left = HeadRoot.Find("eye_l_0/eye_l");
            }
            if (Right is null) {
                Right = HeadRoot.Find("eye_r_0/eye_r");
            }
        }
    }
}