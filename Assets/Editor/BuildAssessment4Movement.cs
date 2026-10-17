using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildAssessment4Movement
{
    const string ScenePath = "Assets/Scenes/RecreatedLevel.unity";
    const string ControllerPath = "Assets/Animation/Controllers/PacStudentAnimator.controller";

    [MenuItem("Tools/PacStudent/Build Assessment 4 Movement")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building movement.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var pac = GameObject.Find("PacStudent");
        var maze = GameObject.Find("Level01_Manual");
        if (pac == null || maze == null) throw new InvalidOperationException("PacStudent or Level01_Manual is missing.");
        ConfigurePac(pac, maze.transform);
        ConfigureCherry(pac);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Assessment 4 movement built: PacStudentController, grid movement, animation/audio, dust particles, and CherryController.");
    }

    static void ConfigurePac(GameObject pac, Transform maze)
    {
        foreach (var old in pac.GetComponents<PacStudentMovement>()) UnityEngine.Object.DestroyImmediate(old);
        var animator = pac.GetComponent<Animator>();
        if (animator == null) animator = pac.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var audio = pac.GetComponent<AudioSource>();
        if (audio == null) audio = pac.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = true;
        audio.spatialBlend = 0f;
        audio.volume = .65f;

        var dust = pac.transform.Find("PacStudent Dust");
        if (dust == null)
        {
            dust = new GameObject("PacStudent Dust").transform;
            dust.SetParent(pac.transform, false);
            dust.localPosition = new Vector3(0, -.35f, .05f);
        }
        var particles = dust.GetComponent<ParticleSystem>();
        if (particles == null) particles = dust.gameObject.AddComponent<ParticleSystem>();
        ConfigureDust(particles);

        var controller = pac.GetComponent<PacStudentController>();
        if (controller == null) controller = pac.AddComponent<PacStudentController>();
        var so = new SerializedObject(controller);
        so.FindProperty("moveSpeed").floatValue = 5f;
        so.FindProperty("mazeRoot").objectReferenceValue = maze;
        so.FindProperty("animator").objectReferenceValue = animator;
        so.FindProperty("dustParticles").objectReferenceValue = particles;
        so.FindProperty("movementAudio").objectReferenceValue = audio;
        so.FindProperty("walkingClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/PacStudent_move.wav");
        so.FindProperty("pelletApproachClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Eat pellets.wav");
        so.FindProperty("tileSize").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    static void ConfigureDust(ParticleSystem particles)
    {
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = .35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, .45f);
        main.startSize = new ParticleSystem.MinMaxCurve(.06f, .13f);
        main.startColor = new Color(0.92f, .76f, .36f, .75f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 24f;
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = .12f;
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(.2f, .5f);
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 19;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        EditorUtility.SetDirty(particles);
    }

    static void ConfigureCherry(GameObject pac)
    {
        var manager = GameObject.Find("CherryController");
        if (manager == null) manager = new GameObject("CherryController");
        var controller = manager.GetComponent<CherryController>();
        if (controller == null) controller = manager.AddComponent<CherryController>();
        var audio = manager.GetComponent<AudioSource>();
        if (audio == null) audio = manager.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        var so = new SerializedObject(controller);
        so.FindProperty("cherrySprite").objectReferenceValue = LoadSprite("樱桃.png");
        so.FindProperty("levelCamera").objectReferenceValue = Camera.main;
        so.FindProperty("pacStudent").objectReferenceValue = pac.transform;
        so.FindProperty("hud").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
        so.FindProperty("collectClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Eat bonus cherry.wav");
        so.FindProperty("spawnDelay").floatValue = 5f;
        so.FindProperty("moveSpeed").floatValue = 5f;
        so.FindProperty("mapMin").vector2Value = new Vector2(-13.5f, -13.5f);
        so.FindProperty("mapMax").vector2Value = new Vector2(13.5f, 13.5f);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
    }

    static Sprite LoadSprite(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + file).OfType<Sprite>().FirstOrDefault();
    }

    public static void Validate()
    {
        Require(SceneManager.GetActiveScene().path == ScenePath, "RecreatedLevel is active.");
        var pac = GameObject.Find("PacStudent");
        Require(pac != null, "PacStudent exists.");
        Require(pac.GetComponent<PacStudentController>() != null, "PacStudentController is attached.");
        Require(pac.GetComponent<PacStudentMovement>() == null, "Old automatic route controller is removed.");
        Require(pac.GetComponent<Animator>() != null && pac.GetComponent<Animator>().runtimeAnimatorController != null, "PacStudent animator is bound.");
        Require(pac.GetComponentInChildren<ParticleSystem>() != null, "PacStudent dust particle effect exists.");
        Require(pac.GetComponent<AudioSource>() != null, "PacStudent movement audio source exists.");
        var cherry = GameObject.Find("CherryController");
        Require(cherry != null && cherry.GetComponent<CherryController>() != null, "CherryController exists.");
        Debug.Log("Assessment 4 movement validation passed: input controller, grid data, animation, audio, dust, and cherry spawner are bound.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
