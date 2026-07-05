using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Используется, если GameConfig не назначен")]
    public int maxHealth = 100;
    public Slider healthSlider;

    [Header("Config")]
    [Tooltip("Глобальный конфиг: берём из него стартовое здоровье")]
    [SerializeField] private GameConfig config;

    [Header("Block")]
    [Tooltip("Какая доля урона поглощается блоком (0.7 = минус 70% урона)")]
    [SerializeField, Range(0f, 1f)] private float blockDamageAbsorb = 0.7f;

    private int currentHealth;
    private ThirdPersonController controller;

    void Awake()
    {
        controller = GetComponent<ThirdPersonController>();
    }

    void Start()
    {
        if (config != null) maxHealth = config.playerStartHealth;
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        // Блок поглощает часть урона (но минимум 1 всё же проходит)
        if (controller != null && controller.IsBlocking)
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - blockDamageAbsorb)));

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        CursorLocker.playerIsAlive = false;
        if (GameManager.Instance != null)
            GameManager.Instance.ShowGameOver();
        gameObject.SetActive(false);
    }
}
