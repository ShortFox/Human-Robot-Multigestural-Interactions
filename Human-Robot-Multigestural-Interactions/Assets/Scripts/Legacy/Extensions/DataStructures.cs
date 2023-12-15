namespace MQ.MultiAgent
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    using System.IO;

    public enum TaskType
    {
        None = 0,
        Herd = 1,
        Box = 2,
    }

    #region Classes

    /// <summary>
    /// Static Class organizing Title Screen Data.
    /// </summary>
    public static class TitleScreenData
    {
        public static int SubjectNum;
        public static string PersonName;
        public static TaskType Task;
        public static string Condition;
        public static string Gender;
        public static int GenderIndx;
        public static string PartnerGender;
        public static int PartnerGenderIndx;
        public static bool IsAI;
    }

    /// <summary>
    /// Manage data writing.
    /// </summary>
    public static class Data
    {
        public static bool Ready;
        public static List<string> Buffer;
        public static Player Player;
        public static TaskController Task;
        public static OtherPlayer Partner;

        // TODO: Add method to include more data to the output file (for SYNC info).
        // public static SyncExperimentPhase SYNC;

        private static string _dataFolderPath;         //Location of data folder.
        private static string _pairNum;

        #region Properties
        private static string _header;
        public static string Header
        {
            get
            {
                //Need to define Task and "Partner" Headers.
                if (_header == null)
                {
                    _header += Task.Header + ",";
                    _header += Player.HeadHeader + ",";
                    _header += Player.Eyes.Header + ",";
                    _header += Player.Hand.Header + ",";
                    _header += Partner.Header + ",";
                    _header += "ServerTime,ServerTimeSD,ServerRRT";
                }
                return _header;
            }
        }

        public static string LatestDataString
        {
            get
            {
                string output = "";

                output += Task.LatestDataString() + ",";
                output += Player.LatestHeadDataString() + ",";
                output += Player.Eyes.LatestDataString() + ",";
                output += Player.Hand.LatestDataString() + ",";
                output += Partner.LatestDataString() + ",";
                return output;
            }
        }
        #endregion

        public static void Initialize(Player player, OtherPlayer partner, TaskController task)
        {
            if (Ready) Debug.LogError("Data class error: Class already initialized");

            Player = player;
            Task = task;
            Partner = partner;
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
            _dataFolderPath = Application.dataPath + "/OutData/" + TitleScreenData.Task + "_wAI" + "/" + _pairNum;

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

                string filename = _pairNum + "_" + TitleScreenData.PersonName + "_" + Task.TaskName + "_" + Task.TrialNum.ToString() + "_" + "TrialID" + Task.TrialID + "_Date_" + time.ToString("dd'_'MM'_'yyyy'_'H''mm''ss") + ".csv";

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
    /// Class containing functionality to convert player's partner data to string output.
    /// </summary>
    public class OtherPlayer
    {
        private GameObject Player;
        public Transform Head;
        public Hand Hand;
        public Eyes Eyes;

        public OtherPlayer(GameObject otherPlayer)
        {
            //if (otherPlayer.tag != "OtherPlayer") Debug.LogError("Error Creating OtherPlace Class. Expecting GameObject with tag 'OtherPlayer'");
            Player = otherPlayer;
            Head = Player.transform.Find("Camera");
            Hand = Player.transform.Find("Finger").GetComponent<Hand>();
            Eyes = Player.transform.Find("Avatar").GetComponent<Eyes>();
        }
        public string LatestDataString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}",
                    Head.position.x.ToString("F4"),
                    Head.position.y.ToString("F4"),
                    Head.position.z.ToString("F4"),
                    Head.eulerAngles.x.ToString("F4"),
                    Head.eulerAngles.y.ToString("F4"),
                    Head.eulerAngles.z.ToString("F4"),
                    Eyes.GazeLeftEye.x.ToString("F4"),
                    Eyes.GazeLeftEye.y.ToString("F4"),
                    Eyes.GazeLeftEye.z.ToString("F4"),
                    Eyes.LeftRot.x.ToString("F4"),
                    Eyes.LeftRot.y.ToString("F4"),
                    Eyes.LeftRot.z.ToString("F4"),
                    Eyes.GazeRightEye.x.ToString("F4"),
                    Eyes.GazeRightEye.y.ToString("F4"),
                    Eyes.GazeRightEye.z.ToString("F4"),
                    Eyes.RightRot.x.ToString("F4"),
                    Eyes.RightRot.y.ToString("F4"),
                    Eyes.RightRot.z.ToString("F4"),
                    Eyes.GazedTransformName,
                    Eyes.GazedLocat.x.ToString("F4"),
                    Eyes.GazedLocat.y.ToString("F4"),
                    Eyes.GazedLocat.z.ToString("F4"),
                    Hand.Position.x.ToString("F4"),
                    Hand.Position.y.ToString("F4"),
                    Hand.Position.z.ToString("F4"),
                    Hand.Rotation.eulerAngles.x.ToString("F4"),
                    Hand.Rotation.eulerAngles.y.ToString("F4"),
                    Hand.Rotation.eulerAngles.z.ToString("F4"),
                    Hand.ContactName,
                    Hand.PointName,
                    // TODO: networking stuff, might not be necessary.
                    "SyncValues.localTime");
        }
        private string _header;
        public string Header
        {
            get
            {
                if (_header == null)
                {
                    _header = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30}",
                        "PartnerHeadPosX",
                        "PartnerHeadPosY",
                        "PartnerHeadPosZ",
                        "PartnerHeadEulerX",
                        "PartnerHeadEulerY",
                        "PartnerHeadEulerZ",
                        "PartnerLeftEyeGazeVectorX",
                        "PartnerLeftEyeGazeVectorY",
                        "PartnerLeftEyeGazeVectorZ",
                        "PartnerLeftEyeEulerX",
                        "PartnerLeftEyeEulerY",
                        "PartnerLeftEyeEulerZ",
                        "PartnerRightEyeGazeVectorX",
                        "PartnerRightEyeGazeVectorY",
                        "PartnerRightEyeGazeVectorZ",
                        "PartnerRightEyeEulerX",
                        "PartnerRightEyeEulerY",
                        "PartnerRightEyeEulerZ",
                        "PartnerGazedObject",
                        "PartnerGazedPointX",
                        "PartnerGazedPointY",
                        "PartnerGazedPointZ",
                        "PartnerHandPosX",
                        "PartnerHandPosY",
                        "PartnerHandPosZ",
                        "PartnerHandEulerX",
                        "PartnerHandEulerY",
                        "PartnerHandEulerZ",
                        "PartnerHandContactName",
                        "PartnerPointName",
                        "PartnerLocalTime");
                }
                return _header;
            }
        }
    }
    #endregion
}