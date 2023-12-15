using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MQ.MultiAgent.Box
{
    /// <summary>
    ///     Behaviour for the BoxSetup Canvas.
    ///     Used to set up and start the Box experiment.
    /// </summary>
    public class BoxSetup : MonoBehaviour
    {
        [SerializeField] ExperimentRecorder _recorder;

        [SerializeField] Button _loadExperimentButton;
        [SerializeField] Button _calibrationButton;
        [SerializeField] Dropdown _avatarSelect;
        [SerializeField] Dropdown _partnerAvatarSelect;
        [SerializeField] Dropdown _beliefSelect;
        [SerializeField] Dropdown _conditionSelect;
        [SerializeField] InputField _participantField;
        [SerializeField] Text _debugMessage;
        [SerializeField] Toggle _practiceToggle;
        [SerializeField] InputField _practiceTrialField;

        [SerializeField] GameObject _continueDialogCanvas;
        [SerializeField] Button _continueDialogCancelButton;
        [SerializeField] Button _continueDialogRestartButton;
        [SerializeField] Button _continueDialogContinueButton;
        [SerializeField] Text _continueDialogText;

        [SerializeField] GameObject _loadingIndicator;

        BidirectionalMap<int, string> avatarSelection = new BidirectionalMap<int, string>(
            new Dictionary<int, string> {
                { 0, "" },
                { 1, "Female" },
                { 2, "iCub" },
                { 3, "Male" },
            }
        );

        BidirectionalMap<int, string> beliefType = new BidirectionalMap<int, string>(
            new Dictionary<int, string> {
                { 0, "" },
                { 1, "Partner Local" },
                { 2, "Partner Elsewhere" },
            }
        );

        BidirectionalMap<int, string> conditionType = new BidirectionalMap<int, string>(
            new Dictionary<int, string> {
                { 0, "" },
                { 1, "5050_HeadConstrained" },
                { 2, "5050_HeadUnConstrained" },
                { 3, "7525_HeadConstrained" },
                { 4, "7525_HeadUnConstrained" },
                { 5, "2575_HeadConstrained" },
                { 6, "2575_HeadUnConstrained" },
            }
        );

        /// <summary>Executed on start. Callbacks for all required listeners in the canvas should be set here.</summary>
        private void Start()
        {
            CheckSettings();

            // Participant ID
            if (!string.IsNullOrEmpty(Settings.ParticipantId))
            {
                _participantField.text = Settings.ParticipantId;
            }
            _participantField.onEndEdit.AddListener(delegate
            {
                Settings.ParticipantId = _participantField.text;
                CheckSettings();
            });

            // Participant Avatar
            if (!string.IsNullOrEmpty(Settings.ParticipantAvatar))
            {
            _avatarSelect.value = avatarSelection.Reverse[Settings.ParticipantAvatar.Replace(" VR", "")];
            }
            _avatarSelect.onValueChanged.AddListener(delegate
            {
                string selected = avatarSelection.Forward[_avatarSelect.value];
                if (!string.IsNullOrEmpty(selected))
                {
                    Settings.ParticipantAvatar = selected + " VR";
                    AvatarLoader.Instance?.LoadMain();
                }
                CheckSettings();
            });

            // Partner Avatar
            if (!string.IsNullOrEmpty(Settings.PartnerAvatar))
            {
                _partnerAvatarSelect.value = avatarSelection.Reverse[Settings.PartnerAvatar.Replace(" AI", "")];
            }
            _partnerAvatarSelect.onValueChanged.AddListener(delegate
            {
                string selected = avatarSelection.Forward[_partnerAvatarSelect.value];
                if (!string.IsNullOrEmpty(selected))
                {
                    Settings.PartnerAvatar = selected + " AI";
                    AvatarLoader.Instance?.LoadPartner();
                }
                CheckSettings();
            });

            // Belief
            if (!string.IsNullOrEmpty(Settings.Belief))
            {
                _beliefSelect.value = beliefType.Reverse[Settings.Belief];
            }
            _beliefSelect.onValueChanged.AddListener(delegate
            {
                Settings.Belief = beliefType.Forward[_beliefSelect.value];
                CheckSettings();
            });

            // Condition
            if (!string.IsNullOrEmpty(Settings.Condition))
            {
                _conditionSelect.value = conditionType.Reverse[Settings.Condition];
            }
            _conditionSelect.onValueChanged.AddListener(delegate
            {
                Settings.Condition = conditionType.Forward[_conditionSelect.value];
                CheckSettings();
            });

            // Run practice
            _practiceToggle.isOn = Settings.RunPracticeRound;
            _practiceToggle.onValueChanged.AddListener(delegate {
                Settings.RunPracticeRound = _practiceToggle.isOn;
            });

            if (Settings.PracticeTrialCount > 0)
            {
                _practiceTrialField.text = Settings.PracticeTrialCount.ToString();
            }
            _practiceTrialField.onValueChanged.AddListener(delegate {
                Settings.PracticeTrialCount = Int32.Parse(_practiceTrialField.text);
            });

            _loadExperimentButton.onClick.AddListener(delegate {
                CheckReplay();
            });

            _calibrationButton.onClick.AddListener(delegate {
                var sceneManager = SceneLoader.Instance;
                sceneManager.LoadNewScene("EyeTrackingCalibration");
            });
        }

        /// <summary>Check if all settings are selected.</summary>
        private void CheckSettings()
        {
            if (string.IsNullOrEmpty(Settings.Condition) || string.IsNullOrEmpty(Settings.ParticipantId) ||
                string.IsNullOrEmpty(Settings.ParticipantAvatar) || string.IsNullOrEmpty(Settings.PartnerAvatar) ||
                string.IsNullOrEmpty(Settings.Belief))
            {
                _loadExperimentButton.interactable = false;
            }
            else
            {
                _loadExperimentButton.interactable = true;
            }
        }

        private void CheckReplay()
        {
            var id = Settings.ParticipantId;
            Settings.SavePath = _recorder.SavePathRel(id);
            switch (_recorder.CheckIdFile(id))
            {
                case 1:
                case 2:
                    _continueDialogCancelButton.onClick.AddListener(delegate {
                        _continueDialogCanvas.SetActive(false);
                    });
                    _continueDialogContinueButton.onClick.AddListener(delegate {
                        Settings.TrialIdx = _recorder.GetLastTrial(id);
                        Settings.Trials = _recorder.InitData(id);
                        OpenNewScene();
                    });
                    _continueDialogRestartButton.onClick.AddListener(delegate {
                        Settings.TrialIdx = 0;
                        Settings.Trials = _recorder.InitData(id);
                        _recorder.ClearIdPath(id);
                        // Disable other callbacks
                        _continueDialogCancelButton.interactable = false;
                        _continueDialogContinueButton.interactable = false;
                        _continueDialogRestartButton.interactable = false;
                        // Reload experiment
                        StartCoroutine(LoadNewExperimentAsync());
                    });
                    _continueDialogCanvas.SetActive(true);
                    break;
                default:
                    Settings.TrialIdx = 0;
                    StartCoroutine(LoadNewExperimentAsync());
                    break;
            }
        }

        private void OpenNewScene()
        {
            var sceneManager = SceneLoader.Instance;
            var nextScene = Settings.RunPracticeRound ? "BoxPractice" : "BoxExperiment";
            sceneManager.LoadNewScene(nextScene);
        }

        protected IEnumerator LoadNewExperimentAsync()
        {
            _continueDialogCanvas.SetActive(false);
            _loadingIndicator.SetActive(true);
            // Trial setup
            // Trial list needs to run asynchronously to avoid freezing the application.
            var t = System.Threading.Tasks.Task.Run(() => {
                Settings.Trials = ExpInitializer.Instance.GetTrialList();
            });
            yield return new WaitUntil(() => t.IsCompleted);

            var x = Settings.Trials;
            _recorder.RecordSetup();

            OpenNewScene();
        }
    }
}
