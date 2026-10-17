using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] string level1Scene = "RecreatedLevel";
    [SerializeField] string level2Scene = "";
    [SerializeField] Button level1Button;
    [SerializeField] Text statusText;
    [SerializeField] Text[] highScoreLabels;
    [SerializeField] Text[] bestTimeLabels;
    bool loading;

    public void Configure(Button firstButton, Text status, Text[] scores, Text[] times, string secondScene)
    {
        level1Button = firstButton;
        statusText = status;
        highScoreLabels = scores;
        bestTimeLabels = times;
        level2Scene = secondScene;
    }

    void Start()
    {
        for (int i = 0; i < highScoreLabels.Length; i++)
        {
            highScoreLabels[i].text = PlayerPrefs.GetInt("Level" + (i + 1) + "HighScore", 0).ToString("D6");
            double seconds = Mathf.Max(0, PlayerPrefs.GetFloat("Level" + (i + 1) + "BestTime", 0));
            bestTimeLabels[i].text = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
        }
        if (EventSystem.current != null && level1Button != null)
            EventSystem.current.SetSelectedGameObject(level1Button.gameObject);
    }

    public void LoadLevel1() => LoadLevel(level1Scene);

    public void LoadLevel2()
    {
        if (string.IsNullOrEmpty(level2Scene))
        {
            statusText.text = "A NEW ROUTE IS COMING SOON. KEEP YOUR SNEAKERS READY.";
            return;
        }
        LoadLevel(level2Scene);
    }

    void LoadLevel(string sceneName)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            statusText.text = "THIS ROUTE IS UNAVAILABLE. PLEASE CHOOSE ANOTHER LEVEL.";
            Debug.LogError("Scene is not enabled in Build Settings: " + sceneName);
            return;
        }
        loading = true;
        SceneManager.LoadScene(sceneName);
    }
}
