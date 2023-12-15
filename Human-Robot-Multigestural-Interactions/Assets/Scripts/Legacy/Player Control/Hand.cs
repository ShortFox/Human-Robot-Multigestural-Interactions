
/// <summary>
/// This code works for now but I would like to make changes in future to optimize.
/// </summary>
namespace MQ.MultiAgent
{
    using UnityEngine;

    public class Hand : MonoBehaviour
    {
        #region Hand Position, Rotation, and Contact Object Data and Methods
        public Transform Transform { get { return this.transform; } }
        private Vector3 _position;
        public Vector3 Position { get { return _position; } set { _position = value; } }
        private Quaternion _rotation;
        public Quaternion Rotation { get { return _rotation; } set { _rotation = value; } }
        public Quaternion RotationOffset { get; private set; }
        private GameObject _contactObject;
        public GameObject ContactObject { get { return _contactObject; } }
        public string ContactName { get { return ContactObject == null ? "" : ContactObject.name; } }
        private string _pointObject;
        public string PointName { get { return _pointObject; } }

        //Velocity
        private Vector3 _oldPosition;
        private Vector3 _velocity;
        public Vector3 Velocity { get { return _velocity; } set { _velocity = value; } }

        #endregion

        #region Data Properties
        public string LatestDataString()
        {
            string output = "";

            output += Position.x.ToString("F4") + "," + Position.y.ToString("F4") + "," + Position.z.ToString("F4") + "," +
                Rotation.eulerAngles.x.ToString("F4") + "," + Rotation.eulerAngles.y.ToString("F4") + "," + Rotation.eulerAngles.z.ToString("F4") + ","+
                ContactName+","+PointName;
            return output;
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = "HandPosX,HandPosY,HandPosZ,HandEulerX,HandEulerY,HandEulerZ,HandContactObj,HandPointObj";
                }
                return _header;
            }
        }
        #endregion

        #region Unity Methods

        private void OnTriggerEnter(Collider other)
        {
            _contactObject = other.gameObject;
        }
        private void OnTriggerExit(Collider other)
        {
            _contactObject = null;
        }

        private void Awake()
        {
            Position = transform.position;
            _oldPosition = Position;
            Rotation = transform.rotation;
            RotationOffset = Rotation;
        }
        private void LateUpdate()
        {
            //Check if Hand position has moved away from visible contact object. This is done to ensure the trigger is reset if contactObject suddenly disapears
            if (_contactObject != null)
            {
                if (_contactObject.activeSelf == false) _contactObject = null;
            }
        }
        private void Update()
        {
            _oldPosition = _position;
            Position = transform.position;
            Rotation = transform.rotation;

            Velocity = (_position - _oldPosition) / Time.deltaTime;
        }
        #endregion

        #region Helper Methods
        string PointObject()
        {
            string output = "";

            RaycastHit hitinfo;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out hitinfo,20))
            {
                output = hitinfo.transform.name;
            }

            return output;
        }
        #endregion
    }
}

