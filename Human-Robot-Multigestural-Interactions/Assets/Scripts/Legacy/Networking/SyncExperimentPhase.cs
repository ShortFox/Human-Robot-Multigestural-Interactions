namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;

    public class SyncExperimentPhase : NetworkBehaviour
    {
        public static SyncExperimentPhase Instance { get; private set; }

        public double ServerTime { get { return NetworkTime.time; } }
        public double ServerTimeSD { get { return NetworkTime.timeStandardDeviation; } }
        public double ServerRRT { get { return NetworkTime.rtt; } }

        //Experiment State Variables
        [SyncVar] public bool ExperimentRunning;
        [SyncVar(hook = nameof(OnChangeExperimentPhase))]
        public int ExperimentPhase;           // 0 = done, 1 = initiate, 2= run, 3= 0

        //[SyncVar] public int ExperimentPhase;           // 0 = done, 1 = initiate, 2= run, 3= 0

        private void OnChangeExperimentPhase(int old_phase, int new_phase)
        {
            //Debug.Log("Experiment Phase Change from: " + old_phase + " to: " + new_phase);
            LocalPhase = new_phase;
        }

        [SyncVar] public bool TaskSyncronized;          // Flag Indicating if Players Task States should be synchronized.
        [SyncVar] public bool AI_Controlled;            //Flag whether partner player is AI-controlled.

        #region Experiment-Related
        public Task TaskEnvironment;                    //Task Engaged With.
        public SyncActors[] Players;
        [SyncVar] public int NumNetworkedPlayers;

        [SyncVar] public bool PlayersReady;
        #endregion

        [ShowInInspector]
        private int _localPhase;

        public int LocalPhase
        {
            get { return _localPhase; }
            set
            {
                if (_localPhase == value) return;
                _localPhase = value;

                switch (value)
                {
                    case 0:
                        TaskEnvironment.SetUp();
                        break;
                    case 1:
                        TaskEnvironment.Initiate();
                        break;
                    case 2:
                        TaskEnvironment.Run();
                        break;
                    case 3:
                        TaskEnvironment.Done();
                        break;
                    case 4:
                        Debug.Log("Experiment is Finished");
                        TaskEnvironment.Finished();
                        break;
                    case -1:
                        // This is a dummy value to help with syncing
                        break;
                    default:
                        Debug.LogError("Error. ExperimentPhase Not What Expected. Value received: "+value);
                        break;
                }
            }
        }

        private void Awake()
        {
            Instance = this;

            switch (TitleScreenData.Task)
            {
                case TaskType.Box:
                    gameObject.AddComponent<Box.SyncBoxExperiment>();
                    break;
                case TaskType.Herd:
                    gameObject.AddComponent<SyncHerdExperiment>();
                    break;
                default:
                    Debug.LogError("Error: Task not from list of available in program.");
                    break;
            }

            TaskEnvironment = gameObject.AddComponent<Task>();
            TaskEnvironment.Initialize(TitleScreenData.Task);
            //LocalPhase = -1;                                    //Set initial state to -1 so that when set to 0, it triggers event
            ExperimentPhase = -1;
        }
        private void Start()
        {
            ExperimentPhase = 0;
        }
        private void Update()
        {
            if (LocalPhase != ExperimentPhase)
            {
                LocalPhase = ExperimentPhase;
            }

            if (!isServer) return;

            if (NetworkManager.singleton.numPlayers == 1)
            {
                if (!PlayersReady && Input.GetKeyDown(KeyCode.Space))
                {
                    //Add Second Player for Control (Need to test to see if still valid)
                    List<GameObject> prefabObjs = GameObject.Find("[Network Management]").GetComponent<NetworkManagement>().spawnPrefabs;
                    GameObject player = Instantiate(prefabObjs[TitleScreenData.PartnerGenderIndx], Vector3.zero, Quaternion.identity);
                    player.GetComponent<PlayerProperties>().playerID = 2;
                    player.GetComponent<NetworkIdentity>().serverOnly = true;
                    player.GetComponent<SyncActors>().readyState = true;
                    player.GetComponent<SyncActors>().AI_Controllable = true;
                    NetworkServer.Spawn(player);

                    PlayersReady = true;
                    return;

                }
            }
            else if (NetworkManager.singleton.numPlayers == 2) PlayersReady = true;
            else return;

            if (!PlayersReady) return;  //Only proceed if players are ready

            if (Players == null) Players = FindObjectsOfType<SyncActors>();
            if (Players.Length != NetworkManager.singleton.numPlayers)
            {
                Players = FindObjectsOfType<SyncActors>();
                NumNetworkedPlayers = NetworkManager.singleton.numPlayers;
            }

            //If Set up, Proceeds
            if (Input.GetKeyDown(KeyCode.Space) && ExperimentRunning == false)
            {
                Debug.Log("Starting Experiment");
                ExperimentRunning = true;
                TaskSyncronized = true;
                ExperimentPhase = 1;
            }
        }

        //Client message to Server.
        //[ClientCallback]
        public void SetPhase(int phase)
        {
            if (TaskSyncronized)
            {
                if (NetworkManager.singleton.numPlayers == 1) ExperimentPhase = phase; // If 1-person task.
                else if (isServer) ExperimentPhase = phase;
                else CmdExperimentPhase(phase); // If tasks are synchronized, then SetPhase by client will only work if it is to abort
            }
            else
            {
                ExperimentPhase = phase;
            }
        }
        [Command]
        public void CmdExperimentPhase(int phase)
        {
            if (phase == 3) ExperimentPhase = phase;    //Only set if it's to make trial fail.
        }
    }
}