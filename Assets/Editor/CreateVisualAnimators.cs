using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public static class CreateVisualAnimators
{
    const string Anim = "Assets/Animation/";
    const string Out = "Assets/Animation/Controllers/";

    static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip, Vector2 pos)
    {
        var s = sm.AddState(name, pos);
        s.motion = clip;
        s.writeDefaultValues = true;
        return s;
    }

    static void LoopTransition(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 3f;
        t.duration = 0.05f;
        t.hasFixedDuration = true;
    }

    static void DeathTransition(AnimatorState from, AnimatorState dead)
    {
        var t = from.AddTransition(dead);
        t.hasExitTime = true;
        t.exitTime = 3f;
        t.duration = 0.05f;
        t.hasFixedDuration = true;
    }

    static AnimatorController NewController(string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }

    static void MakePacStudent()
    {
        var c = NewController(Out + "PacStudentAnimator.controller");
        var sm = c.layers[0].stateMachine;
        var up = AddState(sm, "Walking_Up", Clip("PacStudent_Walk_Up"), new Vector2(0, 80));
        var right = AddState(sm, "Walking_Right", Clip("PacStudent_Walk_Right"), new Vector2(220, 80));
        var down = AddState(sm, "Walking_Down", Clip("PacStudent_Walk_Down"), new Vector2(440, 80));
        var left = AddState(sm, "Walking_Left", Clip("PacStudent_Walk_Left"), new Vector2(660, 80));
        var dead = AddState(sm, "Dead", Clip("PacStudent_Dead"), new Vector2(440, -100));
        sm.defaultState = up;
        LoopTransition(up, right); LoopTransition(right, down); LoopTransition(down, left); LoopTransition(left, up);
        DeathTransition(left, dead);
        var back = dead.AddTransition(up); back.hasExitTime = true; back.exitTime = 1f; back.duration = 0.05f; back.hasFixedDuration = true;
        EditorUtility.SetDirty(c);
    }

    static void MakePellet()
    {
        var c = NewController(Out + "PowerPelletAnimator.controller");
        var sm = c.layers[0].stateMachine;
        var flash = AddState(sm, "Flashing", Clip("Pellet_Flash"), new Vector2(300, 80));
        sm.defaultState = flash;
        var loop = flash.AddTransition(flash); loop.hasExitTime = true; loop.exitTime = 3f; loop.duration = 0.05f; loop.hasFixedDuration = true;
        EditorUtility.SetDirty(c);
    }

    static void MakeGhost()
    {
        var c = NewController(Out + "GhostAnimator_Base.controller");
        var sm = c.layers[0].stateMachine;
        var nU = AddState(sm, "Walking_Up", Clip("Ghost_Normal_up"), new Vector2(0, 180));
        var nR = AddState(sm, "Walking_Right", Clip("Ghost_Normal_right"), new Vector2(220, 180));
        var nD = AddState(sm, "Walking_Down", Clip("Ghost_Normal_down"), new Vector2(440, 180));
        var nL = AddState(sm, "Walking_Left", Clip("Ghost_Normal_left"), new Vector2(660, 180));
        var sU = AddState(sm, "Scared_Up", Clip("Ghost_Scared_up"), new Vector2(0, 0));
        var sR = AddState(sm, "Scared_Right", Clip("Ghost_Scared_right"), new Vector2(220, 0));
        var sD = AddState(sm, "Scared_Down", Clip("Ghost_Scared_down"), new Vector2(440, 0));
        var sL = AddState(sm, "Scared_Left", Clip("Ghost_Scared_left"), new Vector2(660, 0));
        var rec = AddState(sm, "Recovering", Clip("Ghost_Recovering"), new Vector2(220, -180));
        var dead = AddState(sm, "Dead", Clip("Ghost_Dead"), new Vector2(660, -180));
        sm.defaultState = nU;
        LoopTransition(nU, nR); LoopTransition(nR, nD); LoopTransition(nD, nL); LoopTransition(nL, nU);
        LoopTransition(sU, sR); LoopTransition(sR, sD); LoopTransition(sD, sL); LoopTransition(sL, sU);
        LoopTransition(rec, nU);
        DeathTransition(nL, dead); DeathTransition(sL, rec);
        var back = dead.AddTransition(nU); back.hasExitTime = true; back.exitTime = 1f; back.duration = 0.05f; back.hasFixedDuration = true;
        EditorUtility.SetDirty(c);
        foreach (string n in new[] { "Pink", "Red", "Orange" })
        {
            string path = Out + "GhostAnimator_" + n + ".overrideController";
            var old = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            var ov = new AnimatorOverrideController(c);
            AssetDatabase.CreateAsset(ov, path);
        }
    }

    static void Bind(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var pac = GameObject.Find("PacStudent_Animated");
        if (pac != null) pac.GetOrAddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Out + "PacStudentAnimator.controller");
        var pellet = GameObject.Find("PowerPellet");
        if (pellet != null) pellet.GetOrAddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Out + "PowerPelletAnimator.controller");
        var normal = GameObject.Find("Ghost_Normal_Frame1");
        if (normal != null) normal.GetOrAddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Out + "GhostAnimator_Base.controller");
        var scared = GameObject.Find("Ghost_Scared");
        if (scared != null) scared.GetOrAddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Out + "GhostAnimator_Pink.overrideController");
        var dead = GameObject.Find("Ghost_Dead");
        if (dead != null) dead.GetOrAddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Out + "GhostAnimator_Red.overrideController");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Run()
    {
        Directory.CreateDirectory(Application.dataPath + "/Animation/Controllers");
        MakePacStudent(); MakePellet(); MakeGhost();
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Bind("Assets/Scenes/VisualAssets_Showcase.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Visual animator controllers created and bound.");
    }

    public static void Validate()
    {
        var lines = new System.Collections.Generic.List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animation" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            lines.Add(Path.GetFileName(path) + " length=" + clip.length + " spriteBindings=" + bindings.Length);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:AnimatorController", new[] { Out }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var c = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            lines.Add(Path.GetFileName(path) + " states=" + c.layers[0].stateMachine.states.Length);
        }
        File.WriteAllLines("C:/Users/Lenovo/Documents/New project/animator_validation.txt", lines.ToArray());
    }
}

static class GameObjectAnimatorExtensions
{
    public static Animator GetOrAddComponent<Animator>(this GameObject go) where Animator : Component
    {
        var c = go.GetComponent<Animator>();
        return c != null ? c : go.AddComponent<Animator>();
    }
}
