using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Только в GameManager

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject gameOverPanel;
    public TMP_Text scoreText;
    int score;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1;
    }

    public void ShowGameOver()
    {
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = "Очки: " + score;
    }
}
