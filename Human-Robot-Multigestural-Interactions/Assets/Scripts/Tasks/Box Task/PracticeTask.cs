using System.Collections;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class PracticeTask : JointTask
    {
        public int NumberOfTrials = 3;
        public override string TaskName { get { return "practice"; } }

        protected override void Start()
        {
            base.Start();
            TurnOn(DividingWall);
            LoadingIndicator.Instance.SetAnimation(true);
        }

        protected override void InitRecording() {}

        public override IEnumerator SetUp()
        {
            if (Settings.PracticeTrialCount is int tc && tc > 0)
            {
                NumberOfTrials = tc;
            }

            yield return new WaitForSeconds(5);
            if (TaskMessage != null)
            {
                TaskMessage.SetActive(false);
            }

            yield return StartCoroutine(base.SetUp());
        }

        protected override void LoadNextScene()
        {
            SceneLoader.Instance.LoadNewScene("BoxExperiment");
        }

        protected override void LoadTrials()
        {
            Trials = ExpInitializer.Instance.GetTrialList(NumberOfTrials, false, "joint", "Initiator");
        }
    }
}
