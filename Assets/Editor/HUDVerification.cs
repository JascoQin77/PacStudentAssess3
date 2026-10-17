using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Batch-mode integration check. Uses the graphic raycaster before dispatching EXIT.
[InitializeOnLoad]
public static class HUDVerification
{
    const string Key = "PacStudent.HUDVerification";
    static int frames;
    static double deadline;
    static HUDVerification()
    {
        if (SessionState.GetBool(Key, false))
        {
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity");
        Require(GameObject.Find("HUD").GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceCamera, "HUD screen space");
        Require(GameObject.Find("Score").GetComponent<Text>().text == "000000", "saved initial score");
        Require(GameObject.Find("Game Timer").GetComponent<Text>().text == "00:00:00", "saved initial timer");
        var scared = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(t => t.name == "Ghost Scared Timer");
        Require(!scared.gameObject.activeSelf, "saved scared timer hidden");
        SessionState.SetBool(Key, true);
        SessionState.SetInt(Key + ".phase", 0);
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play-mode verification timeout");
            if (!EditorApplication.isPlaying || ++frames < 15) return;
            int phase = SessionState.GetInt(Key + ".phase", 0);
            if (phase == 0)
            {
                var hud = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
                Require(hud != null && hud.Score == 0, "runtime starts at zero");
                hud.SetScore(1234);
                Require(GameObject.Find("Score").GetComponent<Text>().text == "001234", "six-digit score padding");
                var format = typeof(InGameHUDController).GetMethod("FormatTime", BindingFlags.Static | BindingFlags.NonPublic);
                Require((string)format.Invoke(null, new object[] { 92.25f }) == "01:32:25", "timer hundredths formatting");
                hud.SetLives(2);
                Require(GameObject.Find("Lives").GetComponentsInChildren<Image>().Length == 2, "life count can change");
                hud.SetLives(3);
                hud.SetScore(0);
                typeof(InGameHUDController).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, 0f);
                typeof(InGameHUDController).GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                RenderAndCheck(1600, 900, "Logs/hud-16x9.png");
                RenderAndCheck(1200, 900, "Logs/hud-4x3.png");
                hud.BeginScaredTimer(.2f);
                var scared = GameObject.Find("Ghost Scared Timer");
                Require(scared != null && scared.GetComponent<Text>().text == "1", "scared timer shows integer seconds");
                SessionState.SetInt(Key + ".phase", 1);
                frames = 0;
            }
            else if (phase == 1)
            {
                var hud = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
                if (hud.ScaredTimeRemaining > 0) return;
                Require(GameObject.Find("Ghost Scared Timer") == null, "scared timer hidden at zero");
                Require(hud.GameTime > 0, "game timer advances");
                Canvas.ForceUpdateCanvases();
                var exit = GameObject.Find("Exit Button");
                var canvas = exit.GetComponentInParent<Canvas>();
                var position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, exit.transform.position);
                var data = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, hits);
                Require(hits.Count > 0 && hits[0].gameObject == exit, "EXIT is the first actual mouse raycast hit");
                SessionState.SetInt(Key + ".phase", 2);
                frames = 0;
                ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
            }
            else
            {
                Require(SceneManager.GetActiveScene().name == "StartScene", "EXIT loads StartScene");
                Debug.Log("HUD_VERIFICATION_PASS: saved defaults, separated full-heart lives, both aspect ratios, maze visibility, ghost labels, timer/score/scared behavior, raycast-driven EXIT.");
                Finish(0);
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }

    static void RenderAndCheck(int width, int height, string path)
    {
        var camera = Camera.main;
        var canvas = GameObject.Find("HUD").GetComponent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>();
        var target = new RenderTexture(width, height, 24);
        target.Create();
        camera.targetTexture = target;
        scaler.enabled = false;
        // Same logarithmic match as CanvasScaler, using the target's dimensions.
        canvas.scaleFactor = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(width / 1600f, 2), Mathf.Log(height / 900f, 2), .5f));
        foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) graphic.SetAllDirty();
        Canvas.ForceUpdateCanvases();
        var hearts = GameObject.Find("Lives").GetComponentsInChildren<Image>();
        Require(hearts.Length == 3, "three visible lives at " + width);
        for (int i = 0; i < hearts.Length; i++)
        {
            Require(hearts[i].sprite.rect.width == 36 && hearts[i].sprite.rect.height == 32, "single full-heart frame");
            var bounds = ScreenBounds(hearts[i].rectTransform, camera);
            Require(bounds.width > 25 && bounds.height > 25, "life icon readable size");
            if (i > 0) Require(ScreenBounds(hearts[i - 1].rectTransform, camera).xMax < bounds.xMin, "life icons do not overlap");
        }
        var maze = GameObject.Find("Level01_Manual").GetComponentsInChildren<SpriteRenderer>();
        Require(maze.Length > 100, "manual maze exists");
        foreach (var sprite in maze)
        {
            var min = camera.WorldToViewportPoint(sprite.bounds.min);
            var max = camera.WorldToViewportPoint(sprite.bounds.max);
            Require(min.x >= 0 && min.y >= 0 && max.x <= 1 && max.y < .84f, "maze fits below HUD: " + sprite.name);
        }
        string[] ghosts = { "GhostRed", "GhostPink", "GhostBlue", "GhostOrange" };
        for (int i = 0; i < 4; i++)
        {
            var child = GameObject.Find(ghosts[i]).transform.Find("Ghost" + (i + 1) + "Canvas");
            Require(child != null && child.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "ghost child world canvas");
            var texts = child.GetComponentsInChildren<Text>();
            Require(texts.Length == 1 && texts[0].text == (i + 1).ToString(), "single correct ghost number");
            var bounds = ScreenBounds(texts[0].rectTransform, camera);
            Require(bounds.height > 15 && bounds.width > 15, "ghost number readable bounds");
            Require(child.position.y > child.parent.position.y + .5f, "ghost label above ghost");
        }
        camera.Render();
        RenderTexture.active = target;
        var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
        capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        capture.Apply();
        File.WriteAllBytes(path, capture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(capture);
        RenderTexture.active = null;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        scaler.enabled = true;
        Canvas.ForceUpdateCanvases();
    }

    static Rect ScreenBounds(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var a = camera.WorldToScreenPoint(corners[0]);
        var b = camera.WorldToScreenPoint(corners[2]);
        return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception("HUD check failed: " + message); }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
