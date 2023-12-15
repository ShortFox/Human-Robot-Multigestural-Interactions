using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
///     Behaviour for the StartMenu scene's canvas.
///     Includes all methods to select a task and load its corresponding scene.
/// </summary>
public class StartMenu : MonoBehaviour
{
    private string _chosenTask;
    [SerializeField] Dropdown _selectTask;
    [SerializeField] Button _loadButton;
    [SerializeField] Button _exitButton;
    [SerializeField] Button _calibrationButton;

    /// <summary>Start method. Callbacks for all required listeners in the canvas should be set here.</summary>
    public void Start()
    {
        _selectTask.onValueChanged.AddListener(delegate
        {
            OnTaskSelected(_selectTask.value);
        });

        _loadButton.interactable = false;
        _loadButton.onClick.AddListener(LoadScene);

        _exitButton.onClick.AddListener(ExitApplication);

        _calibrationButton.onClick.AddListener(OpenCalibrationScene);
    }

    /// <summary>Callback for task selection dropdown.</summary>
    /// <param name="newSelection">Index of the item selected in the drowpdown.</param>
    public void OnTaskSelected(int newSelection)
    {
        switch (newSelection)
        {
            case 1:
                _loadButton.interactable = true;
                _chosenTask = "Herding";
                break;
            case 2:
                _loadButton.interactable = true;
                _chosenTask = "Persistance";
                break;
            default:
                _loadButton.interactable = false;
                _chosenTask = null;
                break;
        }
    }

    /// <summary>
    ///     Terminate the application. It will stop the application or play mode when running on the editor.
    /// </summary>
    public void ExitApplication()
    {
        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }

    public void OpenCalibrationScene()
    {
        SceneManager.LoadScene("EyeTrackingCalibration");
    }

    /// <summary>Load the scene chosen in the dropdown menu.</summary>
    public void LoadScene()
    {
        SceneManager.LoadScene(_chosenTask);
    }
}
