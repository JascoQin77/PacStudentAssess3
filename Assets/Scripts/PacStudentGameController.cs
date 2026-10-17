using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class PacStudentGameController : MonoBehaviour
{
    [SerializeField] PacStudentController player;
    [SerializeField] InGameHUDController hud;
    [SerializeField] GhostStateController[] ghosts;
    [SerializeField] ParticleSystem wallParticles;
    [SerializeField] ParticleSystem deathParticles;
    [SerializeField] AudioSource music;
    [SerializeField] AudioSource effects;
    [SerializeField] AudioClip introClip, normalClip, scaredClip, deadGhostClip;
    [SerializeField] AudioClip wallClip, pelletClip, cherryClip, ghostEatClip, deathClip;
    [SerializeField] AnimationClip deathAnimation;

    public enum RoundState { Countdown, Playing, Dying, GameOver }
    public RoundState State { get; private set; } = RoundState.Countdown;
    public bool IsPlaying => State == RoundState.Playing;
    public int Lives { get; private set; } = 3;
    public int RemainingPellets { get; private set; }
    public float ScaredRemaining { get; private set; }
    public int Score => hud.Score;

    void Awake()
    {
        RemainingPellets = 0;
        foreach (var item in FindObjectsByType<LevelCollectible>(FindObjectsSortMode.None))
            if (item.kind != LevelCollectible.CollectibleKind.Cherry) RemainingPellets++;
        Freeze(true);
    }

    IEnumerator Start()
    {
        hud.ResetRound();
        hud.SetLives(Lives);
        hud.SetButtonsEnabled(false);
        SetMusic(introClip, false);
        foreach (string label in new[] { "3", "2", "1", "GO!" })
        {
            hud.ShowBlockingMessage(label);
            yield return new WaitForSeconds(1f);
        }
        hud.HideBlockingMessage();
        State = RoundState.Playing;
        hud.SetButtonsEnabled(true);
        Freeze(false);
        UpdateMusic();
    }

    void Update()
    {
        if (!IsPlaying) return;
        hud.AdvanceTime(Time.deltaTime);
        ScaredRemaining = Mathf.Max(0, ScaredRemaining - Time.deltaTime);
        hud.SetScaredTime(ScaredRemaining);
        foreach (var ghost in ghosts) ghost.Tick(Time.deltaTime, ScaredRemaining);
        UpdateMusic();
    }

    public void Collect(LevelCollectible item)
    {
        if (!IsPlaying || item == null || !item.TryCollect()) return;
        switch (item.kind)
        {
            case LevelCollectible.CollectibleKind.Cherry:
                hud.AddScore(100);
                PlayEffect(cherryClip);
                break;
            case LevelCollectible.CollectibleKind.PowerPill:
                hud.AddScore(50);
                RemainingPellets--;
                ScaredRemaining = 10f;
                hud.SetScaredTime(ScaredRemaining);
                foreach (var ghost in ghosts) ghost.Tick(0, ScaredRemaining);
                PlayEffect(pelletClip);
                UpdateMusic();
                break;
            default:
                hud.AddScore(10);
                RemainingPellets--;
                PlayEffect(pelletClip);
                break;
        }
        if (RemainingPellets == 0) BeginGameOver();
    }

    public void TouchGhost(GhostStateController ghost)
    {
        if (!IsPlaying || ghost == null || ghost.State == GhostStateController.GhostState.Dead) return;
        if (ghost.State == GhostStateController.GhostState.Normal)
        {
            State = RoundState.Dying; // Reject any other trigger from the same physics frame.
            StartCoroutine(Die());
        }
        else
        {
            ghost.Eat();
            hud.AddScore(300);
            PlayEffect(ghostEatClip);
            UpdateMusic();
        }
    }

    public void BumpWall(Vector3 position)
    {
        if (!IsPlaying) return;
        Burst(wallParticles, position);
        PlayEffect(wallClip);
    }

    IEnumerator Die()
    {
        Lives--;
        hud.SetLives(Lives);
        Freeze(true);
        music.Stop();
        Burst(deathParticles, player.transform.position);
        float duration = Mathf.Max(deathAnimation != null ? deathAnimation.length : .4f,
            deathClip != null ? deathClip.length : 0);
        player.PlayDeath(deathAnimation != null ? deathAnimation.length / duration : 1f);
        PlayEffect(deathClip);
        yield return new WaitForSeconds(duration);
        if (Lives == 0)
        {
            BeginGameOver();
            yield break;
        }
        ScaredRemaining = 0;
        hud.ClearScaredTimer();
        player.ResetToStart();
        foreach (var ghost in ghosts) ghost.ResetGhost();
        State = RoundState.Playing;
        Freeze(false); // ResetToStart cleared input, so the player waits for WASD.
        UpdateMusic();
    }

    void BeginGameOver()
    {
        if (State == RoundState.GameOver) return;
        State = RoundState.GameOver;
        Freeze(true);
        music.Stop();
        hud.SetButtonsEnabled(false);
        hud.ShowBlockingMessage("Game Over");
        HighScoreStore.SaveIfBetter(hud.Score, hud.GameTime);
        StartCoroutine(ReturnToMenu());
    }

    IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("StartScene");
    }

    void Freeze(bool frozen)
    {
        player.SetControlEnabled(!frozen);
        foreach (var ghost in ghosts) ghost.SetFrozen(frozen);
    }

    void UpdateMusic()
    {
        foreach (var ghost in ghosts)
            if (ghost.State == GhostStateController.GhostState.Dead)
            {
                SetMusic(deadGhostClip, true);
                return;
            }
        SetMusic(ScaredRemaining > 0 ? scaredClip : normalClip, true);
    }

    void SetMusic(AudioClip clip, bool loop)
    {
        if (music.clip == clip && music.isPlaying) return;
        music.Stop();
        music.clip = clip;
        music.loop = loop;
        if (clip != null) music.Play();
    }

    void PlayEffect(AudioClip clip)
    {
        if (clip != null) effects.PlayOneShot(clip);
    }

    static void Burst(ParticleSystem particles, Vector3 position)
    {
        if (particles == null) return;
        particles.transform.position = position;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Play();
    }
}
