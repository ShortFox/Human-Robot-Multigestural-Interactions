using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class JointTask : BoxTask
    {
        public override string TaskName { get { return "joint"; } }
        public Recorder DataRecorder;

        public override void Awake()
        {
            base.Awake();
            // Load recorder and register avatar IRecordables.
            DataRecorder?.Init(new IRecordable[] {
                MyPlayer.GetComponent<AvatarDataRecorder>(),
                PartnerPlayer.GetComponent<AvatarDataRecorder>()
            });
        }

        protected override void InitRecording()
        {
            if (DataRecorder != null)
            {
                var time = DateTime.Now.ToString("dd'_'MM'_'yyyy'_'H''mm''ss");
                DataRecorder.savePath = Path.Combine(Settings.SavePath,
                    $"{Settings.ParticipantId}_T{_trialNum}_{Settings.Condition}_{_trialInfo.TrialID}_Date_{time}.csv");
                DataRecorder.StartRecording();
            }
        }

        public IEnumerator InitialMessage(float time)
        {
            yield return new WaitForSeconds(time);
            if (TaskMessage != null)
            {
                TaskMessage.SetActive(false);
            }
        }

        public override IEnumerator SetUp()
        {
            StartCoroutine(InitialMessage(5));
            yield return StartCoroutine(base.SetUp());
        }

        public override IEnumerator Run()
        {
            foreach(AvatarInputBase c in controllers)
            {
                c.Run();
            }

            Debug.Log("Start Joint Task");

            var timeout_cr = StartCoroutine(TimeoutCheck());

            InitTask();

            //Set Numbers on blocks.
            SetBlockFaces();
            TurnOff(ReadyCubes);

            bool correctChoice = false;

            //Find who is Responder
            if (_trialInfo.RoleA == "Responder")
            {
                GameObject contactObjectA = null;
                GameObject contactObjectB = null;

                //Wait for hand hit on a block object.
                while (contactObjectA == null)
                {
                    contactObjectB = GetLastContactedCube(OtherHand);
                    contactObjectA = GetLastContactedCube(MyHand);
                    yield return null;
                }

                // We shouldn't accept a response that starts before the other player
                //correctChoice = contactObjectA == _targetObj && contactObjectB != null;
                correctChoice = contactObjectA == _targetObj;
            }
            else if (_trialInfo.RoleB == "Responder")
            {
                bool done = false;
                EventHandler handler = (sender, args) => {
                    done = true;
                };
                ReplayInput input = PartnerPlayer.GetComponent<ReplayInput>();
                input.ReplayEnded += handler;
                while (!done)
                {
                    yield return null;
                }
                input.ReplayEnded -= handler;

                GameObject contactObject = GetLastContactedCube(MyHand);
                correctChoice = contactObject != null && contactObject == _targetObj;
            }
            else
            {
                Debug.LogError("Error: No Responder Role Found");
            }

            //Provide feedback to participant.
            ChangeColor(_targetObj, correctChoice ? CorrectColor : IncorrectColor);

            StopCoroutine(timeout_cr);

            Phase++;
            yield break;
        }

        public override IEnumerator Done()
        {
            yield return new WaitForSeconds(_trialFeedbackLength);
            DataRecorder?.StopRecordingAndReset();
            yield return StartCoroutine(base.Done());
        }

        protected override void LoadNextScene()
        {
            SceneLoader.Instance.LoadNewScene("BoxFinalization");
        }
    }
}
