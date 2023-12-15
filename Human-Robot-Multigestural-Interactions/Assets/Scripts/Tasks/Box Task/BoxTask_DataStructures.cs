namespace MQ.MultiAgent.Box
{
    using System;
    using UnityEngine;
    using Tobii.Research.Unity;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    #region Structures

    /// <summary>
    /// Header for Initialization file.
    /// </summary>
    public enum INITHeader
    {
        TaskName,
        PlaybackID,
        TrialID,
        GazeCongruency,
        TargetLocation,
        Participant,
        AI
    }

    /// <summary>
    /// Reference to Initiation file data.
    /// </summary>
    public struct INITInfo: IRecordable
    {
        public string Task;
        public string PlaybackFileName;
        public int TrialID;
        public int GazeCongruency;
        public int TargetLocation;
        public string Role_Participant;
        public string Role_AI;
        public string[] InitString;
        public INITInfo(string[] initInfo)
        {
            Task = initInfo[(int)INITHeader.TaskName];
            PlaybackFileName = initInfo[(int)INITHeader.PlaybackID];
            TrialID = Convert.ToInt16(initInfo[(int)INITHeader.TrialID]);
            TargetLocation = Convert.ToInt16(initInfo[(int)INITHeader.TargetLocation]);
            Role_Participant = initInfo[(int)INITHeader.Participant];
            Role_AI = initInfo[(int)INITHeader.AI];
            InitString = initInfo;
            var cong = initInfo[(int)INITHeader.GazeCongruency];
            GazeCongruency = string.IsNullOrEmpty(cong) ? -1 : Convert.ToInt16(cong);
        }

        public static string Header
        {
            get
            {
                return string.Format("{0},{1},{2},{3},{4},{5},{6}",
                                       "Task",
                                       "PlaybackFileName",
                                       "TrialID",
                                       "GazeCongruency",
                                       "TargetLocation",
                                       "Role_Participant",
                                       "Role_AI");
            }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6}",
            Task,
            PlaybackFileName,
            TrialID.ToString(),
            GazeCongruency.ToString(),
            TargetLocation.ToString(),
            Role_Participant,
            Role_AI);
        }

        /// <inheritdoc/>
        public int NumberOfColumnsNeeded
        {
            get
            {
                return  7;
            }
        }

        /// <inheritdoc/>
        public string[] GetHeader()
        {
            return Header.Split(',');
        }

        /// <inheritdoc/>
        public string[] Record() {
            return this.ToString().Split(',');
        }

        /// <inheritdoc/>
        public string OnRecordingExceptionCaught(Exception e)
        {
            Debug.LogAssertion($"Exception occurred when attempting to record INIT Info: {e.Message}");
            return e is ArgumentNullException ? "null" : "exception occured";
        }

        /// <inheritdoc/>
        public void OnGetHeaderExceptionCaught(Exception e, int failedHeaderNo)
        {
            Debug.LogAssertion($"While reading INITInfo's header definition an exception occured: {e.Message}" +
                $" Fail id in header name will be {failedHeaderNo}.");
        }
    }

    public enum TrialInfoHeader
    {
        TaskName,
        TrialID,
        GazeCongruency,
        TargetLocation,
        RoleA,
        B1_val_A,
        B2_val_A,
        B3_val_A,
        RoleB,
        B3_val_B,
        B2_val_B,
        B1_val_B
    }

    /// <summary>
    /// Trial information and playback data
    /// </summary>
    public struct TrialInfo : IRecordable
    {
        public INITInfo InitInfo;
        public string Task;
        public int TrialID;
        public string PlaybackID;
        public int GazeCongruency;
        public int TargetLocation;
        public string RoleA;
        public int B1_Val_A;
        public int B2_Val_A;
        public int B3_Val_A;
        public string RoleB;
        public int B3_Val_B;
        public int B2_Val_B;
        public int B1_Val_B;
        public string[] TrialString;

        public List<PlaybackInfo> PlaybackData { get; private set; }        //Information on playback Data.

        public TrialInfo(string[] initInfo)
        {
            InitInfo = new INITInfo(initInfo);
            Task = initInfo[(int)INITHeader.TaskName];
            TrialID = Convert.ToInt16(initInfo[(int)INITHeader.TrialID]);
            TargetLocation = Convert.ToInt16(initInfo[(int)INITHeader.TargetLocation]);

            try { PlaybackID = initInfo[(int)INITHeader.PlaybackID]; } catch { PlaybackID = null; }
            try { GazeCongruency = Convert.ToInt16(initInfo[(int)INITHeader.GazeCongruency]); } catch { GazeCongruency = -1; }
            try { RoleA = initInfo[(int)INITHeader.Participant]; } catch { RoleA = null; }
            try { RoleB = initInfo[(int)INITHeader.AI]; } catch { RoleB = null; }

            //Initialize to any value so methods can be run
            B1_Val_A = 0;
            B2_Val_A = 0;
            B3_Val_A = 0;
            B3_Val_B = 0;
            B2_Val_B = 0;
            B1_Val_B = 0;

            TrialString = null;
            PlaybackData = null;

            if (!string.IsNullOrEmpty(PlaybackID))
            {
                PlaybackData = ReadPlaybackFile(PlaybackID);
                TargetLocation = PlaybackData[0].TargetLocation; //Pull target location from playback file.

                //Set remaining values correctly.
                B1_Val_A = PlaybackData[0].B1_Val_A;
                B2_Val_A = PlaybackData[0].B2_Val_A;
                B3_Val_A = PlaybackData[0].B3_Val_A;
                B3_Val_B = PlaybackData[0].B3_Val_B;
                B2_Val_B = PlaybackData[0].B2_Val_B;
                B1_Val_B = PlaybackData[0].B1_Val_B;
            }
        }

        public static List<PlaybackInfo> ReadPlaybackFile(string file)
        {
            DirectoryInfo directory = new DirectoryInfo(Application.streamingAssetsPath + "/Trajectories");
            FileInfo[] trajectFile;                //File Information
            trajectFile = directory.GetFiles(file);
            file = trajectFile[0].FullName;         //Get full path name of trajectory

            List<PlaybackInfo> output = new List<PlaybackInfo>();

            using (StreamReader sr = new StreamReader(file))
            {
                sr.ReadLine();          //Remove header.
                while (!sr.EndOfStream)
                {
                    output.Add(new PlaybackInfo(sr.ReadLine().Split(',')));
                }
            }
            return output;
        }

        public static string[] Header = new string[]
            {
                "Task",
                "TrialID",
                "GazeCongruency",
                "TargetLocation",
                "RoleA",
                "B1_Val_A",
                "B2_Val_A",
                "B3_Val_A",
                "RoleB",
                "B3_Val_B",
                "B2_Val_B",
                "B1_Val_B"
            };

        public int NumberOfColumnsNeeded { get { return 12; }}

        public string[] GetHeader()
        {
            return Header;
        }

        public string[] Record()
        {
            return new string[]
            {
                Task,
                TrialID.ToString(),
                GazeCongruency.ToString(),
                TargetLocation.ToString(),
                RoleA,
                B1_Val_A.ToString(),
                B2_Val_A.ToString(),
                B3_Val_A.ToString(),
                RoleB,
                B3_Val_B.ToString(),
                B2_Val_B.ToString(),
                B1_Val_B.ToString()
            };
        }

        /// <inheritdoc/>
        public string OnRecordingExceptionCaught(Exception e)
        {
            Debug.LogAssertion($"Exception occurred when attempting to record Task Info: {e.Message}");
            return e is ArgumentNullException ? "null" : "exception occured";
        }

        /// <inheritdoc/>
        public void OnGetHeaderExceptionCaught(Exception e, int failedHeaderNo)
        {
            Debug.LogAssertion($"While reading TrialInfo's header definition an exception occured: {e.Message}" +
                $" Fail id in header name will be {failedHeaderNo}.");
        }
    }

    /// <summary>
    /// Header for Playback file
    /// </summary>
    public enum PlaybackHeader
    {
        Task = 0,
        TrialID = 1,
        TargetLocation = 2,
        RoleA = 3,
        B1_Val_A = 4,
        B2_Val_A = 5,
        B3_Val_A = 6,
        RoleB = 7,
        //B3_Val_B = 8,
        //B2_Val_B = 9,
        //B1_Val_B = 10,
        HeadPosX = 14,
        HeadPosY = 15,
        HeadPosZ = 16,
        HeadEulerX = 17,
        HeadEulerY = 18,
        HeadEulerZ = 19,
        GazeObject = 46,
        GazePosX = 47,
        GazePosY = 48,
        GazePosZ = 49,
        HandPosX = 52,
        HandPosY = 53,
        HandPosZ = 54,
        HandEulerX = 55,
        HandEulerY = 56,
        HandEulerZ = 57,
        MovePhase = 106,
        UnityTime = 98,
        GazeValid = 45
    }

    /// <summary>
    /// Playback file information
    /// </summary>
    public struct PlaybackInfo
    {
        public string Task;
        public int TrialID;
        public int TargetLocation;
        public string RoleA;
        public int B1_Val_A;
        public int B2_Val_A;
        public int B3_Val_A;
        public string RoleB;
        public int B3_Val_B;
        public int B2_Val_B;
        public int B1_Val_B;
        public float HeadPosX;
        public float HeadPosY;
        public float HeadPosZ;
        public Vector3 HeadPos;
        public float HeadRotX;
        public float HeadRotY;
        public float HeadRotZ;
        public Vector3 HeadRot;
        public Vector3 HeadTarget;
        public float HandPosX;
        public float HandPosY;
        public float HandPosZ;
        public Vector3 HandPos;
        public float HandRotX;
        public float HandRotY;
        public float HandRotZ;
        public Vector3 HandRot;
        public string GazeObject;
        public float GazePosX;
        public float GazePosY;
        public float GazePosZ;
        public Vector3 GazePos;
        public bool Moving;
        public float UnityTime;
        public bool GazeValid;

        public PlaybackInfo(string[] trajectInfo)
        {
            Task = trajectInfo[(int)PlaybackHeader.Task];
            TrialID = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.TrialID]);
            TargetLocation = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.TargetLocation]);
            RoleA = trajectInfo[(int)PlaybackHeader.RoleA];
            B1_Val_A = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B1_Val_A]);
            B2_Val_A = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B2_Val_A]);
            B3_Val_A = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B3_Val_A]);
            RoleB = trajectInfo[(int)PlaybackHeader.RoleB];

            B3_Val_B = 0;
            B2_Val_B = 0;
            B1_Val_B = 0;

            /*
            B3_Val_B = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B3_Val_B]);
            B2_Val_B = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B2_Val_B]);
            B1_Val_B = Convert.ToInt16(trajectInfo[(int)PlaybackHeader.B1_Val_B]);
            */

            HeadPosX = float.Parse(trajectInfo[(int)PlaybackHeader.HeadPosX]);
            HeadPosY = float.Parse(trajectInfo[(int)PlaybackHeader.HeadPosY]);
            HeadPosZ = float.Parse(trajectInfo[(int)PlaybackHeader.HeadPosZ]);
            HeadPos = new Vector3(HeadPosX, HeadPosY, HeadPosZ);
            HeadRotX = float.Parse(trajectInfo[(int)PlaybackHeader.HeadEulerX]);
            HeadRotY = float.Parse(trajectInfo[(int)PlaybackHeader.HeadEulerY]);
            HeadRotZ = float.Parse(trajectInfo[(int)PlaybackHeader.HeadEulerZ]);
            HeadRot = new Vector3(HeadRotX, HeadRotY, HeadRotZ);

            HeadTarget = (Quaternion.Euler(HeadRot) * Vector3.forward) + HeadPos;       //Should create position in front of head.

            HandPosX = float.Parse(trajectInfo[(int)PlaybackHeader.HandPosX]);
            HandPosY = float.Parse(trajectInfo[(int)PlaybackHeader.HandPosY]);
            HandPosZ = float.Parse(trajectInfo[(int)PlaybackHeader.HandPosZ]);
            HandPos = new Vector3(HandPosX, HandPosY, HandPosZ);
            HandRotX = float.Parse(trajectInfo[(int)PlaybackHeader.HandEulerX]);
            HandRotY = float.Parse(trajectInfo[(int)PlaybackHeader.HandEulerY]);
            HandRotZ = float.Parse(trajectInfo[(int)PlaybackHeader.HandEulerZ]);
            HandRot = new Vector3(HandRotX, HandRotY, HandRotZ);

            try { GazeObject = trajectInfo[(int)PlaybackHeader.GazeObject]; } catch { GazeObject = ""; }
            try { GazePosX = float.Parse(trajectInfo[(int)PlaybackHeader.GazePosX]); } catch { GazePosX = float.NaN; }
            try { GazePosY = float.Parse(trajectInfo[(int)PlaybackHeader.GazePosY]); } catch { GazePosY = float.NaN; }
            try { GazePosZ = float.Parse(trajectInfo[(int)PlaybackHeader.GazePosZ]); } catch { GazePosZ = float.NaN; }
            try { GazePos = new Vector3(GazePosX, GazePosY, GazePosZ); } catch { GazePos = new Vector3(float.NaN, float.NaN, float.NaN); }

            Moving = Convert.ToBoolean(Convert.ToInt16(trajectInfo[(int)PlaybackHeader.MovePhase]));
            UnityTime = float.Parse(trajectInfo[(int)PlaybackHeader.UnityTime]);
            GazeValid = Convert.ToBoolean(Convert.ToInt16(trajectInfo[(int)PlaybackHeader.GazeValid]));
        }
    }
    #endregion

    #region Classes
    public sealed class ExpInitializer
    {
        private static readonly ExpInitializer _instance = new ExpInitializer();
        public static ExpInitializer Instance { get { return _instance; } }
        private string _filename; // = "SimAgent_INI_HeadConstrained_5050.csv";//"ExpInitialization_Debug.csv";
        private string _fileNameBackUp = "ExpInitialization_BackUp.csv";

        private int _fileCount;

        private ExpInitializer()
        {
            try
            {
                _initDirectory = new DirectoryInfo(Application.streamingAssetsPath +"/INIT");

                _initData = Settings.Trials;
                if (_initData == null || _initData.Count > 0)
                {
                    switch(Settings.Condition)
                    {
                        case "5050_HeadConstrained":
                            _filename = "SimAgent_INI_HeadConstrained_5050.csv";
                            break;
                        case "5050_HeadUnConstrained":
                            _filename = "SimAgent_INI_HeadUnconstrained_5050.csv";
                            break;
                        case "7525_HeadConstrained":
                            _filename = "SimAgent_INI_HeadConstrained_7525.csv";
                            break;
                        case "7525_HeadUnConstrained":
                            _filename = "SimAgent_INI_HeadUnconstrained_7525.csv";
                            break;
                        case "2575_HeadConstrained":
                            _filename = "SimAgent_INI_HeadConstrained_2575.csv";
                            break;
                        case "2575_HeadUnConstrained":
                            _filename = "SimAgent_INI_HeadUnconstrained_2575.csv";
                            break;
                        default:
                            _filename = "SimAgent_INI_HeadUnconstrained_ALL.csv";
                            break;
                    }

                    _initData = new List<TrialInfo>();
                    LoadInitData(Enum.GetNames(typeof(INITHeader)));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred when loading INIT file: {e}");
            }
        }

        public List<TrialInfo> GetTrialList(int n_trials, bool shuffled, string task_type, string role = null)
        {
            if (n_trials < 0)
            {
                n_trials = _fileCount;
            }

            List<TrialInfo> trials = new List<TrialInfo>();

            int i = 0;
            if (shuffled)
            {
                _initData.Shuffle();
            }

            while (trials.Count < n_trials)
            {
                int idx = i++ % _fileCount;
                try
                {
                    if (trials.Count == 0 && i >= _fileCount)
                    {
                        SubmitError("Error selecting task");
                        break;
                    }
                    TrialInfo trial = _initData[idx];

                    if (trial.Task == task_type)
                    {
                        if (role == null || role == trial.RoleA)
                        {
                            trials.Add(trial);
                        }
                    }
                }
                catch
                {
                    Debug.LogError("Error: There was an error obtaining TrialInfo");
                    break;
                }
            }

            return trials;
        }

        public List<TrialInfo> GetTrialList(bool shuffled = true)
        {
            List<TrialInfo> saccadeTrials = new List<TrialInfo>();
            List<TrialInfo> reachTrials = new List<TrialInfo>();
            List<TrialInfo> jointTrials = new List<TrialInfo>();

            List<TrialInfo> trials = new List<TrialInfo>();

            for (int i = 0; i < _fileCount; i++)
            {
                try
                {
                    TrialInfo trial = _initData[i];
                    switch (trial.Task)
                    {
                        case "solo_saccade":
                            saccadeTrials.Add(trial);
                            break;
                        case "solo_point":
                            reachTrials.Add(trial);
                            break;
                        case "joint":
                            jointTrials.Add(trial);
                            break;
                        default:
                            SubmitError("Error selecting task");     //Make INIT file unavailable.
                            break;
                    }
                }
                catch
                {
                    Debug.LogError("Error: There was an error obtaining TrialInfo");
                    break;
                }
            }

            if (shuffled)
            {
                saccadeTrials.Shuffle();
                reachTrials.Shuffle();
                jointTrials.Shuffle();
            }

            trials.AddRange(saccadeTrials);
            trials.AddRange(reachTrials);
            trials.AddRange(jointTrials);

            return trials;
        }

        #region Properties and Fields
         //Flag to denote if Initializer went through file.
        private bool _finished = false;
        public bool IsFinished { get { return _finished; } }

        private DirectoryInfo _initDirectory;       //Directory
        private string _trialInfoHeader;               //Header for INIT file.

        //Initialization Data with index.
        private List<TrialInfo> _initData;

        #endregion

        #region ExpInitializer Functions
        void LoadInitData(string[] header_reference)
        {
            string[] initFile = Directory.GetFiles(
                Application.streamingAssetsPath + "/INIT",
                _filename,
                SearchOption.AllDirectories)
                .Where(name => !name.EndsWith(".meta"))
                .ToArray<string>();

            using (StreamReader sr = new StreamReader(initFile[0]))
            {
                _trialInfoHeader = sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    try
                    {
                    _initData.Add(new TrialInfo(sr.ReadLine().Split(',')));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }

            _fileCount = _initData.Count;
        }

        public void SubmitError(string error_message)
        {
            Debug.LogError(error_message);
        }
        #endregion
    }

    public static class Data
    {
        public static bool Ready;
        public static List<string> Buffer;
        public static Player Player;
        public static Task Task;
        public static TaskDynamics TaskDyn;
        public static OtherPlayer Partner;
        public static SyncBoxExperiment SYNC;

        private static string _dataFolderPath;         //Location of data folder.
        private static string _pairNum;

        #region Properties
        private static string _header;
        public static string Header
        {
            get
            {
                if (_header == null)
                {
                    // _header += Task.Header + ",";
                    _header += Player.HeadHeader + ",";
                    _header += Player.Eyes.Header + ",";
                    _header += Player.Hand.Header + ",";
                    _header += Partner.Header + ",";
                    _header += "JointAttention,JointGaze,";
                    _header += "NetworkTime,NetworkRTT,";
                    _header += TaskDyn.Header;
                }
                return _header;
            }
        }

        //Check Data Write structure
        public static string LatestDataString
        {
            get
            {
                string output = "";

                // output += Task.LatestDataString() + ",";
                output += Player.LatestHeadDataString() + ",";
                output += Player.Eyes.LatestDataString() + ",";
                output += Player.Hand.LatestDataString() + ",";
                output += Partner.LatestDataString() + ",";
                output += (SYNC.jointAttention ? 1 : 0).ToString() + "," + (SYNC.jointGaze ? 1 : 0).ToString() + ",";
                output += SyncExperimentPhase.Instance.ServerTime + "," + SyncExperimentPhase.Instance.ServerRRT + ",";
                output += TaskDyn.LatestDataString();

                return output;
            }
        }
        #endregion

        public static void Initialize(Player player, OtherPlayer partner)
        {
            if (Ready) Debug.LogError("Data class error: Class already initialized");

            Player = player;
            Partner = partner;
            Task = SyncExperimentPhase.Instance.TaskEnvironment;
            TaskDyn = new TaskDynamics();
            SYNC = SyncBoxExperiment.Instance;
            Buffer = new List<string>();
            CreateDirectory();
        }

        //Methods
        private static void CreateDirectory()
        {
            //Get data folder.
            _pairNum = TitleScreenData.SubjectNum.ToString();
            if (TitleScreenData.SubjectNum < 10) _pairNum = "0" + _pairNum;
            _pairNum = "P" + _pairNum;
            _dataFolderPath = Application.dataPath + "/OutData/" + TitleScreenData.Task + "/" + TitleScreenData.Condition+"/"+_pairNum;

            //Create directory if none exists.
            if (!Directory.Exists(_dataFolderPath))
            {
                Directory.CreateDirectory(_dataFolderPath);
            }
            Ready = true;
        }

        public static void FlushBuffer()
        {
            if (!Ready) Debug.LogError("Data class error: Class not yet initialized");

            if (Buffer.Count == 0) return;
            else
            {
                DateTime time = DateTime.Now;

                string filename = _pairNum + "_" + TitleScreenData.PersonName + "_"+ Task.TrialNum.ToString() + "_" + TitleScreenData.Condition + "_" + "TrialID" + Task.TrialID + "_Date_" + time.ToString("dd'_'MM'_'yyyy'_'H''mm''ss") + ".csv";

                using (StreamWriter sw = File.CreateText(Path.Combine(_dataFolderPath, filename)))
                {
                    sw.WriteLine(Header);

                    for (int i = 0; i < Buffer.Count; i++)
                    {
                        sw.WriteLine(Buffer[i]);
                    }
                }
                Buffer.Clear();
            }
        }

    }

    /// <summary>
    /// Class containnig task-relevant dynamics.
    /// </summary>
    public class TaskDynamics
    {
        BoxTask Experiment;
        Eyes Eyes;
        Hand Finger;
        OtherPlayer Partner;

        public TaskDynamics()
        {
            Experiment = BoxTask.Instance;
            Eyes = Data.Player.Eyes;
            Finger = Data.Player.Hand;
            Partner = Data.Partner;
        }

        IVRGazeData EyeData { get { return Eyes.LatestEyesData; } }
        Vector3 TargetPos { get { return Experiment.TargetObj.position; } }

        float HandToTargetDist
        {
            get
            {
                return Vector3.Distance(Finger.Position, TargetPos);
            }
        }

        private Vector3 _vector2hand;
        float HandAngleToTarget
        {
            get
            {
                _vector2hand = (TargetPos - Finger.Position).normalized;
                return Vector3.Angle(_vector2hand, Finger.transform.right);
            }
        }

        private Vector3 _vector2eyes;
        float GazeAngleToTarget
        {
            get
            {
                _vector2eyes = (TargetPos - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        private Vector3 _vector2partnerHand;
        float GazeAngleToPartnerHand
        {
            get
            {
                _vector2eyes = (Partner.Hand.Position - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        private Vector3 _vector2partnerHead;
        float GazeAngleToPartnerHead
        {
            get
            {
                _vector2eyes = (Partner.Head.position - EyeData.CombinedGazeRayWorld.origin).normalized;
                return Vector3.Angle(_vector2eyes, EyeData.CombinedGazeRayWorld.direction);
            }
        }

        public string LatestDataString()
        {
            string output = "";

            try { output += (HandToTargetDist.ToString("F4") + ","); } catch { output += ","; }
            try { output+= (HandAngleToTarget.ToString("F4") + ","); } catch { output += ","; }
            try { output += (GazeAngleToTarget.ToString("F4") + ","); } catch { output += ","; }
            try { output += (GazeAngleToPartnerHand.ToString("F4") + ","); } catch { output += ","; }
            try { output += (GazeAngleToPartnerHead.ToString("F4") + ","); } catch { output += ","; }
            return output;
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = "HandToTargetDist,HandAngleToTarget,GazeAngleToTarget,GazeAngleToPartnerHand,GazeAngleToPartnerHead";
                }
                return _header;
            }
        }
    }
    /// <summary>
    /// Class containing functionality to convert player's hand data to string output.
    /// </summary>
    #endregion
}

