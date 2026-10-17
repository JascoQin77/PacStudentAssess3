using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Play Mode integration check for the Assessment 4 movement requirements.
public static class Assessment4MovementVerification
{
    static int frameCount;
    static float startedAt;
    static Vector3 initialPosition;
    static Vector3 positionAfterMove;
    static bool inputSent;
    static bool cherryWasActive;
    static bool requestedLevel;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity");
        frameCount = 0;
        startedAt = 0f;
        inputSent = false;
        cherryWasActive = false;
        requestedLevel = false;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) return;
            frameCount++;
            if (!requestedLevel && frameCount > 10)
            {
                var menu = UnityEngine.Object.FindFirstObjectByType<StartScreenController>();
                if (menu == null) throw new Exception("StartScene controller is missing in Play Mode.");
                requestedLevel = true;
                menu.LoadLevel1();
                return;
            }
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "RecreatedLevel") return;
            var pac = UnityEngine.Object.FindFirstObjectByType<PacStudentController>();
            var cherry = UnityEngine.Object.FindFirstObjectByType<CherryController>();
            if (pac == null || cherry == null) throw new Exception("Movement controllers are missing in RecreatedLevel Play Mode.");
            if (frameCount == 2)
            {
                initialPosition = pac.transform.position;
                if (initialPosition != new Vector3(-12.5f, 13.5f, 0f)) throw new Exception("PacStudent start position changed.");
                pac.SetInput(Vector2Int.right);
                inputSent = true;
                startedAt = Time.time;
            }
            if (inputSent && frameCount > 15 && positionAfterMove == Vector3.zero)
            {
                positionAfterMove = pac.transform.position;
                if (positionAfterMove == initialPosition) throw new Exception("PacStudent did not move with input.");
                var dust = pac.GetComponentInChildren<ParticleSystem>();
                if (dust == null || !dust.isPlaying) throw new Exception("Dust particle effect did not play while moving.");
            }
            if (Time.time - startedAt > 5.25f && !cherryWasActive)
            {
                cherryWasActive = cherry.IsCherryActive;
                if (!cherryWasActive) throw new Exception("Cherry did not spawn after the five-second delay.");
            }
            if (cherryWasActive && Time.time - startedAt > 6.25f)
            {
                if (cherry.CurrentCherryPosition == Vector3.zero) throw new Exception("Cherry position was not initialized.");
                Debug.Log("ASSESSMENT4_MOVEMENT_VERIFICATION_PASS: grid movement, animation/dust state, and five-second cherry spawning passed.");
                Finish(0);
            }
            if (Time.time - startedAt > 12f) throw new Exception("Movement verification timeout.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
        EditorApplication.delayCall += () => EditorApplication.Exit(code);
    }
}
