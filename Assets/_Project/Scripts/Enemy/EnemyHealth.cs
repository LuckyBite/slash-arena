using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("UI (World Space)")]
    [Tooltip("Префаб полоски HP c Slider внутри (World Space Canvas)")]
    public GameObject healthBarPrefab;

    private int currentHealth;
    private Slider slider;
    private GameObject healthBarInstance;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
    }

    private void Start()
    {
        // если задан префаб полоски — создаём и настраиваем
        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);
            slider = healthBarInstance.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                slider.maxValue = maxHealth;
                slider.value = currentHealth;
            }

            // приподнимем над головой (если у префаба нет собственного оффсета)
            healthBarInstance.transform.localPosition = new Vector3(0f, 2.0f, 0f);
        }
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        if (slider != null)
            slider.value = currentHealth;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        // можно добавить анимацию смерти/очистку счёта
        Destroy(gameObject);
    }
}
