using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Animator (имена параметров; отсутствующие тихо пропускаются)")]
    [SerializeField] private Animator animator;
    [Tooltip("Float скорости движения (в демо-контроллере скелета его НЕТ)")]
    [SerializeField] private string speedParam = "Speed";
    [Tooltip("Bool «идёт» — именно его использует демо-контроллер скелета")]
    [SerializeField] private string walkingBoolParam = "isWalking";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Move/Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float attackWindup = 0.25f; // задержка перед уроном
    [Tooltip("Как часто пересчитывать путь к игроку (сек). Каждый кадр — дорого при 30+ врагах.")]
    [SerializeField] private float repathInterval = 0.2f;

    [Header("SFX (слот — назначь клип)")]
    [Tooltip("Замах/рык при атаке")]
    [SerializeField] private AudioClip attackClip;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private float lastAttackTime = -999f;
    private float nextRepathTime;
    private IDamageable playerDamageable;

    // кэш флагов наличия параметров
    private bool hasSpeedParam;
    private bool hasWalkingBool;
    private bool hasAttackTrigger;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }

        // Кэшируем получателя урона один раз (вместо SendMessage каждый удар)
        if (player != null)
        {
            playerDamageable = player.GetComponent<IDamageable>();
            if (playerDamageable == null)
                playerDamageable = player.GetComponentInChildren<IDamageable>();
        }

        // Настройка агента. Med-avoidance: High слишком дорог при 30+ агентах
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.1f);
        agent.updateRotation = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        // Валидируем параметры аниматора, чтобы не падать в рантайме
        if (animator)
        {
            hasSpeedParam    = AnimatorParams.Has(animator, speedParam,       AnimatorControllerParameterType.Float);
            hasWalkingBool   = AnimatorParams.Has(animator, walkingBoolParam, AnimatorControllerParameterType.Bool);
            hasAttackTrigger = AnimatorParams.Has(animator, attackTrigger,    AnimatorControllerParameterType.Trigger);
        }
    }

    private void Update()
    {
        if (player == null) return;
        if (health != null && health.IsDead) return; // труп не воюет

        // Игрок мёртв/выключен — стоим спокойно
        if (!player.gameObject.activeInHierarchy)
        {
            if (!agent.isStopped) agent.isStopped = true;
            SetMoveAnim(false, 0f);
            return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;

            // Путь пересчитываем не каждый кадр — этого достаточно, игрок не телепортируется
            if (Time.time >= nextRepathTime)
            {
                nextRepathTime = Time.time + repathInterval;
                agent.SetDestination(player.position);
            }

            SetMoveAnim(agent.velocity.sqrMagnitude > 0.04f, agent.velocity.magnitude);
        }
        else
        {
            agent.isStopped = true;
            SetMoveAnim(false, 0f);

            // Повернуться к игроку
            Vector3 look = player.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
            {
                Quaternion rq = Quaternion.LookRotation(look);
                transform.rotation = Quaternion.Slerp(transform.rotation, rq, Time.deltaTime * 8f);
            }

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;

                if (animator && hasAttackTrigger)
                    animator.SetTrigger(attackTrigger);

                Sfx.Play(attackClip, transform.position);

                // Телеграф удара (урон один раз за атаку)
                StartCoroutine(DealDamageAfterDelay(attackWindup));
            }
        }
    }

    private void SetMoveAnim(bool walking, float speed)
    {
        if (!animator) return;
        if (hasWalkingBool) animator.SetBool(walkingBoolParam, walking);
        if (hasSpeedParam)  animator.SetFloat(speedParam, speed);
    }

    private System.Collections.IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (health != null && health.IsDead) yield break; // умер во время замаха
        if (player == null || playerDamageable == null) yield break;
        if (!player.gameObject.activeInHierarchy) yield break;
        if (Vector3.Distance(transform.position, player.position) > attackRange + 0.15f) yield break;

        playerDamageable.TakeDamage(damage);
    }
}
