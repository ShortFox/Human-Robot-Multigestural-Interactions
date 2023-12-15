using System.Collections;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    public class ReachTask : BoxTask
    {
        public override string TaskName { get { return "reach"; } }

        public override IEnumerator Run()
        {
            foreach(AvatarInputBase c in controllers)
            {
                c.Run();
            }

            // Turn off player's INIT Cube. Might be redundant with ReadyCubes. Need to investigate.
            TurnOff(MyHandInitCube);
            yield return new WaitForEndOfFrame();

            Debug.Log("Starting Reach Task");

            TurnOn(ReadyCubes);
            InitTask();

            //Change target object color
            ChangeColor(_targetObj, CueColor);

            //Wait for hand hit on a block object.
            Vector3 initPos = PlayerInput.polhemusTransform.position;
            GameObject contactObject = null;
            while (contactObject == null)
            {
                CheckPlayerIntent();
                contactObject = GetLastContactedCube(MyPlayer);
                yield return null;
            }

            //Provide feedback to participant.
            if (contactObject == _targetObj)
            {
                ChangeColor(contactObject, CorrectColor);
            }
            else
            {
                ChangeColor(_targetObj, UnselectedColor);
                ChangeColor(contactObject, IncorrectColor);
                //SendTrialToEnd(_trialInfo.Task);
            }

            yield return new WaitForSeconds(_trialFeedbackLength);

            //Reset objects.
            ChangeColor(contactObject, UnselectedColor);
            TurnOff(FixationObject);
            TurnOff(Blocks);
            ChangeColor(MyHandInitCube, Black);
            //TurnOff(HandInitCube);
            TurnOff(ReadyCubes);

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
    }
}
