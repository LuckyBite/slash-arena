using UnityEngine;
using UnityEngine.InputSystem;

public class DiagnosticsHUD : MonoBehaviour
{
    [Header("Refs")]
    public Animator playerAnimator;
    public CharacterController playerCC;
    public PlayerInput playerInput;
    public Transform groundCheck;     // точка у ног
    public LayerMask groundMask;      // должен быть только Ground
    public int upperBodyLayerIndex = 1;

    [Header("Attack Probe (player)")]
    public Transform hitOrigin;       // та же, что у PlayerAttack
    public float probeRange = 0.9f;
    public float probeRadius = 0.4f;
    public float probeYOffset = 1.1f;
    public LayerMask enemyLayer;

    private Vector3 _prevPos;
    private float _velY;

    private void Reset()
    {
        playerAnimator = GetComponentInChildren<Animator>();
        playerCC = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        // простая оценка вертикальной скорости (если нет публичной переменной)
        float dy = transform.position.y - _prevPos.y;
        _velY = dy / Mathf.Max(Time.deltaTime, 1e-5f);
        _prevPos = transform.position;
    }

    private void OnGUI()
    {
        const int pad = 8;
        int x = 10, y = 10, lh = 20;

        GUI.Label(new Rect(x,y,600,lh), "=== SmashArena Diagnostics ==="); y+=lh;

        // Ground
        bool ccGround = playerCC ? playerCC.isGrounded : false;
        bool probe = false;
        if (groundCheck)
        {
            Vector3 origin = groundCheck.position + Vector3.up*0.05f;
            probe = Physics.CheckSphere(groundCheck.position, 0.24f, groundMask, QueryTriggerInteraction.Ignore)
                 || Physics.SphereCast(origin, 0.22f, Vector3.down, out _, 0.5f, groundMask, QueryTriggerInteraction.Ignore);
        }
        GUI.Label(new Rect(x,y,600,lh), $"Ground: CC={ccGround} | Probe={probe} | velY={_velY:F2}"); y+=lh;

        // Animator flags
        if (playerAnimator)
        {
            bool has_isGrounded = HasParam(playerAnimator, "isGrounded", AnimatorControllerParameterType.Bool);
            bool p_isGrounded = has_isGrounded && playerAnimator.GetBool("isGrounded");
            float speed = HasParam(playerAnimator, "Speed", AnimatorControllerParameterType.Float) ? playerAnimator.GetFloat("Speed") : -1f;

            GUI.Label(new Rect(x,y,600,lh), $"Animator: isGroundedParam={has_isGrounded} val={p_isGrounded} | Speed={speed:F2}"); y+=lh;

            string baseState = playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("") ? "<none>" :
                playerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash.ToString();
            string upperState = (playerAnimator.layerCount>upperBodyLayerIndex) ?
                playerAnimator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex).shortNameHash.ToString() : "-";
            float upperW = (playerAnimator.layerCount>upperBodyLayerIndex) ? playerAnimator.GetLayerWeight(upperBodyLayerIndex) : -1f;

            GUI.Label(new Rect(x,y,800,lh), $"States: BaseHash={baseState} | UpperHash={upperState} | UpperW={upperW:F2}"); y+=lh;
        }

        // Input
        if (playerInput)
        {
            var actMove = playerInput.actions.FindAction("Move", false);
            var actJump = playerInput.actions.FindAction("Jump", false);
            var actP    = playerInput.actions.FindAction("AttackPunch", false);
            var actS    = playerInput.actions.FindAction("AttackOneHandSword", false);
            Vector2 mv = actMove!=null ? actMove.ReadValue<Vector2>() : Vector2.zero;
            GUI.Label(new Rect(x,y,800,lh), $"Input: Move=({mv.x:F2},{mv.y:F2}) | Jump={(actJump!=null && actJump.triggered)} | Punch={(actP!=null && actP.triggered)} | Sword={(actS!=null && actS.triggered)}"); y+=lh;
        }

        // Attack overlap probe
        if (hitOrigin)
        {
            Vector3 c = hitOrigin.position + hitOrigin.forward * probeRange + Vector3.up * probeYOffset;
            int count = Physics.OverlapSphere(c, probeRadius, enemyLayer, QueryTriggerInteraction.Collide).Length;
            GUI.Label(new Rect(x,y,800,lh), $"AttackProbe: center={c} radius={probeRadius:F2} hits={count}"); y+=lh;
        }
    }

    private bool HasParam(Animator a, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in a.parameters) if (p.type==type && p.name==name) return true;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!hitOrigin) return;
        Gizmos.color = Color.red;
        Vector3 c = hitOrigin.position + hitOrigin.forward * probeRange + Vector3.up * probeYOffset;
        Gizmos.DrawWireSphere(c, probeRadius);
    }
#endif
}
