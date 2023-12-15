using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SceneMenu
{
    [MenuItem("Scenes/Calibration")]
    public static void OpenCalibrationScene()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Calibration/EyeTrackingCalibration.unity",
            OpenSceneMode.Single);
    }

    [MenuItem("Scenes/StartMenu")]
    public static void OpenStartScene()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/StartMenu.unity",
            OpenSceneMode.Single);
    }


    [MenuItem("Scenes/BoxExperiment")]
    public static void OpenMenu()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistance.unity",
            OpenSceneMode.Single);

        EditorSceneManager.OpenScene(
            "Assets/Scenes/BoxExperiment/BoxExperiment.unity",
            OpenSceneMode.Additive);
    }

    [MenuItem("Scenes/BoxSetup")]
    public static void OpenSetupMenu()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistance.unity",
            OpenSceneMode.Single);

        EditorSceneManager.OpenScene(
            "Assets/Scenes/BoxExperiment/BoxSetup.unity",
            OpenSceneMode.Additive);
    }

    [MenuItem("Scenes/BoxPractice")]
    public static void OpenBoxPractice()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistance.unity",
            OpenSceneMode.Single);

        EditorSceneManager.OpenScene(
            "Assets/Scenes/BoxExperiment/BoxPractice.unity",
            OpenSceneMode.Additive);
    }

    [MenuItem("Scenes/BoxFinalization")]
    public static void OpenBoxFinalization()
    {
        EditorSceneManager.OpenScene(
            "Assets/Scenes/Persistance.unity",
            OpenSceneMode.Single);

        EditorSceneManager.OpenScene(
            "Assets/Scenes/BoxExperiment/BoxFinalization.unity",
            OpenSceneMode.Additive);
    }


}
