using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prepares the saved manual scene for the "Catch-up from Assessment 3" criteria.
public static class PrepareAssessment4
{
    const string ScenePath = "Assets/Scenes/RecreatedLevel.unity";
    const string ControllerPath = "Assets/Animation/Controllers/";

    [MenuItem("Tools/PacStudent/Prepare Assessment 4 Catch-up")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before preparing the scene.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var manual = GameObject.Find("Level01_Manual");
        if (manual == null) throw new InvalidOperationException("The saved manual level is missing.");

        // These are old duplicate layouts and demonstration objects, not gameplay assets.
        foreach (string name in new[] { "LevelRoot", "LevelGenerator", "VisualAnimations" })
        {
            var old = GameObject.Find(name);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
        }
        foreach (var movement in UnityEngine.Object.FindObjectsByType<PacStudentMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(movement);

        FixWallRotations(manual);
        var pelletController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath + "PowerPelletAnimator.controller");
        if (pelletController == null) throw new InvalidOperationException("Power pellet controller is missing.");
        foreach (var renderer in manual.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.name != "PowerPellet") continue;
            var animator = renderer.GetComponent<Animator>();
            if (animator == null) animator = renderer.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = pelletController;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        var pac = GameObject.Find("PacStudent");
        if (pac == null) pac = new GameObject("PacStudent");
        SetCharacter(pac, new Vector3(-12.5f, 13.5f, 0), LoadSprite("PacStudent右面.png"), Color.white);
        // The old controller cycles through directions and death without player input.
        // Keep it in the Project view for the later gameplay implementation.
        foreach (var animator in pac.GetComponents<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
        foreach (var audio in pac.GetComponents<AudioSource>()) audio.playOnAwake = false;

        var ghosts = GameObject.Find("GhostGroup");
        if (ghosts == null) ghosts = new GameObject("GhostGroup");
        ghosts.transform.position = Vector3.zero;
        string[] names = { "GhostRed", "GhostPink", "GhostBlue", "GhostOrange" };
        Color[] colors = { new Color(1f, 0.45f, 0.45f), new Color(1f, 0.65f, 0.9f), new Color(0.45f, 0.85f, 1f), Color.white };
        for (int i = 0; i < names.Length; i++)
        {
            var ghost = GameObject.Find(names[i]);
            if (ghost == null) ghost = new GameObject(names[i]);
            ghost.transform.SetParent(ghosts.transform, false);
            SetCharacter(ghost, new Vector3(-1.5f + i, 0.5f, 0), LoadSprite("Ghost_down.png"), colors[i]);
            foreach (var animator in ghost.GetComponents<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
        }

        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Main camera is missing.");
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 16.5f;
        camera.backgroundColor = new Color(0.015f, 0.02f, 0.06f);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Assessment 4 catch-up scene prepared and saved.");
    }

    static Sprite LoadSprite(string file)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + file).OfType<Sprite>().OrderBy(s => s.name).FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Missing sprite: " + file);
        return sprite;
    }

    static void SetCharacter(GameObject go, Vector3 position, Sprite sprite, Color color)
    {
        go.SetActive(true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one;
        go.transform.rotation = Quaternion.identity;
        var renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 20;
    }

    static void FixWallRotations(GameObject manual)
    {
        var walls = manual.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("Tile_")).ToArray();
        var grid = walls.ToDictionary(r => (Vector2)r.transform.localPosition, r => int.Parse(r.name.Split('_')[1]));
        foreach (var wall in walls)
        {
            int kind = int.Parse(wall.name.Split('_')[1]);
            Vector2 position = wall.transform.localPosition;
            Func<Vector2, bool> connected = offset =>
            {
                if (!grid.TryGetValue(position + offset, out int other)) return false;
                // Separate outer and inner wall outlines at adjacent grid cells.
                return kind == 7 || ((kind == 1 || kind == 2) ? (other == 1 || other == 2 || other == 7) : (other == 3 || other == 4 || other == 8));
            };
            bool up = connected(Vector2.up), right = connected(Vector2.right);
            bool down = connected(Vector2.down), left = connected(Vector2.left);
            float angle = 0;
            // Imported corner sprites point down and right; the T points left, right and down.
            if (kind == 1 || kind == 3)
            {
                if (down && right) angle = 0;
                else if (up && right) angle = 90;
                else if (up && left) angle = 180;
                else if (down && left) angle = -90;
            }
            else if (kind == 7)
            {
                if (!down) angle = 180;
                else if (!left) angle = 90;
                else if (!up) angle = 0;
                else if (!right) angle = -90;
            }
            else if ((up || down) && !(left || right)) angle = 90;
            wall.transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public static void Validate()
    {
        var scene = SceneManager.GetActiveScene();
        Require(scene.path == ScenePath, "Validate RecreatedLevel.");
        var manual = GameObject.Find("Level01_Manual");
        Require(manual != null, "Manual level is present.");
        Require(UnityEngine.Object.FindObjectsByType<LevelGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, "No runtime level replacement.");
        Require(UnityEngine.Object.FindObjectsByType<PacStudentMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, "Old movement component is removed.");
        Require(GameObject.Find("VisualAnimations") == null && GameObject.Find("LevelRoot") == null, "Duplicate layout and examples are removed.");
        var pac = GameObject.Find("PacStudent");
        Require(pac != null && pac.GetComponent<SpriteRenderer>().sprite != null && pac.transform.position == new Vector3(-12.5f, 13.5f, 0), "PacStudent starts in the top-left corridor.");
        var ghosts = GameObject.Find("GhostGroup");
        Require(ghosts != null && ghosts.transform.childCount == 4, "Four ghosts are present.");
        foreach (Transform ghost in ghosts.transform)
            Require(ghost.GetComponent<SpriteRenderer>().sprite != null && ghost.GetComponent<Animator>() == null && Mathf.Abs(ghost.position.x) < 3 && Mathf.Abs(ghost.position.y) < 2, "Ghosts are static in the central pen.");
        var pellets = manual.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "PowerPellet").ToArray();
        Require(pellets.Length == 4, "Four power pellets are present.");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation/Pellet_Flash.anim");
        Require(clip != null && clip.isLooping, "Power pellet animation loops.");
        foreach (var pellet in pellets)
        {
            var animator = pellet.GetComponent<Animator>();
            Require(animator != null && animator.enabled && animator.runtimeAnimatorController != null, "Each power pellet has an active animator.");
            var original = pellet.sprite;
            clip.SampleAnimation(pellet.gameObject, 0);
            var first = pellet.sprite;
            clip.SampleAnimation(pellet.gameObject, 0.21f);
            var second = pellet.sprite;
            pellet.sprite = original;
            Require(first != null && second != null && first != second, "Flash frames resolve to different sprites.");
        }
        Require(AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath + "GhostAnimator_Base.controller") != null, "Ghost controller is retained in the Project view.");
        Debug.Log("Catch-up validation passed: manual level, character starts, no old movement/ghost cycles, four flashing pellets, retained ghost assets.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // A native Unity camera render for visual review; output stays outside Assets.
    public static void RenderPreview()
    {
        Validate();
        var camera = Camera.main;
        var target = new RenderTexture(1000, 1000, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var image = new Texture2D(1000, 1000, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1000, 1000), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/catchup-preview.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
