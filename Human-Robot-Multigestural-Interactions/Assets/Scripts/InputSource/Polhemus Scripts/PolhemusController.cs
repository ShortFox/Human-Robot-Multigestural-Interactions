using UnityEngine;

namespace MQ.MultiAgent
{
    /// <summary>
    /// Processes Polhemus Data to be used with experiments.
    /// Note, some values (e.g., posDimIndx, rotDimIndx, candidateOffsets are determined based on room and Polhemus
    /// base station setup.
    /// </summary>
    public class PolhemusController : PolhemusStream
    {
        //Position and rotation of Polhemus sensor in Unity coordinates
        public Vector3 Position { get { return gameObject.transform.position; } }
        public Quaternion Rotation { get { return gameObject.transform.rotation; } }

        //When this is set to true, Polhemus stream is initialized. (May need to handle this better).
        private bool _isActive;
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if (value && !_isActive)
                {
                    Debug.Log("Activating Polhemus Controller");
                    base.Initialize();
                    _isActive = value;
                }
            }
        }

        //Polhemus-related variables
        //Sensor index of first Sensor.
        private int _sensorIndx = 0;

        //Polhemus Stream Data Index for Position.
        private int[] _posDimIndx = new int[3] { 0, 2, 1 };

        //Polhemus Stream Data Index for Rotation.
        private int[] _rotDimIndx = new int[4] { 0, 1, 3, 2 };

        //Axis Sign Correction for Position.
        private Vector3 _posAxisCorrection = new Vector3(1, -1, -1);

        //Axis Sign Correction for Rotation.
        private Vector4 _orientAxisCorrection = new Vector4(1, -1, 1, 1);

        //Unit Conversion from Inch to Meter.
        private float _unitConversion = 0.0254f;

        //The zero-ed position of sensor.
        private Vector3 _center;

        //The offset for Sensor.
        private Vector3 _offset;
        public Vector3 Offset { get { return _offset; } }

        #region Unity Methods
        private void OnEnable()
        {
            IsActive = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isActive) UpdateState();

        }
        #endregion

        #region Helper Methods
        void UpdateState()
        {
            if (active[_sensorIndx])
            {
                Vector3 pol_position = positions[_sensorIndx] - _center;
                Vector3 unity_position;
                unity_position.x = pol_position[_posDimIndx[0]] * _posAxisCorrection[0];
                unity_position.y = pol_position[_posDimIndx[1]] * _posAxisCorrection[1];
                unity_position.z = pol_position[_posDimIndx[2]] * _posAxisCorrection[2];

                Vector4 pol_rotation = orientations[_sensorIndx];
                Quaternion unity_rotation;
                unity_rotation.w = pol_rotation[_rotDimIndx[0]] * _orientAxisCorrection[0];
                unity_rotation.x = pol_rotation[_rotDimIndx[1]] * _orientAxisCorrection[1];
                unity_rotation.y = pol_rotation[_rotDimIndx[2]] * _orientAxisCorrection[2];
                unity_rotation.z = pol_rotation[_rotDimIndx[3]] * _orientAxisCorrection[3];

                gameObject.transform.SetPositionAndRotation((unity_position * _unitConversion) + _offset, unity_rotation);

                //Unclear what the following conditional refers to but it is used in Polhemus demo script.
                if (digio[_sensorIndx] != 0)
                {
                    Center(_offset);
                }
            }
        }

        //Centers motion data to offset value.
        public void Center(Vector3 offset)
        {
            if (active[_sensorIndx])
            {
                _center = positions[_sensorIndx];
                _offset = offset;
            }
        }
        #endregion
    }
}

