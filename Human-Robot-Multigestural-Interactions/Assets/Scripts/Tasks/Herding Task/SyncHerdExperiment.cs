namespace MQ.MultiAgent
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Mirror;
    using Tobii.Research.Unity;

    [RequireComponent(typeof(NetworkIdentity))]
    public class SyncHerdExperiment : NetworkBehaviour
    {
        public static SyncHerdExperiment Instance { get; private set; }
        private SyncExperimentPhase SyncPhase;

        #region Networked Task Variables
        //Participant Number
        [SyncVar] public int subjectNum;

        //Sync State Flag
        [SyncVar] public bool networkSync;          //Flag indicating whether both players are ready.
        [SyncVar] public bool jointAttention;       //Are players looking at the same object?
        [SyncVar] public bool jointGaze;            //Are players looking at each other?

        //Responder Touch Information for Joint Reach Task
        [SyncVar] public bool jointReadyTouch;
        [SyncVar] public string touchObjName;

        #region Herd-Task Specific Sync Variables
        #endregion

        private float _timeSinceWrite;
        private float _sampleRate = 1 / 90f;

        private bool _saveData = false;
        private bool SaveData
        {
            get { return _saveData; }
            set
            {
                if (!_saveData)
                {
                    if (value != _saveData)
                    {
                        _timeSinceWrite = 0;
                    }
                }
                else if (_saveData)
                {
                    if (value != _saveData)
                    {
                        Data.FlushBuffer();
                    }
                }
                _saveData = value;
            }
        }
        #endregion

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            syncInterval = 0f;
            SyncPhase = SyncExperimentPhase.Instance;

            networkSync = false;
            jointAttention = false;
            jointGaze = false;

            if (isServer) subjectNum = TitleScreenData.SubjectNum;
        }

        // Update is called once per frame
        void Update()
        {
            if (SyncPhase.ExperimentRunning && !Data.Ready)
            {
                var myPlayer = GameObject.FindGameObjectWithTag("Player");
                Data.Initialize(myPlayer.GetComponent<Player>(),
                                myPlayer.GetComponent<SyncActors>().partnerPlayer,
                                myPlayer.GetComponent<TaskController>());
            }

            if (!SyncPhase.PlayersReady) return;
            if (!SyncPhase.ExperimentRunning) return;

            if (isServer)
            {
                networkSync = CheckReadiness(SyncPhase.Players);
                jointAttention = CheckAttention(SyncPhase.Players);
                jointGaze = CheckGaze(SyncPhase.Players);
                jointReadyTouch = CheckReadyTouch(SyncPhase.Players);
            }

            //Check Saving
            if (SyncPhase.LocalPhase == 2 && !SaveData) SaveData = true;
            if (SyncPhase.LocalPhase == 3 && SaveData) SaveData = false;

            if (SaveData)
            {
                _timeSinceWrite += Time.deltaTime;

                if (_timeSinceWrite >= _sampleRate)
                {
                    Data.Buffer.Add(Data.LatestDataString);
                    _timeSinceWrite = _timeSinceWrite - _sampleRate;
                }
            }
        }

        #region Helper Functions
        private bool CheckReadiness(SyncActors[] players)
        {
            foreach (SyncActors player in players)
            {
                if (player.readyState == false) return false;       //If any player is not ready, return false.
            }
            return true;
        }
        private bool CheckAttention(SyncActors[] players)
        {
            string comparator = players[0].gazeObject;

            for (int i = 1; i < players.Length; i++)
            {
                if (comparator != players[i].gazeObject) return false;  //If mismatched, then no shared attention.
            }

            if (comparator == "") return false;                         //If no object is detected, no shared attention.
            else return true;
        }
        private bool CheckGaze(SyncActors[] players)
        {

            foreach (SyncActors player in players)
            {
                switch (player.transform.name)
                {
                    case ("Player1"):
                        if (player.gazeObject != "Player2Face") return false;
                        break;
                    case ("Player2"):
                        if (player.gazeObject != "Player1Face") return false;
                        break;
                    default:
                        Debug.LogError("Error with SyncExperiment/CheckGaze. PlayerID not what expected");
                        return false;
                }
            }
            return true;
        }
        private bool CheckReadyTouch(SyncActors[] players)
        {
            foreach (SyncActors player in players)
            {
                if (player.touchObject != "FingerCubeReady") return false;
            }
            return true;
        }
        #endregion
    }
}
