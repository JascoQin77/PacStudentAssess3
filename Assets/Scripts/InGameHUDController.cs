using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameHUDController : MonoBehaviour
{
    [SerializeField] Text scoreText;
    [SerializeField] Text gameTimerText;
    [SerializeField] Text scaredTimerText;
    [SerializeField] Text levelNameText;
    [SerializeField] Image[] lifeIcons;
    [SerializeField] string startScene = "StartScene";
    float elapsed;
    float scaredRemaining;
    int score;

    public int Score => score;
    public float GameTime => elapsed;
    public float ScaredTimeRemaining => scaredRemaining;

    public void Configure(Text scoreLabel, Text timerLabel, Text scaredLabel, Text levelLabel,
        Image[] icons, string startSceneName)
    {
        scoreText = scoreLabel;
        gameTimerText = timerLabel;
        scaredTimerText = scaredLabel;
        levelNameText = levelLabel;
        lifeIcons = icons;
        startScene = string.IsNullOrEmpty(startSceneName) ? "StartScene" : startSceneName;
    }

    void Start()
    {
        score = 0;
        elapsed = 0;
        scaredRemaining = 0;
        if (levelNameText != null) levelNameText.text = "LEVEL 1";
        SetLives(3);
        Refresh();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (scaredRemaining > 0) scaredRemaining = Mathf.Max(0, scaredRemaining - Time.deltaTime);
        Refresh();
    }

    public void SetScore(int value)
    {
        score = Mathf.Max(0, value);
        Refresh();
    }

    public void AddScore(int value) => SetScore(score + value);

    public void BeginScaredTimer(float seconds)
    {
        scaredRemaining = Mathf.Max(0, seconds);
        Refresh();
    }

    public void ClearScaredTimer()
    {
        scaredRemaining = 0;
        Refresh();
    }

    public void SetLives(int count)
    {
        if (lifeIcons == null) return;
        count = Mathf.Clamp(count, 0, lifeIcons.Length);
        for (int i = 0; i < lifeIcons.Length; i++)
            if (lifeIcons[i] != null) lifeIcons[i].gameObject.SetActive(i < count);
    }

    public void ExitToStartScene()
    {
        if (Application.CanStreamedLevelBeLoaded(startScene)) SceneManager.LoadScene(startScene);
        else Debug.LogError("Start scene is not enabled in Build Settings: " + startScene);
    }

    void Refresh()
    {
        if (scoreText != null) scoreText.text = score.ToString("D6");
        if (gameTimerText != null) gameTimerText.text = FormatTime(elapsed);
        if (scaredTimerText != null)
        {
            scaredTimerText.gameObject.SetActive(scaredRemaining > 0);
            scaredTimerText.text = Mathf.CeilToInt(scaredRemaining).ToString();
        }
    }

    static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds) % 60;
        int hundredths = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, wholeSeconds, hundredths);
    }
}
