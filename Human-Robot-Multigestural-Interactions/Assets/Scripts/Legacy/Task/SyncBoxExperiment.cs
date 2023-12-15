using System.Collections;
using UnityEngine;
using Mirror;

namespace MQ.MultiAgent.Box
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class SyncBoxExperiment : NetworkBehaviour
    {
        public static SyncBoxExperiment Instance { get; private set; }
        private SyncExperimentPhase SyncPhase;

        #region Networked Task Variables
        //Participant Number
        [SyncVar] public int subjectNum;

        //Sync State Flag
        //[SyncVar] public bool networkSync;          //Flag indicating whether both players are ready for multiplayer.
        [SyncVar] public bool jointAttention;       //Are players looking at the same object?
        [SyncVar] public bool jointGaze;            //Are players looking at each other?

        //Responder Touch Information for Joint Reach Task
        [SyncVar] public bool jointReadyTouch;
        [SyncVar] public string touchObjName;

        #region Box-Task Specific Sync Variables
        //Trial Information. Will be updated to match Host's states throughout experiment
        // [SyncVar(hook = "OnSyncStateReceived")]
        public int syncFlag;
        // [SyncVar(hook = "OnTrialNumberReceived")]
        public int trialNumber;
        [SyncVar] public string task;
        [SyncVar] public int trialID;
        [SyncVar] public string playbackID;
        [SyncVar] public int gazeCongruency;
        [SyncVar] public int targetLocation;
        [SyncVar] public string roleA;
        [SyncVar] public int b1_val_A;
        [SyncVar] public int b2_val_A;
        [SyncVar] public int b3_val_A;
        [SyncVar] public string roleB;
        [SyncVar] public int b3_val_B;
        [SyncVar] public int b2_val_B;
        [SyncVar] public int b1_val_B;
        #endregion

        private float _timeSinceWrite;
        private float _sampleRate = 1 / 90f;

        private bool _saveData = false;
        public bool SaveData
        {
            get { return _saveData; }
            set
            {
                if (!_saveData)
                {
                    if (value != _saveData)
                    {
                        _timeSinceWrite = 0;
                        Data.Buffer.Add(Data.LatestDataString);
                    }
                }
                else if (_saveData)
                {
                    if (value != _saveData)
                    {
                        Data.Buffer.Add(Data.LatestDataString);
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

            //networkSync = false;
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
                Data.Initialize(myPlayer.GetComponent<Player>(), myPlayer.GetComponent<SyncActors>().partnerPlayer);
            }

            if (!SyncPhase.PlayersReady) return;
            if (!SyncPhase.ExperimentRunning) return;

            if (SyncPhase.TaskSyncronized)
            {
                if (isServer)
                {
                    //networkSync = CheckReadiness(SyncPhase.Players);
                    //SyncPhase.TaskSyncronized = CheckReadiness(SyncPhase.Players);

                    jointAttention = CheckAttention(SyncPhase.Players);
                    jointGaze = CheckGaze(SyncPhase.Players);
                    jointReadyTouch = CheckReadyTouch(SyncPhase.Players);
                    touchObjName = CheckRespTouch(SyncPhase.Players);
                }
            }


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

        public void SyncTouchTime(Transform obj, float time)
        {
            if (!isServer) return;

            StartCoroutine(TimedJointTouch(obj, time));
        }
        IEnumerator TimedJointTouch(Transform obj, float time)
        {
            float timeElapsed = 0;
            while (timeElapsed < time)
            {
                //if ((Players[0].gazeObject == obj.name) && (jointReadyTouch) && (jointAttention))       //Although participants are attending to different objects, their name is the same and so jointAttention should be true.
                if ((jointReadyTouch))
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }
            syncFlag++;
        }

        public void SyncJointGazeTime(float time)
        {
            if (!isServer) return;

            StartCoroutine(TimedJointGaze(time));
        }
        IEnumerator TimedJointGaze(float time)
        {
            float timeElapsed = 0;
            while (timeElapsed < time)
            {
                if ((jointGaze))
                {
                    timeElapsed += Time.deltaTime;
                }
                else { timeElapsed = 0; }
                yield return null;
            }
            syncFlag++;
        }

        public void SyncTrialInfo(int number, TrialInfo trial)
        {
            if (SyncPhase.TaskSyncronized)
            {
                if (!isServer) return;
            }


            task = trial.Task;
            trialID = trial.TrialID;
            playbackID = trial.PlaybackID;
            gazeCongruency = trial.GazeCongruency;
            targetLocation = trial.TargetLocation;
            roleA = trial.RoleA;
            b1_val_A = trial.B1_Val_A;
            b2_val_A = trial.B2_Val_A;
            b3_val_A = trial.B3_Val_A;
            roleB = trial.RoleB;
            b3_val_B = trial.B3_Val_B;
            b2_val_B = trial.B2_Val_B;
            b1_val_B = trial.B1_Val_B;
            trialNumber = number;
        }

        #region Helper Functions
        // void OnSyncStateReceived(int oldSync,int newSync)
        // {
        //     BoxTask.Instance.SyncFlag++;
        // }

        // void OnTrialNumberReceived(int oldNum,int newNum)
        // {
        //     trialNumber = newNum;
        //     BoxTask.Instance.JointProceed = true;
        // }

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

        private string CheckRespTouch(SyncActors[] players)
        {
            string touchObj = "";

            foreach (SyncActors player in players)
            {
                switch (player.transform.name)
                {
                    case ("Player1"):
                        if (roleA == "Responder") touchObj = player.touchObject;
                        break;
                    case ("Player2"):
                        if (roleB == "Responder") touchObj = player.touchObject;
                        break;
                    default:
                        Debug.LogError("Error with SyncExperiment/CheckRespTouch. PlayerID: "+player.transform.name+"not what expected");
                        return "";
                }
            }
            return touchObj;
        }
        #endregion
    }
}
