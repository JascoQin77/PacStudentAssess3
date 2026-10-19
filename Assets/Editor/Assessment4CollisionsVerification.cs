using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the real saved scene, including physics trigger callbacks. Restores the
// user's high score after exercising PlayerPrefs and both game-over conditions.
public static class Assessment4CollisionsVerification
{
    static int phase;
    static double deadline;
    static float at;
    static bool hadScore, hadTime;
    static int savedScore;
    static float savedTime;
    static Vector3 initial;
    static Vector3 cherryPosition;
    static int expectedScore;
    static GhostStateController targetGhost;
    static PacStudentGameController game;
    static PacStudentController player;
    static InGameHUDController hud;
    static bool previousPlayOptionsEnabled;
    static EnterPlayModeOptions previousPlayOptions;
    static bool capturedCountdown;
    static string output => Path.Combine("Logs", "assessment4-collisions-verification.txt");

    public static void Run()
    {
        previousPlayOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
        previousPlayOptions = EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        hadScore = PlayerPrefs.HasKey("Level1HighScore");
        hadTime = PlayerPrefs.HasKey("Level1BestTime");
        savedScore = PlayerPrefs.GetInt("Level1HighScore");
        savedTime = PlayerPrefs.GetFloat("Level1BestTime");
        PlayerPrefs.DeleteKey("Level1HighScore"); PlayerPrefs.DeleteKey("Level1BestTime");
        Require(HighScoreStore.SaveIfBetter(100, 20), "First score saves");
        Require(!HighScoreStore.SaveIfBetter(90, 1), "Lower score rejected");
        Require(!HighScoreStore.SaveIfBetter(100, 21), "Same score slower rejected");
        Require(HighScoreStore.SaveIfBetter(100, 19), "Same score faster saved");
        Require(HighScoreStore.SaveIfBetter(110, 30), "Higher score saved even with slower time");
        EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(output, "PlayerPrefs comparison PASS\n");
        phase = 0;
        capturedCountdown = false;
        deadline = EditorApplication.timeSinceStartup + 100;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Play Mode verification timed out at phase " + phase);
            if (!EditorApplication.isPlaying) return;
            if (phase == 0)
            {
                var menu = UnityEngine.Object.FindFirstObjectByType<StartScreenController>();
                if (menu == null) return;
                var labels = GameObject.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None);
                Require(labels.Any(t => t.text == "000110"), "StartScene loads high score");
                Require(labels.Any(t => t.text == "00:30:00"), "StartScene loads associated time");
                menu.LoadLevel1(); phase = 1; return;
            }
            if (phase == 1)
            {
                game = UnityEngine.Object.FindFirstObjectByType<PacStudentGameController>();
                if (game == null) return;
                player = UnityEngine.Object.FindFirstObjectByType<PacStudentController>();
                hud = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
                initial = player.transform.position;
                player.SetInput(Vector2Int.right);
                at = Time.time; phase = 2; return;
            }
            if (phase == 2)
            {
                if (!game.IsPlaying)
                {
                    if (!capturedCountdown && Time.time - at > .1f)
                    {
                        SavePreview("countdown");
                        capturedCountdown = true;
                    }
                    Require(hud.GameTime == 0 && hud.Score == 0, "Countdown freezes timer/score");
                    Require(player.transform.position == initial && !player.GetComponent<Collider2D>().enabled, "Countdown freezes control/collisions");
                    return;
                }
                Require(Time.time - at >= 3.8f, "3,2,1,GO lasts four seconds");
                Append("Countdown freeze and start PASS");
                SavePreview("playing");
                at = Time.time;
                player.SetInput(Vector2Int.up);
                phase = 18; return;
            }
            if (phase == 18)
            {
                if (Time.time - at < .12f) return;
                Require(player.transform.position == initial, "Wall blocks movement");
                var wall = new SerializedObject(game).FindProperty("wallParticles").objectReferenceValue as ParticleSystem;
                Require(wall != null && wall.isPlaying, "Wall bump particle emitted");
                Append("Wall blocking and bump particle PASS");
                player.SetInput(Vector2Int.right);
                at = Time.time;
                phase = 19; return;
            }
            if (phase == 19)
            {
                if (Time.time - at < .12f) return;
                Require(player.transform.position.x > initial.x, "Movement resumes after wall");
                expectedScore = hud.Score + 10;
                PlaceAt(Items(LevelCollectible.CollectibleKind.Pellet)[0].transform.position);
                phase = 3; return;
            }
            if (phase == 3)
            {
                if (hud.Score != expectedScore) return;
                Append("Pellet physics trigger +10 PASS");
                expectedScore += 50;
                PlaceAt(Items(LevelCollectible.CollectibleKind.PowerPill)[0].transform.position);
                phase = 4; return;
            }
            if (phase == 4)
            {
                if (hud.Score != expectedScore) return;
                Require(game.ScaredRemaining > 9, "Power pill starts ten-second timer");
                targetGhost = UnityEngine.Object.FindObjectsByType<GhostStateController>(FindObjectsSortMode.None)[0];
                Require(targetGhost.State == GhostStateController.GhostState.Scared, "Ghost scared state");
                PlaceAt(targetGhost.transform.position);
                expectedScore += 300;
                phase = 5; return;
            }
            if (phase == 5)
            {
                if (hud.Score != expectedScore) return;
                Require(targetGhost.State == GhostStateController.GhostState.Dead, "Ghost eaten enters Dead");
                PlaceAt(initial);
                at = Time.time; phase = 6; return;
            }
            if (phase == 6)
            {
                // The 85% AI requirement removes the old three-second timer:
                // an eaten ghost stays Dead until it reaches its own spawn.
                if (Time.time - at < 3.15f) return;
                Require(targetGhost.State == GhostStateController.GhostState.Dead,
                    "Dead ghost remains Dead until it reaches its spawn");
                Append("Power pill, ghost +300, and dead-state persistence PASS");
                // The death-reset path is covered later and is the supported
                // way to restore a ghost without relying on a timer.
                targetGhost.ResetGhost();
                // Tunnel trigger at either edge continues inward.
                PlaceAt(new Vector3(-14.4f, .5f, 0));
                phase = 9; return;
            }
            if (phase == 9)
            {
                if (player.transform.position.x < 0) return;
                Require(player.currentInput == Vector2Int.left, "Left tunnel resumes inward");
                PlaceAt(new Vector3(14.4f, -.5f, 0));
                phase = 10; return;
            }
            if (phase == 10)
            {
                if (player.transform.position.x > 0) return;
                Require(player.currentInput == Vector2Int.right, "Right tunnel resumes inward");
                Append("Both tunnel physics triggers PASS");
                player.ResetToStart();
                var cherry = UnityEngine.Object.FindFirstObjectByType<CherryController>();
                if (!cherry.IsCherryActive) return;
                cherryPosition = cherry.CurrentCherryPosition;
                PlaceAt(cherryPosition);
                expectedScore = hud.Score + 100;
                phase = 11; return;
            }
            if (phase == 11)
            {
                if (hud.Score != expectedScore) return;
                Append("Cherry physics trigger +100 PASS");
                PlaceAt(targetGhost.transform.position);
                phase = 12; return;
            }
            if (phase == 12)
            {
                if (game.State != PacStudentGameController.RoundState.Dying) return;
                Require(game.Lives == 2, "Normal ghost loses life");
                Require(!targetGhost.CanMove, "Ghost frozen during death");
                at = hud.GameTime;
                phase = 13; return;
            }
            if (phase == 13)
            {
                if (!game.IsPlaying)
                {
                    Require(hud.GameTime == at, "Death pauses timer");
                    return;
                }
                Require(player.transform.position == initial && !player.IsLerping, "Death resets start and waits for input");
                Require(targetGhost.State == GhostStateController.GhostState.Normal, "Death resets ghosts");
                Append("Death, life HUD, freeze, reset and wait-for-input PASS");
                game.TouchGhost(targetGhost);
                phase = 14; return;
            }
            if (phase == 14)
            {
                if (!game.IsPlaying) return;
                Require(game.Lives == 1, "Second death");
                game.TouchGhost(targetGhost);
                phase = 15; return;
            }
            if (phase == 15)
            {
                if (game.State != PacStudentGameController.RoundState.GameOver) return;
                Require(game.Lives == 0, "No lives game over");
                Require(!GameObject.Find("Exit Button").GetComponent<UnityEngine.UI.Button>().interactable, "Game over disables button");
                at = Time.time;
                SavePreview("game-over");
                Append("Zero lives Game Over and disabled UI PASS");
                phase = 16; return;
            }
            if (phase == 16)
            {
                if (SceneManager.GetActiveScene().name != "StartScene") return;
                Require(Time.time - at >= 2.8f, "Game Over remains for three seconds");
                Require(PlayerPrefs.GetInt("Level1HighScore") == expectedScore, "Game Over persists score");
                Append("Game Over delay, save and StartScene return PASS");
                UnityEngine.Object.FindFirstObjectByType<StartScreenController>().LoadLevel1();
                phase = 17; return;
            }
            if (phase == 17)
            {
                game = UnityEngine.Object.FindFirstObjectByType<PacStudentGameController>();
                if (game == null || !game.IsPlaying) return;
                var items = UnityEngine.Object.FindObjectsByType<LevelCollectible>(FindObjectsSortMode.None);
                foreach (var item in items) game.Collect(item);
                Require(game.RemainingPellets == 0 && game.State == PacStudentGameController.RoundState.GameOver, "All pellets game over");
                Append("All pellets Game Over PASS\nASSESSMENT4_COLLISIONS_RUNTIME_PASS");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Append("FAIL phase " + phase + ": " + error);
            Debug.LogException(error);
            Finish(1);
        }
    }

    static LevelCollectible[] Items(LevelCollectible.CollectibleKind kind) =>
        UnityEngine.Object.FindObjectsByType<LevelCollectible>(FindObjectsSortMode.None).Where(i => i.kind == kind).ToArray();

    static void PlaceAt(Vector3 position)
    {
        player.ResetToStart();
        player.transform.position = position;
        Physics2D.SyncTransforms();
    }

    static void Append(string text) { File.AppendAllText(output, text + "\n"); Debug.Log(text); }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    static void SavePreview(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera = Camera.main;
        var target = new RenderTexture(1600, 900, 24);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var screenshot = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            screenshot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(Path.Combine("Logs", "assessment4-" + name + ".png"), screenshot.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(screenshot);
        }
    }

    public static void BuildAndPreview()
    {
        BuildAssessment4Collisions.Run();
        var sceneHud = UnityEngine.Object.FindFirstObjectByType<InGameHUDController>();
        sceneHud.ResetRound();
        sceneHud.SetButtonsEnabled(false);
        sceneHud.ShowBlockingMessage("3");
        SavePreview("countdown");
        sceneHud.ShowBlockingMessage("Game Over");
        SavePreview("game-over");
        sceneHud.HideBlockingMessage();
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        if (hadScore) PlayerPrefs.SetInt("Level1HighScore", savedScore); else PlayerPrefs.DeleteKey("Level1HighScore");
        if (hadTime) PlayerPrefs.SetFloat("Level1BestTime", savedTime); else PlayerPrefs.DeleteKey("Level1BestTime");
        PlayerPrefs.Save();
        EditorSettings.enterPlayModeOptionsEnabled = previousPlayOptionsEnabled;
        EditorSettings.enterPlayModeOptions = previousPlayOptions;
        EditorApplication.ExitPlaymode();
        EditorApplication.Exit(code);
    }
}
