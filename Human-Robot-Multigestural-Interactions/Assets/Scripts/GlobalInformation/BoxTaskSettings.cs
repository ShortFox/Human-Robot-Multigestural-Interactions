using System.Collections.Generic;

namespace MQ.MultiAgent.Box
{
    ///<summary>Adjustable settings for box task.</summary>
    public static class Settings
    {
        public static string ParticipantId;
        public static string Belief;
        public static string Condition;
        public static string ParticipantAvatar;
        public static string PartnerAvatar;
        public static bool RunPracticeRound;
        public static int PracticeTrialCount;
        public static int TrialIdx = 0;
        public static List<TrialInfo> Trials;
        public static string SavePath;
    }
}
