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

    private NavMeshAgent agent;
    private float lastAttackTime = -999f;

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

        // Настройка агента
        agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.1f);
        agent.updateRotation = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

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

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

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

        if (player == null) yield break;
        if (Vector3.Distance(transform.position, player.position) > attackRange + 0.15f) yield break;

        // передаём урон в скрипт игрока (любой, кто его принимает)
        player.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
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
