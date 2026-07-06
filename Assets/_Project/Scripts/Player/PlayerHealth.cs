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

    [Header("SFX (слоты — назначь клипы)")]
    [Tooltip("Получил урон")]
    [SerializeField] private AudioClip hurtClip;
    [Tooltip("Удар пришёл в блок")]
    [SerializeField] private AudioClip blockedClip;
    [Tooltip("Смерть игрока")]
    [SerializeField] private AudioClip deathClip;

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

        // I-frames: во время переката урон не проходит вообще
        if (controller != null && controller.IsDodging) return;

        // Блок поглощает часть урона (но минимум 1 всё же проходит)
        bool blocked = controller != null && controller.IsBlocking;
        if (blocked)
            amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - blockDamageAbsorb)));

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        Sfx.Play(blocked ? blockedClip : hurtClip, transform.position);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Sfx.Play(deathClip, transform.position);
        CursorLocker.playerIsAlive = false;
        if (GameManager.Instance != null)
            GameManager.Instance.ShowGameOver();
        gameObject.SetActive(false);
    }
}
