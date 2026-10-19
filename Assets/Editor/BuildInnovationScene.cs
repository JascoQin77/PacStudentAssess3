using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildInnovationScene
{
    const string SourcePath = "Assets/Scenes/RecreatedLevel.unity";
    const string InnovationPath = "Assets/Scenes/InnovationScene.unity";

    [MenuItem("Tools/PacStudent/Build Innovation Scene")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the innovation scene.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(source, InnovationPath, true))
            throw new InvalidOperationException("Could not save InnovationScene.");
        var innovation = EditorSceneManager.OpenScene(InnovationPath, OpenSceneMode.Single);

        var gameObject = GameObject.Find("Game Controller");
        if (gameObject == null) throw new InvalidOperationException("Game Controller is missing.");
        var controller = gameObject.GetComponent<InnovationController>();
        if (controller == null) controller = gameObject.AddComponent<InnovationController>();
        EditorUtility.SetDirty(controller);

        EditorSceneManager.MarkSceneDirty(innovation);
        EditorSceneManager.SaveScene(innovation);
        AddBuildSettings();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("InnovationScene built: Pulse Shift ability, cooldown HUD, and pulse visual effect.");
    }

    static void AddBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.All(scene => scene.path != InnovationPath))
            scenes.Add(new EditorBuildSettingsScene(InnovationPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    public static void Validate()
    {
        Require(SceneManager.GetActiveScene().path == InnovationPath, "InnovationScene is active.");
        Require(GameObject.Find("Game Controller")?.GetComponent<InnovationController>() != null,
            "InnovationController is attached.");
        Require(EditorBuildSettings.scenes.Any(scene => scene.path == InnovationPath && scene.enabled),
            "InnovationScene is enabled in Build Settings.");
        Require(GameObject.Find("PacStudent") != null && GameObject.Find("GhostGroup") != null,
            "InnovationScene retains the playable PacStudent level.");
        Debug.Log("INNOVATION_SCENE_PASS: second scene, Pulse Shift ability, and Build Settings validated.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
