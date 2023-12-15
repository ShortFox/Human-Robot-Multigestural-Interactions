using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tobii.Research.Unity;

public class CalibrationMenu : MonoBehaviour
{
    [SerializeField] Button _startCalibrationButton;
    [SerializeField] Button _tooglePositioningGuideButton;
    [SerializeField] Button _toogleGazeTrailButton;
    [SerializeField] Button _exitCalibrationButton;

    [SerializeField] VRCalibration _vrCalibration;
    [SerializeField] VRPositioningGuide _vrPositioningGuide;
    [SerializeField] VRGazeTrail _vrGazeTrail;

    [SerializeField]
    [Tooltip("Key to press to exit the scene and go back to the Start menu.")]
    private KeyCode _exitScene = KeyCode.None;

    // Start is called before the first frame update
    void Start()
    {
        _startCalibrationButton.onClick.AddListener(delegate {
            var calibrationStartResult = _vrCalibration.StartCalibration(
                resultCallback: (calibrationResult) =>
                    Debug.Log("Calibration was " + (calibrationResult ? "successful" : "unsuccessful"))
            );
            Debug.Log("Calibration " + (calibrationStartResult ? "" : "not ") + "started");
        });

        _toogleGazeTrailButton.onClick.AddListener(delegate {
            _vrGazeTrail.On = !_vrGazeTrail.On;
        });

        _tooglePositioningGuideButton.onClick.AddListener(delegate {
            _vrPositioningGuide.PositioningGuideActive = !_vrPositioningGuide.PositioningGuideActive;
        });

        _exitCalibrationButton.onClick.AddListener(delegate {
            ExitCalibration();
        });
    }

    void Update()
    {
        if (Input.GetKeyDown(_exitScene))
        {
            ExitCalibration();
        }
    }

    void ExitCalibration() {
        var eye_tracker_origin = VRUtility.EyeTrackerOriginVive;
        Destroy(eye_tracker_origin.Find("VRCalibrationPoint")?.gameObject);
        Destroy(eye_tracker_origin.Find("[VRPositioningGuide]")?.gameObject);
        var sceneManager = SceneLoader.Instance;
        sceneManager.LoadNewScene(sceneManager.InitialScene);
    }
}
