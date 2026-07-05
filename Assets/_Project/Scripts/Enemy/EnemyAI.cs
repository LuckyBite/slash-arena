using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Animator (безопасные имена параметров)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";   // float
    [SerializeField] private string attackTrigger = "Attack"; // trigger

    [Header("Move/Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int damage = 5;
    [SerializeField] private float attackWindup = 0.25f; // задержка перед уроном
    [Tooltip("Как часто пересчитывать путь к игроку (сек). Каждый кадр — дорого при 30+ врагах.")]
    [SerializeField] private float repathInterval = 0.2f;

    private NavMeshAgent agent;
    private float lastAttackTime = -999f;
    private float nextRepathTime;
    private IDamageable playerDamageable;

    // кэш флагов наличия параметров
    private bool hasSpeedParam;
    private bool hasAttackTrigger;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
            hasSpeedParam = HasAnimatorParam(animator, speedParam, AnimatorControllerParameterType.Float);
            hasAttackTrigger = HasAnimatorParam(animator, attackTrigger, AnimatorControllerParameterType.Trigger);
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Игрок мёртв/выключен — стоим спокойно
        if (!player.gameObject.activeInHierarchy)
        {
            if (!agent.isStopped) agent.isStopped = true;
            if (animator && hasSpeedParam) animator.SetFloat(speedParam, 0f);
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

            if (animator && hasSpeedParam)
                animator.SetFloat(speedParam, agent.velocity.magnitude);
        }
        else
        {
            agent.isStopped = true;

            if (animator && hasSpeedParam)
                animator.SetFloat(speedParam, 0f);

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

                // Телеграф удара (урон один раз за атаку)
                StartCoroutine(DealDamageAfterDelay(attackWindup));
            }
        }
    }

    private System.Collections.IEnumerator DealDamageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (player == null || playerDamageable == null) yield break;
        if (!player.gameObject.activeInHierarchy) yield break;
        if (Vector3.Distance(transform.position, player.position) > attackRange + 0.15f) yield break;

        playerDamageable.TakeDamage(damage);
    }

    private static bool HasAnimatorParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (!anim || string.IsNullOrEmpty(name)) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name)
                return true;
        return false;
    }
}
