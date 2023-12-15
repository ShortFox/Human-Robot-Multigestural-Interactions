using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace MQ.MultiAgent
{
    public class RecordVideoTask: Task
    {
        public override string TaskName { get { return "RecordVideo"; } }
        public override string TrialID { get { return _trialIdx.ToString(); } }
        public override int TrialNum { get; }
        protected List<Box.TrialInfo> Trials;                                                             //List that stores shuffled trials.
        public override int BlockNum { get; }

        public int _trialIdx = 0;
        public string TargetFolder;
        private RecorderController cam;

        private List<ReplayInput> replayInputs;

        public bool Recording = true;

        public override IEnumerator SetUp()
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                ReplayInput m = player.GetComponent<ReplayInput>();
                if (m != null)
                {
                    m.Stopped += OnControllerStopped;
                    replayInputs.Add(m);
                }
            }
            if (String.IsNullOrEmpty(TargetFolder))
            {
                TargetFolder = Application.dataPath;
            }
            Trials = Box.ExpInitializer.Instance.GetTrialList(false);
            Phase++;
            yield break;
        }

        public override IEnumerator Initiate()
        {
            Box.TrialInfo trial = Trials[_trialIdx];

            foreach (ReplayInput m in replayInputs)
            {
                m.SetTrialInfo(trial);
                m.Initiate();
            }

            StopCamera();
            if (Recording)
            {
                string mid_folder = trial.RoleB == "Responder" ?
                                    $"target_{trial.TargetLocation}" :
                                    $"target_{trial.TargetLocation}_gaze_{trial.GazeCongruency}";
                SetUpCamera(Path.Combine($"{TargetFolder}/{Name}",
                                            trial.RoleB,
                                            mid_folder,
                                            trial.PlaybackID));
            }

            Phase++;
            yield break;
        }

        public override IEnumerator Run()
        {

            yield return new WaitForSeconds(3);

            if (Recording)
            {
                StartCamera();
            }

            foreach (ReplayInput m in replayInputs)
            {
                m.Run();
            }

            yield break;
        }

        public override IEnumerator Done()
        {
            if (_trialIdx > Trials.Count)
            {
                Phase++;
                yield break;
            }
            _trialIdx++;
            Phase = TaskPhase.Run;
            yield break;
        }

        public override IEnumerator Finished()
        {
            StopCamera();
            UnityEditor.EditorApplication.isPlaying = false;
            yield break;
        }


        private void SetUpCamera(string outputFile)
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var rs = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            rs.OutputFile = Path.ChangeExtension(outputFile, null);

            rs.name = "recorder";
            rs.Enabled = true;
            // rs.VideoBitrateMode = VideoBitrateMode.High;
            rs.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080,
            };
            // rs.AudioInputSettings.PreserveAudio = true;
            rs.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.WebM;

            controllerSettings.AddRecorderSettings(rs);
            // controllerSettings.SetRecordModeToFrameInterval(0, 59);
            controllerSettings.FrameRate = 60;

            cam = new RecorderController(controllerSettings);
        }

        private float timer = 0;

        private bool Waited(float seconds)
        {
            timer += Time.deltaTime;

            if (timer >= seconds)
            {
                timer = 0;
                return true; //max reached - waited x - seconds
            }
            return false;
        }

        private void StartCamera()
        {
            cam.PrepareRecording();
            cam.StartRecording();
        }

        private void StopCamera()
        {
            if (cam != null)
            {
                cam.StopRecording();
            }
        }

        void OnControllerStopped(object sender, EventArgs e)
        {
            Phase = TaskPhase.Done;
        }

    }
}