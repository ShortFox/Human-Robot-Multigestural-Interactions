using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string InitialScene = "BoxSetup";

    public UnityEvent OnLoadBegin = new UnityEvent();
    public UnityEvent OnLoadEnd = new UnityEvent();

    private static SceneLoader instance = null;

    public static SceneLoader Instance
    {
        get
        {
            if ((object)instance == null)
            {
                instance = (SceneLoader)FindObjectOfType(typeof(SceneLoader));

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("SceneLoader");
                    instance = singletonObject.AddComponent<SceneLoader>();
                }
            }
            return instance;
        }
    }

    private bool isLoading = false;

    private void Awake()
    {
        SceneManager.sceneLoaded += SetActiveScene;
        if (SceneManager.sceneCount < 2)
        {
            StartCoroutine(LoadNew(InitialScene));
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SetActiveScene;
    }

    public void LoadNewScene(string sceneName)
    {
        if (!isLoading)
        {
            StartCoroutine(LoadScene(sceneName));
        }
    }

    private IEnumerator LoadScene(string sceneName)
    {
        isLoading = true;

        OnLoadBegin?.Invoke();

        // Warning: For a very short period of time we will have two scenes loaded at the same time.
        // This could cause issues in some cases, we need to make sure that each scene does a good job destroying
        // itself to avoid issues.
        var currentScene = SceneManager.GetActiveScene();

        yield return StartCoroutine(LoadNew(sceneName));
        yield return StartCoroutine(UnloadCurrent(currentScene));

        OnLoadEnd?.Invoke();

        isLoading = false;
    }

    private IEnumerator UnloadCurrent(Scene scene)
    {
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);

        while (!unloadOperation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator LoadNew(string sceneName)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void SetActiveScene(Scene scene, LoadSceneMode mode)
    {
        SceneManager.SetActiveScene(scene);
    }

}
