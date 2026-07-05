using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Только в GameManager

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TMP_Text scoreText;

    [Header("Config")]
    [Tooltip("Глобальный конфиг (очки за убийство и т. д.)")]
    [SerializeField] private GameConfig config;

    public bool IsGameOver { get; private set; }

    int score;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1;
    }

    void Start()
    {
        if (scoreText != null) scoreText.text = "Очки: 0";
    }

    public void ShowGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0;
        CursorLocker.UnlockCursor(); // иначе по панели нельзя кликнуть
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void Restart()
    {
        CursorLocker.playerIsAlive = true; // static переживает перезагрузку сцены
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Вызывается EnemyHealth при смерти врага
    public void RegisterKill()
    {
        AddScore(config != null ? config.scorePerKill : 10);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null) scoreText.text = "Очки: " + score;
    }
}
