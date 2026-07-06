using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 100;

    [Header("UI (World Space)")]
    [Tooltip("Префаб полоски HP c Slider внутри (World Space Canvas)")]
    public GameObject healthBarPrefab;

    [Header("Animator (имена параметров; отсутствующие тихо пропускаются)")]
    [SerializeField] private Animator animator; // если пусто — возьмём из детей
    [Tooltip("Trigger вздрагивания при получении урона (у демо-контроллера скелета его нет — добавь стейт при желании)")]
    [SerializeField] private string hitTrigger = "Hit";
    [Tooltip("Trigger анимации смерти")]
    [SerializeField] private string dieTrigger = "Die";
    [Tooltip("Bool «мёртв» (блокирует другие стейты)")]
    [SerializeField] private string isDeadBool = "isDead";

    [Header("Death")]
    [Tooltip("Сколько секунд труп лежит до удаления")]
    [SerializeField] private float destroyDelay = 2.5f;
    [Tooltip("Если анимации смерти нет — «утопить» труп под пол перед удалением")]
    [SerializeField] private bool sinkIfNoDeathAnim = true;

    [Header("SFX (слоты — назначь клипы)")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip deathClip;

    /// <summary>Враг умер (началась анимация смерти). Стреляет один раз.</summary>
    public event System.Action<EnemyHealth> Died;

    public bool IsDead { get; private set; }

    private int currentHealth;
    private Slider slider;
    private GameObject healthBarInstance;
    private bool hasHitTrigger, hasDieTrigger, hasIsDeadBool;

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            hasHitTrigger  = AnimatorParams.Has(animator, hitTrigger,  AnimatorControllerParameterType.Trigger);
            hasDieTrigger  = AnimatorParams.Has(animator, dieTrigger,  AnimatorControllerParameterType.Trigger);
            hasIsDeadBool  = AnimatorParams.Has(animator, isDeadBool,  AnimatorControllerParameterType.Bool);
        }
    }

    /// <summary>
    /// Масштабирование HP по сложности волны. Вызывать сразу после Instantiate (до первого кадра).
    /// </summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (multiplier <= 0f || IsDead) return;
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * multiplier));
        currentHealth = maxHealth;
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
        if (amount <= 0 || IsDead) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        if (slider != null)
            slider.value = currentHealth;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        Sfx.Play(hurtClip, transform.position);
        if (animator && hasHitTrigger)
            animator.SetTrigger(hitTrigger);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        Died?.Invoke(this);

        // Очки за убийство (сколько именно — решает GameManager по GameConfig)
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();

        Sfx.Play(deathClip, transform.position);

        // Анимация смерти
        if (animator)
        {
            if (hasIsDeadBool) animator.SetBool(isDeadBool, true);
            if (hasDieTrigger) animator.SetTrigger(dieTrigger);
        }

        // Труп не должен мешать: выключаем мозги, навигацию и коллайдеры
        var ai = GetComponent<EnemyAI>();
        if (ai) ai.enabled = false;

        var agent = GetComponent<NavMeshAgent>();
        if (agent) agent.enabled = false;

        var cols = GetComponentsInChildren<Collider>();
        for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;

        if (healthBarInstance) healthBarInstance.SetActive(false);

        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(destroyDelay);

        // Фоллбек без анимации смерти: мягко утопить труп под пол
        bool hasDeathAnim = animator && (hasDieTrigger || hasIsDeadBool);
        if (!hasDeathAnim && sinkIfNoDeathAnim)
        {
            float t = 0f;
            Vector3 start = transform.position;
            while (t < 1f)
            {
                t += Time.deltaTime;
                transform.position = start + Vector3.down * (t * 1.2f);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
