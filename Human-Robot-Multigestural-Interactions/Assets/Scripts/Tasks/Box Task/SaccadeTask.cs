using System.Collections;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class SaccadeTask : BoxTask
    {
        public override string TaskName { get { return "saccade"; } }
        protected Recorder DataRecorder;

        public override void Awake()
        {
            base.Awake();
            // Load recorder and register avatar IRecordables.
            DataRecorder = GameObject.Find("/DataRecorder")?.GetComponent<Recorder>();
            DataRecorder.Init(new IRecordable [] {
                MyPlayer.GetComponent<AvatarDataRecorder>(),
                PartnerPlayer.GetComponent<AvatarDataRecorder>()
            });
        }

        public override IEnumerator SetUp()
        {
            yield return StartCoroutine(base.SetUp());

            // Start recording
            if (DataRecorder != null)
            {
                DataRecorder.StartRecording();
            }
        }

        public override IEnumerator Run()
        {

            foreach(AvatarInputBase c in controllers)
            {
                c.Run();
            }

            // Turn off player's INIT Cube. Might be redundant with ReadyCubes. Need to investigate.
            TurnOff(MyHandInitCube);
            yield return new WaitForEndOfFrame();

            Debug.Log("Starting Fixation Task");

            TurnOff(ReadyCubes);
            InitTask();

            //Change target object color
            ChangeColor(_targetObj, CueColor);

            //Wait for raycast hit on a block object.
            GameObject gazedObject = null;
            while (gazedObject == null)
            {
                gazedObject = GetLastGazedCube();
                yield return null;
            }

            //Provide feedback to participant.
            if (gazedObject == _targetObj)
            {
                ChangeColor(gazedObject, CorrectColor);
            }
            else //Object is incorrect. Add trial to end of task list.
            {
                ChangeColor(_targetObj, UnselectedColor);
                ChangeColor(gazedObject, IncorrectColor);
                //SendTrialToEnd(_trialInfo.Task);
            }

            yield return new WaitForSeconds(_trialFeedbackLength);

            //Reset objects.
            ChangeColor(gazedObject, UnselectedColor);
            TurnOff(FixationObject);
            TurnOff(Blocks);

            float startTime = Time.timeSinceLevelLoad;
            while(Time.timeSinceLevelLoad-startTime < _trialTimeoutLength)
            {
                yield return null;
            }

            // Provide Feedback to Participants if trial Times out
            ChangeColor(_targetObj, IncorrectColor);
            yield return new WaitForSeconds(_trialFeedbackLength);

            Phase++;
            yield break;
        }

        public override IEnumerator Done()
        {
            DataRecorder?.StopRecordingAndReset();
            yield return StartCoroutine(base.Done());
        }

    }
}
