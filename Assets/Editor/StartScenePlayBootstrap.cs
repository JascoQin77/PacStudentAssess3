using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Makes the editor Play button behave like a built game: start at StartScene.
[InitializeOnLoad]
public static class StartScenePlayBootstrap
{
    const string StartScenePath = "Assets/Scenes/StartScene.unity";
    static bool switching;

    static StartScenePlayBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode || switching) return;
        if (SceneManager.GetActiveScene().path == StartScenePath) return;

        switching = true;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Single);
        else
            EditorApplication.isPlaying = false;
        switching = false;
    }
}
