namespace MQ.MultiAgent
{
    class PepperEyes: Eyes {
        private void Awake()
        {
            if (HeadRoot is null) {
                HeadRoot = this.transform.Find("base_link/torso/Neck/Head");
            }

            // Pepper has no eye movements, so we disable this.
            Left = null;
            Right = null;
        }
    }
}