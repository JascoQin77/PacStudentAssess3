using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class RebuildVisualShowcase
{
    const string ScenePath = "Assets/Scenes/VisualAssets_Showcase.unity";
    const string SpritePath = "Assets/Sprites/";
    const string ControllerPath = "Assets/Animation/Controllers/";

    static Sprite Sprite(string file, string subName = null)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath + file);
        foreach (var a in assets)
            if (a is Sprite && (subName == null || a.name == subName)) return (Sprite)a;
        return null;
    }

    static GameObject AddSprite(Transform parent, string name, Vector3 position, Sprite sprite, float scale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 10;
        return go;
    }

    static void Bind(GameObject go, string controller)
    {
        var a = go.AddComponent<Animator>();
        a.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath + controller);
        a.enabled = true;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("VisualAssets_Showcase");

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.transform.position = new Vector3(0, 0, -10);
        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.02f, 0.06f);

        var pac = AddSprite(root.transform, "PacStudent_Animated", new Vector3(-6, 2, 0), Sprite("PacStudent正面.png", "PacStudent正面_0"));
        Bind(pac, "PacStudentAnimator.controller");
        var ghost = AddSprite(root.transform, "Ghost_Normal_Animated", new Vector3(-2, 2, 0), Sprite("Ghost_down.png"));
        Bind(ghost, "GhostAnimator_Base.controller");
        var scared = AddSprite(root.transform, "Ghost_Scared_Animated", new Vector3(2, 2, 0), Sprite("ScaredGhost_down.png"));
        Bind(scared, "GhostAnimator_Pink.overrideController");
        var dead = AddSprite(root.transform, "Ghost_Dead_Animated", new Vector3(6, 2, 0), Sprite("Ghost_dead.png"));
        Bind(dead, "GhostAnimator_Red.overrideController");
        var pellet = AddSprite(root.transform, "PowerPellet_Animated", new Vector3(-4, -2, 0), Sprite("颗粒.png"), 0.25f);
        Bind(pellet, "PowerPelletAnimator.controller");
        AddSprite(root.transform, "BonusCherry", new Vector3(0, -2, 0), Sprite("樱桃.png"), 0.25f);
        AddSprite(root.transform, "LifeIndicator", new Vector3(4, -2, 0), Sprite("life indicator.png"), 0.2f);
        AddSprite(root.transform, "Wall_Outside", new Vector3(-6, -3.5f, 0), Sprite("Wall_Outside.png"));
        AddSprite(root.transform, "Wall_Inside", new Vector3(-3, -3.5f, 0), Sprite("Wall_Inside.png"));
        AddSprite(root.transform, "Wall_TJunction", new Vector3(0, -3.5f, 0), Sprite("Wall_TJunction.png"));
        AddSprite(root.transform, "Wall_GhostExit", new Vector3(3, -3.5f, 0), Sprite("Wall_GhostExit.png"));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Rebuilt VisualAssets_Showcase with Unity serialization.");
    }

    public static void IntegrateIntoRecreatedLevel()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity", OpenSceneMode.Single);
        var old = GameObject.Find("VisualAnimations");
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("VisualAnimations");

        var camera = Camera.main;
        if (camera == null)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
        }
        camera.transform.position = new Vector3(0, 1, -10);
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.02f, 0.06f);

        var pac = AddSprite(root.transform, "PacStudent_Animated", new Vector3(-6, 4, 0), Sprite("PacStudent正面.png", "PacStudent正面_0"));
        Bind(pac, "PacStudentAnimator.controller");
        var normal = AddSprite(root.transform, "Ghost_Normal_Animated", new Vector3(-2, 4, 0), Sprite("Ghost_down.png"));
        Bind(normal, "GhostAnimator_Base.controller");
        var scared = AddSprite(root.transform, "Ghost_Scared_Animated", new Vector3(2, 4, 0), Sprite("ScaredGhost_down.png"));
        Bind(scared, "GhostAnimator_Pink.overrideController");
        var dead = AddSprite(root.transform, "Ghost_Dead_Animated", new Vector3(6, 4, 0), Sprite("Ghost_dead.png"));
        Bind(dead, "GhostAnimator_Red.overrideController");
        var pellet = AddSprite(root.transform, "PowerPellet_Animated", new Vector3(-4, 0, 0), Sprite("颗粒.png"), 0.25f);
        Bind(pellet, "PowerPelletAnimator.controller");
        AddSprite(root.transform, "BonusCherry", new Vector3(0, 0, 0), Sprite("樱桃.png"), 0.25f);
        AddSprite(root.transform, "LifeIndicator", new Vector3(4, 0, 0), Sprite("life indicator.png"), 0.2f);

        var audioObject = GameObject.Find("AudioSource_Player");
        if (audioObject != null)
        {
            var source = audioObject.GetComponent<AudioSource>();
            source.playOnAwake = true;
            var player = audioObject.GetComponent<AudioPlayer>();
            if (player != null)
            {
                var so = new SerializedObject(player);
                so.FindProperty("introBGM").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Game intro.wav");
                so.FindProperty("ghostNormalBGM").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Normal ghost.wav");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Visual animations integrated into RecreatedLevel.");
    }

    public static void BuildManualLevelLayout()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity", OpenSceneMode.Single);
        var previous = GameObject.Find("Level01_Manual");
        if (previous != null) Object.DestroyImmediate(previous);
        var level = new GameObject("Level01_Manual");
        var walls = new GameObject("WallTiles");
        walls.transform.SetParent(level.transform, false);
        var pellets = new GameObject("Pellets");
        pellets.transform.SetParent(level.transform, false);

        int[,] map = new int[,] {
            {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
            {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
            {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
            {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
            {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
            {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
            {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
            {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
            {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
            {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
            {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
            {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
            {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
            {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
            {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
        };
        int rows = map.GetLength(0), cols = map.GetLength(1);
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            AddManualTile(map[r, c], r, c, false, false, rows, cols, walls.transform, pellets.transform);
            AddManualTile(map[r, c], r, c, true, false, rows, cols, walls.transform, pellets.transform);
            AddManualTile(map[r, c], r, c, false, true, rows, cols, walls.transform, pellets.transform);
            AddManualTile(map[r, c], r, c, true, true, rows, cols, walls.transform, pellets.transform);
        }

        var camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true;
            camera.orthographicSize = 16.5f;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Manual Level 01 layout built in RecreatedLevel.");
    }

    public static void BindPacStudentMovement()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity", OpenSceneMode.Single);
        var pac = GameObject.Find("PacStudent_Animated");
        if (pac == null) throw new System.Exception("PacStudent_Animated was not found.");
        var pacRenderer = pac.GetComponent<SpriteRenderer>();
        if (pacRenderer == null) pacRenderer = pac.AddComponent<SpriteRenderer>();
        var pacSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/PacStudent正面.png");
        foreach (var asset in pacSprites)
            if (asset is Sprite) { pacRenderer.sprite = (Sprite)asset; break; }
        pacRenderer.sortingOrder = 20;
        var movement = pac.GetComponent<PacStudentMovement>();
        if (movement == null) movement = pac.AddComponent<PacStudentMovement>();
        var movementData = new SerializedObject(movement);
        var route = movementData.FindProperty("clockwiseRoute");
        route.arraySize = 4;
        Vector3[] points = { new Vector3(-11.5f, 12.5f, 0f), new Vector3(-8.5f, 12.5f, 0f), new Vector3(-8.5f, 10.5f, 0f), new Vector3(-11.5f, 10.5f, 0f) };
        for (int i = 0; i < points.Length; i++) route.GetArrayElementAtIndex(i).vector3Value = points[i];
        movementData.ApplyModifiedPropertiesWithoutUndo();
        var audio = pac.GetComponent<AudioSource>();
        if (audio == null) audio = pac.AddComponent<AudioSource>();
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/PacStudent_move.wav");
        audio.loop = true;
        audio.playOnAwake = false;
        var animator = pac.GetComponent<Animator>();
        if (animator == null) animator = pac.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath + "PacStudentAnimator.controller");
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PacStudentMovement bound to PacStudent_Animated.");
    }

    public static void BindLevelGenerator()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity", OpenSceneMode.Single);
        var host = GameObject.Find("LevelGenerator");
        if (host == null) host = new GameObject("LevelGenerator");
        var generator = host.GetComponent<LevelGenerator>();
        if (generator == null) generator = host.AddComponent<LevelGenerator>();
        var so = new SerializedObject(generator);
        var manual = GameObject.Find("Level01_Manual");
        var parent = GameObject.Find("LevelRoot");
        so.FindProperty("manualLevel").objectReferenceValue = manual != null ? manual.transform : null;
        so.FindProperty("tileParent").objectReferenceValue = parent != null ? parent.transform : host.transform;
        so.FindProperty("pelletParent").objectReferenceValue = parent != null ? parent.transform : host.transform;
        so.FindProperty("levelCamera").objectReferenceValue = Camera.main;
        var sprites = so.FindProperty("tileSprites");
        string[] names = { "Wall_OutsideCorner.png", "Wall_Outside.png", "Wall_InsideCorner.png", "Wall_Inside.png", "Wall_TJunction.png", "Wall_GhostExit.png" };
        sprites.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/" + names[i]);
        so.FindProperty("pelletSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/颗粒.png");
        so.FindProperty("powerPelletController").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath + "PowerPelletAnimator.controller");
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("LevelGenerator bound to RecreatedLevel.");
    }

    static void AddManualTile(int value, int r, int c, bool mirrorX, bool mirrorY, int rows, int cols, Transform walls, Transform pellets)
    {
        float x = mirrorX ? (13.5f - c) : (-13.5f + c);
        float y = mirrorY ? (-14.5f + r) : (14.5f - r);
        int rotation = 0;
        if (value == 1 || value == 3)
            rotation = ((mirrorX ? 1 : 0) + (mirrorY ? 2 : 0)) * 90;
        else if (value == 2 || value == 4)
            rotation = (c == 0 || c == cols - 1) ? 90 : 0;
        else if (value == 7)
            rotation = mirrorX ? 90 : 0;
        else if (value == 8)
            rotation = mirrorX ? 180 : 0;

        if (value >= 1 && value <= 4 || value == 7 || value == 8)
        {
            string file = value == 1 ? "Wall_OutsideCorner.png" :
                          value == 2 ? "Wall_Outside.png" :
                          value == 3 ? "Wall_InsideCorner.png" :
                          value == 4 ? "Wall_Inside.png" :
                          value == 7 ? "Wall_TJunction.png" : "Wall_GhostExit.png";
            var go = AddSprite(walls, "Tile_" + value + "_" + r + "_" + c + "_" + mirrorX + "_" + mirrorY, new Vector3(x, y, 0), Sprite(file));
            go.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            return;
        }
        if (value == 5 || value == 6)
        {
            var go = AddSprite(pellets, value == 5 ? "StandardPellet" : "PowerPellet", new Vector3(x, y, -0.1f), Sprite("颗粒.png"), value == 5 ? 0.08f : 0.14f);
            return;
        }
    }
}
