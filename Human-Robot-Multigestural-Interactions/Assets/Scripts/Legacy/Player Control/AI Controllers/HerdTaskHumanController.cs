namespace MQ.MultiAgent
{
    using UnityEngine;

    public class HerdTaskHumanController: HumanController
    {
        public override void UpdateState()
        {
            Self.Hand.Position = new Vector3(_polController.Position.x,0.8125f, _polController.Position.z);
            Self.Hand.Rotation = Quaternion.Euler(new Vector3(0, 90 * Self.Side, 0));            //Adjust Finger rotation so that finger is pointing straight.

            //Self.Eyes.GazedLocat = _eyeController.GazedLocat;
            Self.Eyes.GazedTransform = _eyeController.GazedTransform;
        }
    }
}
