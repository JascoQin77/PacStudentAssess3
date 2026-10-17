using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildInGameHUD
{
    const string ScenePath = "Assets/Scenes/RecreatedLevel.unity";
    static readonly Color Navy = Hex("071222");
    static readonly Color Cyan = Hex("64E4CE");
    static readonly Color Peach = Hex("FFBA8C");
    static readonly Color Cream = Hex("F6F0DC");
    static readonly Color Muted = Hex("9AB0C1");
    static Font font;

    [MenuItem("Tools/PacStudent/Build In-Game HUD")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before building the HUD.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        foreach (var old in GameObject.FindObjectsByType<InGameHUDController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(old.transform.root.gameObject);
        var oldHud = GameObject.Find("HUD");
        if (oldHud != null) UnityEngine.Object.DestroyImmediate(oldHud);
        foreach (var old in GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (old.name.StartsWith("Ghost") && old.name.EndsWith("Canvas")) UnityEngine.Object.DestroyImmediate(old.gameObject);

        var camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Main Camera is missing.");
        camera.transform.position = new Vector3(0, 3, -10);
        camera.orthographicSize = 19;
        var hud = CreateHUD(camera);
        CreateGhostCanvases(camera);
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .OrderBy(s => s.path == "Assets/Scenes/StartScene.unity" ? 0 : s.path == ScenePath ? 1 : 2).ToArray();
        Debug.Log("In-game HUD built: lives, score, game timer, scared timer, exit button, level name, and four ghost world-space labels.");
    }

    static InGameHUDController CreateHUD(Camera camera)
    {
        var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 2;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)canvasObject.transform;

        var bar = Fill(root, "HUD Bar", V(0, .91f), V(1, 1), Navy);
        Outline(bar, Hex("294258"), 2);
        Label(bar, "Lives caption", "LIVES", V(.025f, .18f), V(.12f, .82f), 17, Muted, FontStyle.Bold);
        var lives = Rect(bar, "Lives", V(.12f, .18f), V(.30f, .82f));
        var lifeIcons = CreateLifeIcons(lives);
        Label(bar, "Score caption", "SCORE", V(.36f, .18f), V(.45f, .82f), 17, Muted, FontStyle.Bold);
        var score = Label(bar, "Score", "000000", V(.45f, .08f), V(.58f, .9f), 28, Cream, FontStyle.Bold);
        Label(bar, "Timer caption", "TIME", V(.62f, .18f), V(.68f, .82f), 17, Muted, FontStyle.Bold);
        var gameTimer = Label(bar, "Game Timer", "00:00:00", V(.68f, .08f), V(.79f, .9f), 28, Cream, FontStyle.Bold);
        Label(bar, "Scared caption", "SCARED", V(.80f, .18f), V(.88f, .82f), 17, Muted, FontStyle.Bold);
        var scared = Label(bar, "Ghost Scared Timer", "0", V(.88f, .08f), V(.915f, .9f), 28, Peach, FontStyle.Bold, TextAnchor.MiddleCenter);
        var exit = Fill(bar, "Exit Button", V(.925f, .17f), V(.985f, .83f), Peach);
        var exitButton = exit.gameObject.AddComponent<Button>();
        exit.GetComponent<Image>().raycastTarget = true;
        exitButton.targetGraphic = exit.GetComponent<Image>();
        var colors = exitButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(.65f, .75f, .8f);
        colors.selectedColor = colors.highlightedColor;
        exitButton.colors = colors;
        Label(exit, "Exit Label", "EXIT", V(0, 0), V(1, 1), 17, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
        Label(root, "Level Name", "LEVEL 1", V(.38f, .84f), V(.62f, .91f), 22, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);

        var controllerObject = new GameObject("HUD Controller");
        var controller = controllerObject.AddComponent<InGameHUDController>();
        controller.Configure(score, gameTimer, scared, root.Find("Level Name").GetComponent<Text>(), lifeIcons, "StartScene");
        UnityEventTools.AddPersistentListener(exitButton.onClick, controller.ExitToStartScene);
        scared.gameObject.SetActive(false);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static Image[] CreateLifeIcons(RectTransform parent)
    {
        const string path = "Assets/Sprites/life indicator full.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        var sprite = LoadSprite("life indicator full.png");
        var icons = new Image[3];
        for (int i = 0; i < icons.Length; i++)
        {
            var rect = Rect(parent, "Life " + (i + 1), V(0, .5f), V(0, .5f));
            rect.pivot = V(0, .5f);
            rect.sizeDelta = V(42, 38);
            rect.anchoredPosition = V(i * 54, 0);
            var icon = rect.gameObject.AddComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icons[i] = icon;
        }
        return icons;
    }

    static void CreateGhostCanvases(Camera camera)
    {
        string[] ghostNames = { "GhostRed", "GhostPink", "GhostBlue", "GhostOrange" };
        for (int i = 0; i < ghostNames.Length; i++)
        {
            var ghost = GameObject.Find(ghostNames[i]);
            if (ghost == null) continue;
            var canvasObject = new GameObject("Ghost" + (i + 1) + "Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(ghost.transform, false);
            canvasObject.transform.localPosition = new Vector3(0, 1.15f, 0);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.sortingOrder = 110;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(90, 80);
            rect.localScale = Vector3.one * .01f;
            var label = Label(rect, "Ghost " + (i + 1) + " Label", (i + 1).ToString(), V(0, 0), V(1, 1), 70, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static Sprite LoadSprite(string file) => AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + file).OfType<Sprite>().First();
    static Color Hex(string code) { ColorUtility.TryParseHtmlString("#" + code, out Color result); return result; }
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    static RectTransform Fill(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var rect = Rect(parent, name, min, max); var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
    }
    static Text Label(Transform parent, string name, string wording, Vector2 min, Vector2 max, int size, Color color,
        FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Rect(parent, name, min, max); var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = wording;
        text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = alignment; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate; text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.FloorToInt(size * .65f); text.resizeTextMaxSize = size; return text;
    }
    static void Outline(Transform parent, Color color, float thickness)
    {
        var top = Fill(parent, "Frame top", V(0, 1), V(1, 1), color); top.sizeDelta = V(0, thickness);
        var bottom = Fill(parent, "Frame bottom", V(0, 0), V(1, 0), color); bottom.sizeDelta = V(0, thickness);
        var left = Fill(parent, "Frame left", V(0, 0), V(0, 1), color); left.sizeDelta = V(thickness, 0);
        var right = Fill(parent, "Frame right", V(1, 0), V(1, 1), color); right.sizeDelta = V(thickness, 0);
    }
}
