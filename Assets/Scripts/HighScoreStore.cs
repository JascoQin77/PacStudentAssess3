using UnityEngine;

public static class HighScoreStore
{
    public static bool SaveIfBetter(int score, float seconds)
    {
        int previous = PlayerPrefs.GetInt("Level1HighScore", 0);
        float previousTime = PlayerPrefs.GetFloat("Level1BestTime", float.MaxValue);
        if (score < previous || (score == previous && seconds >= previousTime)) return false;
        PlayerPrefs.SetInt("Level1HighScore", score);
        PlayerPrefs.SetFloat("Level1BestTime", seconds);
        PlayerPrefs.Save();
        return true;
    }
}
