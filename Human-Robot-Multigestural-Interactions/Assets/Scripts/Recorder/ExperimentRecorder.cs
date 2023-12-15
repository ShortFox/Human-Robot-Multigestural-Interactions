using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MQ.MultiAgent.Box
{
    /// <summary>
    /// A one off storage of data. It's targeted to store general settings on experiments, rather than time series data.
    /// </summary>
    public class ExperimentRecorder : MonoBehaviour
    {
        [SerializeField] public string savePath;

        private string _expPath;

        private StreamWriter _writer;

        public string SavePath(string id)
        {
            return Path.Combine(Application.streamingAssetsPath, SavePathRel(id));
        }

        public string SavePathRel(string id)
        {
            return Path.Combine(savePath, id);
        }

        public int CheckIdFile(string id)
        {
            var path = SavePath(id);
            var data_dir = new DirectoryInfo(path);
            var meta_file = new FileInfo($"{path}.csv");
            List<TrialInfo> data = InitData(id);

            if (data?.Count > 0) {
                var n_trials = data.Count - 1;
                var data_files = data_dir.GetFiles("*.csv");
                var n_files = data_files.Length;

                if (n_trials > n_files)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            } else {
                return 0;
            }
        }

        public void ClearIdPath(string id)
        {
            var path = SavePath(id);
            var data_dir = new DirectoryInfo(path);
            var meta_file = new FileInfo($"{path}.csv");
            var new_dir = data_dir.FullName;
            var new_file = meta_file.FullName;
            var idx = 1;

            while (File.Exists(new_file) || Directory.Exists(new_dir))
            {
                new_dir = $"{path}_bkup{idx}";
                new_file = $"{path}_bkup{idx}.csv";
                idx++;
            }
            data_dir.MoveTo(new_dir);
            meta_file.MoveTo(new_file);
        }

        public List<TrialInfo> InitData(string id)
        {
            var path = SavePath(id);
            var data_dir = new DirectoryInfo(path);
            var meta_file = new FileInfo($"{path}.csv");
            List<TrialInfo> data = new List<TrialInfo>();

            if (meta_file.Exists && data_dir.Exists) {
                foreach(string l in File.ReadAllLines(meta_file.FullName).Skip(9))
                {
                    data.Add(new TrialInfo(l.Split(',')));
                }
            }

            return data;
        }

        public int GetLastTrial(string id)
        {
            var path = SavePath(id);
            var data_dir = new DirectoryInfo(path);
            var meta_file = new FileInfo($"{path}.csv");

            var init_data = InitData(id);
            var data_files = data_dir.GetFiles("*.csv");

            int i = 0;
            foreach( TrialInfo trial in init_data )
            {
                Regex r = new Regex(
                    $"{id}_T{i}_.*_{trial.TrialID}_Date_.*",
                    RegexOptions.Compiled);
                bool found = false;
                foreach( FileInfo df in data_files)
                {
                    if (r.IsMatch(df.Name))
                    {
                        found = true;
                        continue;
                    }
                }

                if (!found)
                {
                    return --i;
                }

                i++;
            }
            return i;
        }

        public void RecordSetup()
        {
            var dataFolder = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, this.savePath));

            if (!dataFolder.Exists)
            {
                dataFolder.Create();
            }

            var experimentDataDir = Directory.CreateDirectory(SavePath(Settings.ParticipantId));
            var experimentFile = $"{experimentDataDir.FullName}.csv";

            // create the writer.
            _writer = new StreamWriter(experimentFile);
            // Write metadata
            _writer?.WriteLine($"# Participant ID: {Settings.ParticipantId}");
            _writer?.WriteLine($"# Belief: {Settings.Belief}");
            _writer?.WriteLine($"# Condition: {Settings.Condition}");
            _writer?.WriteLine($"# Participant Avatar: {Settings.ParticipantAvatar}");
            _writer?.WriteLine($"# Partner Avatar: {Settings.PartnerAvatar}");
            _writer?.WriteLine($"# Run Practice Round: {Settings.RunPracticeRound}");
            _writer?.WriteLine($"# Practice Trial Count: {Settings.PracticeTrialCount}");
            _writer?.WriteLine($"# Start date: {DateTime.Now.ToString("dd'_'MM'_'yyyy'_'H''mm''ss")}");

            // Experiment process
            var csHeader = string.Join(",", INITInfo.Header);
            _writer.WriteLine(csHeader);

            foreach ( TrialInfo trial in Settings.Trials )
            {
                _writer?.WriteLine(string.Join(",", trial.InitInfo.Record()));
            }

            // Reset writer
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }

        private void OnDisable()
        {
            // If holding object gets destroyed or application ended we'll try so save everything waiting in the buffer.
            _writer?.Flush();
            _writer?.Dispose();
        }

        public enum TimeType
        {
            RecordingTime,
            UnityTime,
        }
    }

}