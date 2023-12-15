namespace MQ.MultiAgent
{
    using UnityEngine;
    using Mirror;

    [RequireComponent(typeof(PlayerProperties))]
    public class NetworkPlayerProperties : NetworkBehaviour
    {

        [SyncVar]
        public int playerID;

        [SerializeField]
        GameObject[] playerComponents;

        protected PlayerProperties playerProperties;

        #region Unity Methods

        public override void OnStartServer()
        {
            base.OnStartServer();
            // If this is a player-ownable object, return.
            if (!this.GetComponent<NetworkIdentity>().serverOnly) return;

            playerProperties.InitializePlayer(playerID);
        }

        // Set up local player object
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            playerProperties.InitializePlayer(playerID);
        }

        protected void Start()
        {
            playerProperties = GetComponent<PlayerProperties>();
            if (playerProperties == null)
            {
                playerProperties = gameObject.AddComponent<PlayerProperties>();
            }

            if (!isLocalPlayer)
            {
                playerProperties.Start();
            }
        }
        protected void Update()
        {
            //Update Variables if not the local player
            if (!isLocalPlayer)
            {
                playerProperties.Update();
            }
        }

        protected void InitBody()
        {
            playerProperties.InitBody();

        }
        #endregion
        protected void SetName(int ID)
        {
            playerProperties.SetName(ID);
            if (this.GetComponent<NetworkIdentity>().hasAuthority)
            {
                TitleScreenData.PersonName = playerProperties.playerName;
            }
        }
    }
}
