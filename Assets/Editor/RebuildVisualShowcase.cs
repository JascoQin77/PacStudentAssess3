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
}
