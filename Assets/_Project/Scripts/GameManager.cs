using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Только в GameManager

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TMP_Text scoreText;
    [Tooltip("Слот: надпись «Волна N» (создай TMP-текст на Canvas и привяжи)")]
    public TMP_Text waveText;

    [Header("Config")]
    [Tooltip("Глобальный конфиг (очки за убийство и т. д.)")]
    [SerializeField] private GameConfig config;

    public bool IsGameOver { get; private set; }

    /// <summary>Последняя достигнутая волна (для итогов и рекорда).</summary>
    public int WaveReached { get; private set; }

    /// <summary>Текущий счёт (для итогов и рекорда).</summary>
    public int Score => score;

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

    // ===== Волны (вызывает WaveManager) =====
    public void ReportWaveReached(int wave)
    {
        WaveReached = Mathf.Max(WaveReached, wave);
    }

    public void SetWaveLabel(string text)
    {
        if (waveText != null) waveText.text = text;
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null) scoreText.text = "Очки: " + score;
    }
}
