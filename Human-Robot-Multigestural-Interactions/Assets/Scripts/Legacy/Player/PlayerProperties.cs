namespace MQ.MultiAgent
{
    using UnityEngine;

    public class PlayerProperties : MonoBehaviour
    {


        [SerializeField]
        GameObject[] playerComponents;

        public int playerID;
        public string playerName { get; protected set; }

        protected Transform myCamera;
        protected Avatar avatar;

        protected SyncActors syncActor;

        private Player MyPlayer;     //Should only be defined on localplayer.

        #region Unity Methods

        public void InitializePlayer(int playerID)
        {
            SetName(playerID);
            InitializePlayer();
        }

        public void InitializePlayer()
        {

            //For Server Only AI Agents.
            MyPlayer = gameObject.AddComponent<Player>();

            InitBody();

            gameObject.tag = "Player";

            PlayerController controller = gameObject.GetComponent<PlayerController>();

            if (controller == null)
            {
                Debug.LogError("Missing Controller component. Please add a controller for this player.");
                return;
            }

            MyPlayer.SetUp(controller,
                           Mathf.RoundToInt(Mathf.Sign(transform.position.z)) * -1);

            foreach (GameObject obj in playerComponents)
            {
                Instantiate(obj, Vector3.zero, Quaternion.identity);
            }
        }

        public virtual void Start()
        {
            this.gameObject.tag = "OtherPlayer";
            InitializePlayer();
        }
        public virtual void Update()
        {
            //Update Variables if not the local player
            if (playerName == null)
            {
                InitBody();
            }
        }
        public virtual void InitBody()
        {
            SetName(playerID);
            //Name Face, Body and Hand.
            avatar = GetComponent<Avatar>();
        }

        #endregion

        #region Helper Functions
        public virtual void SetName(int ID)
        {
            playerName = "Player" + ID.ToString();
            TitleScreenData.PersonName = playerName;
            this.transform.name = playerName;
        }

        protected void SetObjName(Transform obj, string objName, int ID)
        {
            string name = playerName + objName;
            obj.name = name;
        }
        #endregion
    }
}
