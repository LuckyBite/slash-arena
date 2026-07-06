using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Только в GameManager

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;
    public TMP_Text scoreText;
    [Tooltip("Слот: надпись «Волна N». Пусто = создадим сами сверху по центру")]
    public TMP_Text waveText;
    [Tooltip("Слот: итоги забега на панели game over. Пусто = создадим сами внутри панели")]
    public TMP_Text gameOverStatsText;

    [Header("Config")]
    [Tooltip("Глобальный конфиг (очки за убийство и т. д.)")]
    [SerializeField] private GameConfig config;

    [Header("Debug")]
    [Tooltip("Логировать каждое начисление очков (диагностика источника очков)")]
    [SerializeField] private bool logScore = true;

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
        EnsureHud();
        if (scoreText != null) scoreText.text = "Очки: 0";
    }

    // Достраиваем недостающие элементы HUD кодом, чтобы не зависеть от ручной вёрстки
    private void EnsureHud()
    {
        Canvas canvas = scoreText ? scoreText.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (waveText == null)
        {
            waveText = CreateHudText(canvas.transform, "WaveText (auto)", 34);
            var rt = waveText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -16);
            rt.sizeDelta = new Vector2(500, 46);
            waveText.alignment = TextAlignmentOptions.Center;
            waveText.text = "";
        }

        if (gameOverStatsText == null && gameOverPanel != null)
        {
            gameOverStatsText = CreateHudText(gameOverPanel.transform, "GameOverStats (auto)", 30);
            var rt = gameOverStatsText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -70);
            rt.sizeDelta = new Vector2(640, 90);
            gameOverStatsText.alignment = TextAlignmentOptions.Center;
            gameOverStatsText.text = "";
        }
    }

    private static TMP_Text CreateHudText(Transform parent, string name, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        return tmp;
    }

    public void ShowGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0;
        CursorLocker.UnlockCursor(); // иначе по панели нельзя кликнуть
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        // Рекорды: лучший счёт и лучшая волна — в сейв
        var save = SaveService.Data;
        bool newRecord = score > save.bestScore;
        if (newRecord) save.bestScore = score;
        if (WaveReached > save.bestWave) save.bestWave = WaveReached;
        SaveService.Save();

        if (gameOverStatsText != null)
        {
            gameOverStatsText.text =
                $"Очки: {score}   Волна: {WaveReached}\n" +
                (newRecord ? "НОВЫЙ РЕКОРД!" : $"Рекорд: {save.bestScore}");
        }
    }

    public void Restart()
    {
        CursorLocker.playerIsAlive = true; // static переживает перезагрузку сцены
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Кнопка «В меню» на панели game over (привяжи onClick в Editor)
    public void BackToMenu()
    {
        CursorLocker.playerIsAlive = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Вызывается EnemyHealth при смерти врага
    public void RegisterKill()
    {
        if (logScore) Debug.Log("[Score] +за убийство врага");
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
        if (logScore) Debug.Log($"[Score] +{amount} → {score} (источник: {new System.Diagnostics.StackTrace(1, false).GetFrame(0)?.GetMethod()?.Name})");
        if (scoreText != null) scoreText.text = "Очки: " + score;
    }
}
