using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildAssessment4Collisions
{
    const string ScenePath = "Assets/Scenes/RecreatedLevel.unity";
    const string AnimationPath = "Assets/Animation/";

    [MenuItem("Tools/PacStudent/Build Assessment 4 Collisions and Game Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var player = GameObject.Find("PacStudent").GetComponent<PacStudentController>();
        var hud = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
        if (player == null || hud == null) throw new InvalidOperationException("Build movement and HUD first.");

        // The showcase controllers have automatic demo transitions. Gameplay
        // controllers use the same clips, with transitions owned by game state.
        var pacAnimator = CreateController("PacStudentGameplay", new[] {
            "Walking_Up", "Walking_Right", "Walking_Down", "Walking_Left", "Dead" },
            new[] { "PacStudent_Walk_Up", "PacStudent_Walk_Right", "PacStudent_Walk_Down", "PacStudent_Walk_Left", "PacStudent_Dead" });
        var ghostAnimator = CreateController("GhostGameplay", new[] { "Normal", "Scared", "Recovering", "Dead" },
            new[] { "Ghost_Normal_down", "Ghost_Scared_down", "Ghost_Recovering", "Ghost_Dead" });
        player.GetComponent<Animator>().runtimeAnimatorController = pacAnimator;
        var playerCollider = GetOrAdd<CircleCollider2D>(player.gameObject);
        playerCollider.isTrigger = true;
        playerCollider.radius = .28f;
        ConfigureBody(player.gameObject);

        var maze = GameObject.Find("Level01_Manual");
        foreach (var sprite in maze.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sprite.name.StartsWith("Tile_"))
            {
                var wall = GetOrAdd<BoxCollider2D>(sprite.gameObject);
                wall.isTrigger = true;
                wall.size = new Vector2(.9f, .9f);
            }
            else if (sprite.name == "StandardPellet" || sprite.name == "PowerPellet")
            {
                var trigger = GetOrAdd<CircleCollider2D>(sprite.gameObject);
                trigger.isTrigger = true;
                trigger.radius = sprite.name == "PowerPellet" ? .22f : .12f;
                var item = GetOrAdd<LevelCollectible>(sprite.gameObject);
                item.kind = sprite.name == "PowerPellet" ? LevelCollectible.CollectibleKind.PowerPill : LevelCollectible.CollectibleKind.Pellet;
            }
        }

        var ghostRoot = GameObject.Find("GhostGroup");
        foreach (Transform ghost in ghostRoot.transform)
        {
            var animator = GetOrAdd<Animator>(ghost.gameObject);
            animator.runtimeAnimatorController = ghostAnimator;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var trigger = GetOrAdd<CircleCollider2D>(ghost.gameObject);
            trigger.isTrigger = true;
            trigger.radius = .3f;
            ConfigureBody(ghost.gameObject);
            GetOrAdd<GhostStateController>(ghost.gameObject);
        }

        CreateTunnel("Left Tunnel Teleporter", -14.5f, 13.5f, Vector2Int.left);
        CreateTunnel("Right Tunnel Teleporter", 14.5f, -13.5f, Vector2Int.right);
        CreateRoundOverlay(hud);

        // Retire the old audio demo, which switches music independently after 3s.
        foreach (var demo in UnityEngine.Object.FindObjectsByType<AudioPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(demo);
        foreach (var audio in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            audio.Stop();
            audio.playOnAwake = false;
        }
        var root = GameObject.Find("Game Controller");
        if (root == null) root = new GameObject("Game Controller");
        var game = GetOrAdd<PacStudentGameController>(root);
        var music = AudioChild(root.transform, "Game Music", .45f);
        var effects = AudioChild(root.transform, "Game Effects", .8f);
        var wallParticles = Burst(root.transform, "Wall Bump Particles", new Color(1, .7f, .25f), 12, .3f);
        var deathParticles = Burst(root.transform, "PacStudent Death Particles", new Color(1, .85f, .15f), 40, .8f);
        var so = new SerializedObject(game);
        Bind(so, "player", player);
        Bind(so, "hud", hud);
        var ghosts = ghostRoot.GetComponentsInChildren<GhostStateController>();
        var array = so.FindProperty("ghosts");
        array.arraySize = ghosts.Length;
        for (int i = 0; i < ghosts.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = ghosts[i];
        Bind(so, "music", music); Bind(so, "effects", effects);
        Bind(so, "wallParticles", wallParticles); Bind(so, "deathParticles", deathParticles);
        string[] fields = { "introClip", "normalClip", "scaredClip", "deadGhostClip", "wallClip", "pelletClip", "cherryClip", "ghostEatClip", "deathClip" };
        string[] clips = { "Game intro", "Normal ghost", "Scared ghost", "Ghost death", "PacStudent collides with a wall", "Eat pellets", "Eat bonus cherry", "Eat ghost", "PacStudent_death" };
        for (int i = 0; i < fields.Length; i++) Bind(so, fields[i], AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/" + clips[i] + ".wav"));
        Bind(so, "deathAnimation", AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationPath + "PacStudent_Dead.anim"));
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(game);

        // Enforce the assessment rule even if a pre-existing body is added later.
        foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0;
        }
        foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None)) body.isKinematic = true;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Validate();
    }

    static AnimatorController CreateController(string name, string[] states, string[] clips)
    {
        string path = AnimationPath + "Controllers/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var old in machine.states) machine.RemoveState(old.state);
        for (int i = 0; i < states.Length; i++)
        {
            var state = machine.AddState(states[i], new Vector3(i * 180, 100, 0));
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimationPath + clips[i] + ".anim");
            if (state.motion == null) throw new InvalidOperationException("Missing animation " + clips[i]);
            if (i == 0) machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void ConfigureBody(GameObject go)
    {
        var body = GetOrAdd<Rigidbody2D>(go);
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        body.useFullKinematicContacts = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    static void CreateTunnel(string name, float x, float destinationX, Vector2Int inward)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        go.transform.position = new Vector3(x, 0, 0);
        var trigger = GetOrAdd<BoxCollider2D>(go);
        trigger.isTrigger = true;
        trigger.size = new Vector2(.5f, 1.8f);
        var tunnel = GetOrAdd<TunnelTeleporter>(go);
        tunnel.destinationX = destinationX;
        tunnel.inwardDirection = inward;
    }

    static void CreateRoundOverlay(InGameHUDController hud)
    {
        var canvas = GameObject.Find("HUD").transform;
        var old = canvas.Find("Round Blocking Overlay");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Round Blocking Overlay", typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(canvas, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = new Color(.015f, .025f, .06f, .75f);
        image.raycastTarget = true;
        // World-space ghost labels use order 110; the blocker must cover those
        // too, rather than only darkening the main camera and HUD canvas.
        var overlayCanvas = go.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 200;
        go.AddComponent<GraphicRaycaster>();
        var label = new GameObject("Round Countdown and Game Over", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        var textRect = label.rectTransform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = new Vector2(.1f, .3f); textRect.anchorMax = new Vector2(.9f, .7f);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "3"; label.fontSize = 110; label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(.4f, .95f, .83f);
        label.raycastTarget = false;
        hud.ConfigureRoundUI(go, label, canvas.GetComponentsInChildren<Button>(true));
        go.SetActive(false);
        EditorUtility.SetDirty(hud);
    }

    static AudioSource AudioChild(Transform parent, string name, float volume)
    {
        var child = parent.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
        var source = GetOrAdd<AudioSource>(child.gameObject);
        source.playOnAwake = false; source.spatialBlend = 0; source.volume = volume;
        return source;
    }

    static ParticleSystem Burst(Transform parent, string name, Color color, int count, float lifetime)
    {
        var child = parent.Find(name);
        if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
        var system = GetOrAdd<ParticleSystem>(child.gameObject);
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.playOnAwake = false; main.loop = false; main.duration = lifetime;
        main.startLifetime = lifetime; main.startSpeed = new ParticleSystem.MinMaxCurve(.7f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(.04f, .13f); main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = system.emission;
        emission.enabled = true; emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
        var shape = system.shape;
        shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .1f;
        var renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 60;
        // Use Unity's particle material so the effect renders without a pipeline asset.
        renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        return system;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static void Bind(SerializedObject so, string field, UnityEngine.Object value) => so.FindProperty(field).objectReferenceValue = value;

    public static void Validate()
    {
        Require(SceneManager.GetActiveScene().path == ScenePath, "Gameplay scene is active");
        var game = UnityEngine.Object.FindFirstObjectByType<PacStudentGameController>();
        Require(game != null, "Game controller exists");
        var so = new SerializedObject(game);
        foreach (var field in new[] { "player", "hud", "music", "effects", "wallParticles", "deathParticles", "introClip", "normalClip", "scaredClip", "deadGhostClip", "wallClip", "pelletClip", "cherryClip", "ghostEatClip", "deathClip", "deathAnimation" })
            Require(so.FindProperty(field).objectReferenceValue != null, field + " is bound");
        Require(so.FindProperty("ghosts").arraySize == 4, "Four ghosts are bound");
        Require(UnityEngine.Object.FindObjectsByType<TunnelTeleporter>(FindObjectsSortMode.None).Length == 2, "Two tunnel teleporters");
        var items = UnityEngine.Object.FindObjectsByType<LevelCollectible>(FindObjectsSortMode.None);
        Require(items.Count(p => p.kind == LevelCollectible.CollectibleKind.PowerPill) == 4, "Four power pills");
        Require(items.Count(p => p.kind == LevelCollectible.CollectibleKind.Pellet) > 100, "Pellets have collection triggers");
        foreach (var item in items) Require(item.GetComponent<Collider2D>().isTrigger, "Collectible trigger");
        foreach (var body in UnityEngine.Object.FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Require(body.bodyType == RigidbodyType2D.Kinematic, "Kinematic Rigidbody2D");
        var overlay = GameObject.Find("HUD").transform.Find("Round Blocking Overlay");
        Require(overlay != null && !overlay.gameObject.activeSelf, "Blocking overlay hidden by default");
        Require(overlay.GetComponent<Canvas>().sortingOrder > 110, "Overlay also covers ghost labels");
        Require(!UnityEngine.Object.FindObjectsByType<AudioPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(), "No competing audio demo");
        Debug.Log("ASSESSMENT4_COLLISIONS_SCENE_PASS: colliders, controllers, audio, particles, HUD overlay, and tunnel references validated.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
