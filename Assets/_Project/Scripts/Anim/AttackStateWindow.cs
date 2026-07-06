using UnityEngine;

// Вешается на state в Animator.
// В окне [windowStart..windowEnd] сделает ровно ОДИН хит (без событий клипа).
public class AttackStateWindow : StateMachineBehaviour
{
    [Header("What to hit with")]
    public WeaponType weapon = WeaponType.Fist;
    public AttackStrength strength = AttackStrength.Light;

    [Header("Hit window (normalized time, 0..1, можно >1 для циклов)")]
    [Range(0f, 2f)] public float windowStart = 0.30f;
    [Range(0f, 2f)] public float windowEnd   = 0.45f;

    private bool didHit;
    private PlayerAttack attack; // кэш на время стейта

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        didHit = false;
        attack = animator.GetComponent<PlayerAttack>();
        if (attack != null)
            attack.OnSwingStarted(strength); // свист замаха
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (didHit || attack == null) return;

        float t = stateInfo.normalizedTime; // может идти >1 при петлях
        // поддержим окна и в 0..1, и в 1..2 (на всякий случай)
        bool inWindow = (t >= windowStart && t <= windowEnd)
                     || (t >= (windowStart + 1f) && t <= (windowEnd + 1f));

        if (!inWindow) return;

        attack.ImmediateAttack(weapon, strength); // нанесём урон один раз
        didHit = true;
    }
}
