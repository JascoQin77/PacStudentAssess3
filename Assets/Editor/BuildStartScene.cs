using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildStartScene
{
    public const string ScenePath = "Assets/Scenes/StartScene.unity";
    static readonly Color Ink = Hex("080F20"), Panel = Hex("111F35"), Cream = Hex("F6F0DC");
    static readonly Color Cyan = Hex("64E4CE"), Peach = Hex("FFBA8C"), Muted = Hex("8EA7BD");
    static Font font;

    [MenuItem("Tools/PacStudent/Build Start Screen")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0, 0, -10);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ink;

        var audioObject = new GameObject("StartScene Music", typeof(AudioSource));
        var music = audioObject.GetComponent<AudioSource>();
        music.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/StartScene.wav");
        music.loop = true;
        music.playOnAwake = true;
        music.spatialBlend = 0;
        music.volume = 0.4f;
        var feedbackAudio = new GameObject("Menu Feedback Audio", typeof(AudioSource)).GetComponent<AudioSource>();
        feedbackAudio.playOnAwake = false;
        feedbackAudio.spatialBlend = 0;

        var canvasObject = new GameObject("StartScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)canvasObject.transform;
        Fill(root, "Background", V(0, 0), V(1, 1), Ink);
        Fill(root, "Right atmosphere", V(.58f, 0), V(1, 1), Hex("0C192C"));

        var border = Rect(root, "AnimatedBorder", V(0, 0), V(1, 1));
        border.offsetMin = V(26, 26);
        border.offsetMax = V(-26, -26);
        Outline(border, Hex("26465A"), 1);
        var packets = new RectTransform[16];
        for (int i = 0; i < packets.Length; i++)
        {
            var packet = Fill(border, "Moving packet " + i, V(.5f, .5f), V(.5f, .5f), i % 4 == 0 ? Peach : Cyan);
            packet.sizeDelta = V(i % 4 == 0 ? 10 : 6, i % 4 == 0 ? 10 : 6);
            packets[i] = packet;
        }
        var animation = border.gameObject.AddComponent<AnimatedMenuBorder>();
        animation.Configure(packets);

        var content = Rect(root, "SafeContent", V(.07f, .075f), V(.93f, .925f));
        Label(content, "Edition", "CAMPUS ARCADE   /   NIGHT SHIFT 01", V(0, .935f), V(.7f, 1), 19, Cyan, FontStyle.Bold);
        Label(content, "Edition right", "AFTER HOURS. ALL IN.", V(.72f, .935f), V(1, 1), 16, Muted, FontStyle.Normal, TextAnchor.MiddleRight);

        var title = Rect(content, "TitleBlock", V(0, .435f), V(.565f, .915f));
        Label(title, "Eyebrow", "ONE STUDENT. FOUR GHOSTS. ZERO CURFEW.", V(0, .84f), V(1, .98f), 18, Muted);
        Label(title, "PacStudent Title", "PacStudent", V(-.007f, .46f), V(1, .85f), 94, Cream, FontStyle.Bold);
        Label(title, "Creative Subtitle", "MIDNIGHT ON CAMPUS", V(0, .29f), V(1, .46f), 32, Peach, FontStyle.Bold);
        Fill(title, "Title underline", V(0, .22f), V(.13f, .23f), Cyan);
        Label(title, "Tagline", "Outsmart the night. Find your way home.", V(0, .015f), V(1, .19f), 24, Muted);

        var stage = Rect(content, "CharacterStage", V(.61f, .455f), V(1, .91f));
        Fill(stage, "Stage background", V(0, 0), V(1, 1), Panel);
        Outline(stage, Hex("294258"), 2);
        Label(stage, "Stage caption", "THE AFTER-HOURS CREW", V(.07f, .85f), V(.93f, .98f), 17, Cyan, FontStyle.Bold);
        // A small campus route, drawn entirely with Canvas images.
        Color route = Hex("29475C");
        Fill(stage, "Route upper", V(.09f, .77f), V(.91f, .777f), route);
        Fill(stage, "Route left", V(.085f, .40f), V(.092f, .77f), route);
        Fill(stage, "Route right", V(.905f, .40f), V(.912f, .77f), route);
        Fill(stage, "Route lower left", V(.09f, .40f), V(.29f, .407f), route);
        Fill(stage, "Route lower right", V(.71f, .40f), V(.91f, .407f), route);
        for (int i = 0; i < 7; i++)
        {
            var dot = Fill(stage, "Route marker " + i, V(.19f + i * .1f, .77f), V(.19f + i * .1f, .77f), i == 3 ? Peach : Cyan);
            dot.sizeDelta = V(5, 5);
        }
        SpriteImage(stage, "PacStudent Sprite", "PacStudent正面.png", V(.39f, .39f), V(.61f, .76f), Color.white);
        Label(stage, "Student label", "YOU", V(.33f, .32f), V(.67f, .41f), 15, Cream, FontStyle.Bold, TextAnchor.MiddleCenter);
        Color[] ghostColors = { new Color(1, .45f, .45f), new Color(1, .65f, .9f), new Color(.45f, .85f, 1), Color.white };
        for (int i = 0; i < 4; i++)
            SpriteImage(stage, "Ghost Sprite " + (i + 1), "Ghost_down.png", V(.11f + i * .205f, .08f), V(.275f + i * .205f, .28f), ghostColors[i]);

        Fill(content, "Level divider", V(0, .408f), V(1, .41f), Hex("294258"));
        Label(content, "Choose level", "CHOOSE YOUR ROUTE", V(0, .365f), V(.7f, .408f), 16, Muted, FontStyle.Bold);
        var controller = new GameObject("StartScreenController").AddComponent<StartScreenController>();
        Text[] scores = new Text[2], times = new Text[2];
        Button[] buttons = new Button[2];
        for (int i = 0; i < 2; i++)
        {
            var card = Rect(content, "Level " + (i + 1) + " Card", V(i == 0 ? 0 : .515f, .075f), V(i == 0 ? .485f : 1, .35f));
            Fill(card, "Card background", V(0, 0), V(1, 1), Panel);
            var stripe = Fill(card, "Accent stripe", V(0, 0), V(.009f, 1), i == 0 ? Cyan : Peach);
            Label(card, "Level name", "Level " + (i + 1), V(.045f, .76f), V(.72f, .97f), 30, Cream, FontStyle.Bold);
            Label(card, "Route description", i == 0 ? "THE ORIGINAL NIGHT RUN" : "THE NEXT CAMPUS ADVENTURE", V(.045f, .63f), V(.96f, .77f), 14, Muted);
            Label(card, "High score caption", "PREVIOUS HIGH SCORE", V(.045f, .48f), V(.52f, .62f), 12, Muted, FontStyle.Bold);
            Label(card, "Best time caption", "PREVIOUS BEST TIME", V(.56f, .48f), V(.96f, .62f), 12, Muted, FontStyle.Bold);
            scores[i] = Label(card, "High score", "000000", V(.045f, .30f), V(.52f, .49f), 24, Cream, FontStyle.Bold);
            times[i] = Label(card, "Best time", "00:00:00", V(.56f, .30f), V(.96f, .49f), 24, Cream, FontStyle.Bold);
            var buttonRect = Fill(card, "Level" + (i + 1) + "Button", V(.045f, .055f), V(.955f, .27f), i == 0 ? Cyan : Peach);
            buttonRect.GetComponent<Image>().raycastTarget = true;
            buttons[i] = buttonRect.gameObject.AddComponent<Button>();
            var colors = buttons[i].colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.17f, 1.17f, 1.17f);
            colors.selectedColor = new Color(1.10f, 1.10f, 1.10f);
            colors.pressedColor = new Color(.65f, .75f, .8f);
            colors.fadeDuration = .09f;
            colors.colorMultiplier = 1;
            buttons[i].colors = colors;
            Label(buttonRect, "Button label", i == 0 ? "ENTER LEVEL 1    >" : "EXPLORE LEVEL 2    >", V(.04f, 0), V(.96f, 1), 18, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonRect.gameObject.AddComponent<MenuButtonFeedback>().Configure(feedbackAudio,
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Eat pellets.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Eat bonus cherry.wav"));
        }
        var nav1 = buttons[0].navigation;
        nav1.mode = Navigation.Mode.Explicit;
        nav1.selectOnRight = buttons[1];
        nav1.selectOnDown = buttons[1];
        buttons[0].navigation = nav1;
        var nav2 = buttons[1].navigation;
        nav2.mode = Navigation.Mode.Explicit;
        nav2.selectOnLeft = buttons[0];
        nav2.selectOnUp = buttons[0];
        buttons[1].navigation = nav2;
        var status = Label(content, "MenuStatus", "MOUSE TO CHOOSE   /   ARROW KEYS + ENTER TO GO", V(0, 0), V(1, .055f), 14, Muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        string secondScene = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => System.IO.Path.GetFileNameWithoutExtension(p) == "DesignIteration");
        controller.Configure(buttons[0], status, scores, times, secondScene == null ? "" : "DesignIteration");
        UnityEventTools.AddPersistentListener(buttons[0].onClick, controller.LoadLevel1);
        UnityEventTools.AddPersistentListener(buttons[1].onClick, controller.LoadLevel2);
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Canvas.ForceUpdateCanvases();
        animation.Animate(0);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene("Assets/Scenes/RecreatedLevel.unity", true) }.ToList();
        if (secondScene != null) scenes.Add(new EditorBuildSettingsScene(secondScene, true));
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => scenes.All(current => current.path != s.path)));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("StartScene created: responsive Canvas, animated border, existing character sprites, level buttons and records, looping StartScene music.");
    }

    static Color Hex(string code)
    {
        ColorUtility.TryParseHtmlString("#" + code, out Color color);
        return color;
    }
    static Vector2 V(float x, float y) => new Vector2(x, y);
    static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    static RectTransform Fill(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var rect = Rect(parent, name, min, max);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
    static Text Label(Transform parent, string name, string wording, Vector2 min, Vector2 max, int size, Color color,
        FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Rect(parent, name, min, max);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = wording;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.FloorToInt(size * .65f);
        text.resizeTextMaxSize = size;
        return text;
    }
    static void SpriteImage(Transform parent, string name, string asset, Vector2 min, Vector2 max, Color tint)
    {
        var rect = Fill(parent, name, min, max, tint);
        var image = rect.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/" + asset).OfType<Sprite>().OrderBy(s => s.rect.x).First();
        image.preserveAspect = true;
    }
    static void Outline(Transform parent, Color color, float thickness)
    {
        var top = Fill(parent, "Frame top", V(0, 1), V(1, 1), color);
        top.sizeDelta = V(0, thickness);
        var bottom = Fill(parent, "Frame bottom", V(0, 0), V(1, 0), color);
        bottom.sizeDelta = V(0, thickness);
        var left = Fill(parent, "Frame left", V(0, 0), V(0, 1), color);
        left.sizeDelta = V(thickness, 0);
        var right = Fill(parent, "Frame right", V(1, 0), V(1, 1), color);
        right.sizeDelta = V(thickness, 0);
    }
}
