namespace MQ.MultiAgent
{
    using UnityEngine;
    using Mirror;
    using Tobii.Research;

    public struct CreateAvatarMessage: NetworkMessage
    {
        public Gender gender;

        public enum Gender
        {
            Female,
            Male,
            Robot,
        }
    }
    public class NetworkManagement : NetworkManager
    {
        public void StartupHost()
        {
            StartHost();
        }

        public void JoinGame()
        {
            StartClient();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            NetworkServer.RegisterHandler<CreateAvatarMessage>(OnCreateCharacter);
        }

        public override void OnStartClient()
        {
            GameObject.Find("Menu Camera").GetComponent<Camera>().enabled = false;
            base.OnStartClient();
        }

        public override void OnStopClient()
        {
            EyeTrackingOperations.Terminate();
            GameObject.Find("Menu Camera").GetComponent<Camera>().enabled = true;
        }

        #region Player Setup and Spawn
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            CreateAvatarMessage.Gender gender;
            string s = TitleScreenData.Gender;
            switch (TitleScreenData.Gender) {
                case "Female":
                    gender = CreateAvatarMessage.Gender.Female;
                    break;
                case "Robot":
                    gender = CreateAvatarMessage.Gender.Robot;
                    break;
                default:
                    gender = CreateAvatarMessage.Gender.Male;
                    break;
            }

            CreateAvatarMessage message = new CreateAvatarMessage { gender = gender };

            conn.Send(message);
        }

        //See https://mirror-networking.com/docs/Guides/GameObjects/SpawnPlayerCustom.html
        private void OnCreateCharacter(NetworkConnection conn, CreateAvatarMessage message)
        {
            TitleScreenData.GenderIndx = (int)message.gender;
            GameObject player = Instantiate(spawnPrefabs[(int)message.gender], Vector3.zero, Quaternion.identity);

            PlayerProperties properties = player.GetComponent<PlayerProperties>();

            if (numPlayers > 0) properties.playerID = 2;
            else properties.playerID = 1;

            NetworkServer.AddPlayerForConnection(conn, player);
        }
        #endregion
    }
}