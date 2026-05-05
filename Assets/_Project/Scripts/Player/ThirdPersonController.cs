using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator модели. Укажи сюда Player_Animator из инспектора.")]
    public Animator animator;
    public Transform cameraTransform;     // если пусто — возьмём Camera.main
    public Transform groundCheck;         // точка у стоп
    public LayerMask groundMask;          // должен содержать ТОЛЬКО слой земли

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSmoothTime = 0.10f;
    public float animDampGround = 0.02f;
    public float animDampIdle   = 0.10f;

    [Header("Jump & Gravity (by metrics)")]
    [Tooltip("Высота прыжка (м)")]
    public float jumpHeight = 1.6f;
    [Tooltip("Время до апекса (сек) — меньше = быстрее и «тяжелее»")]
    public float timeToApex = 0.33f;
    [Tooltip("Прилипание к земле")]
    public float groundStick = -2f;

    [Header("Ground Check")]
    [Tooltip("Радиус сферы у ног")]
    public float groundProbeRadius = 0.24f;
    [Tooltip("Макс. дистанция SphereCast вниз")]
    public float groundCastDistance = 0.5f;
    [Tooltip("Гашение дрожания статуса (сек)")]
    public float groundedGrace = 0.03f;

    [Header("Coyote / Buffer")]
    [Tooltip("Время после схода с края, когда прыжок ещё возможен")]
    public float coyoteTime = 0.12f;
    [Tooltip("Буфер нажатия прыжка ДО касания земли")]
    public float jumpBufferTime = 0.12f;
    [Tooltip("Разрешать управление в воздухе")]
    public bool allowAirControl = false;

    [Header("Input Action Names (как в твоём Input Actions)")]
    public string actionMove = "Move";
    public string actionJump = "Jump";
    public string pMoveX = "MoveX";
    public string pMoveY = "MoveY";
    public string pSpeed = "Speed";
    public string pIsMoving = "isMoving";
    public string pIsGrounded = "isGrounded";
    public string pTrigPunch = "Attack_Punch";
    public string pTrigSword = "Attack_OneHandSword";

    [Header("UpperBody / Attacks")]
    public int   upperBodyLayerIndex = 1;
    public float attackCooldown = 0.35f;
    public float postAttackBlendHold = 0.14f; // ЧУТЬ УВЕЛИЧИЛ: фикс редкого микро-idle

    // runtime
    private CharacterController cc;
    private PlayerInput pi;
    private Vector2 moveInput;
    private Vector3 velocity;
    private float rotVel;
    private bool grounded;
    private float lastGroundTime = -999f;
    private float lastJumpPressedTime = -999f; // буфер нажатия прыжка

    private float gravity;      // высчитываем из метрик
    private float jumpVelocity; // высчитываем из метрик

    private float nextAttackAllowed = -1f;
    private bool isAttackLocked = false;
    private float attackReleaseUntil = -1f;

    // animator hashes
    private int hMoveX, hMoveY, hSpeed, hIsMoving, hIsGrounded, hPunch, hSword;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        if (!animator) Debug.LogError("[ThirdPersonController] Animator не назначен.");
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        // Физика прыжка из метрик
        gravity      = -2f * jumpHeight / (timeToApex * timeToApex);
        jumpVelocity =  2f * jumpHeight /  timeToApex;

        // Animator hashes
        hMoveX      = Animator.StringToHash(pMoveX);
        hMoveY      = Animator.StringToHash(pMoveY);
        hSpeed      = Animator.StringToHash(pSpeed);
        hIsMoving   = Animator.StringToHash(pIsMoving);
        hIsGrounded = Animator.StringToHash(pIsGrounded);
        hPunch      = Animator.StringToHash(pTrigPunch);
        hSword      = Animator.StringToHash(pTrigSword);

        // Привязка инпута (Invoke C# Events)
        var map = pi.actions;
        map[actionMove].performed += OnMove;
        map[actionMove].canceled  += OnMove;
        map[actionJump].performed += OnJump;
    }

    private void OnDestroy()
    {
        if (pi)
        {
            var map = pi.actions;
            if (map != null)
            {
                map[actionMove].performed -= OnMove;
                map[actionMove].canceled  -= OnMove;
                map[actionJump].performed -= OnJump;
            }
        }
    }

    private void Update()
    {
        UpdateGround();
        ApplyVertical();
        ApplyHorizontal();
        RecoverAttackIfFinished();

        // Если атака не играет — слой в 0, чтобы не «кусал» локомоцию
        if (animator && animator.layerCount > upperBodyLayerIndex && !IsUpperAttackPlaying())
            animator.SetLayerWeight(upperBodyLayerIndex, 0f);
    }

    // ===== Ground =====
    private void UpdateGround()
    {
        bool ccGround = cc.isGrounded;

        // Страховка от неправильной маски: слой Player не должен входить в groundMask
        // (настроить в проекте). Здесь игнорируем триггеры.
        bool probe = false;
        if (groundCheck)
        {
            // Узкая проверка у ног
            probe = Physics.CheckSphere(groundCheck.position, groundProbeRadius, groundMask, QueryTriggerInteraction.Ignore);

            // Более надёжная: SphereCast вниз на малую глубину
            if (!probe)
            {
                Vector3 origin = groundCheck.position + Vector3.up * 0.05f;
                if (Physics.SphereCast(origin, groundProbeRadius * 0.9f, Vector3.down, out _, groundCastDistance, groundMask, QueryTriggerInteraction.Ignore))
                    probe = true;
            }
        }
        else
        {
            Debug.LogWarning("[ThirdPersonController] GroundCheck не назначен! Поставь пустой трансформ у стоп.");
        }

        bool wasGrounded = grounded;
        grounded = ccGround || probe;

        if (grounded) lastGroundTime = Time.time;
        if (grounded && velocity.y < 0f) velocity.y = groundStick; // прижимаем к земле

        // гашение дрожания
        if (!grounded && (Time.time - lastGroundTime) <= groundedGrace)
            grounded = true;

        if (animator) animator.SetBool(hIsGrounded, grounded);
    }

    // ===== Vertical first =====
    private void ApplyVertical()
    {
        velocity.y += gravity * Time.deltaTime;
        cc.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
    }

    // ===== Horizontal & Anim =====
    private void ApplyHorizontal()
    {
        Vector3 f = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 r = cameraTransform ? cameraTransform.right   : Vector3.right;
        f.y = 0; r.y = 0; f.Normalize(); r.Normalize();

        Vector3 desired = f * moveInput.y + r * moveInput.x;
        bool allowMove = grounded || allowAirControl;
        if (!allowMove) desired = Vector3.zero;
        if (desired.sqrMagnitude > 1f) desired.Normalize();

        if (allowMove && desired.sqrMagnitude > 1e-4f && cameraTransform)
        {
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            float y = Mathf.SmoothDampAngle(transform.eulerAngles.y, yaw, ref rotVel, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        cc.Move(desired * moveSpeed * Time.deltaTime);

        if (!animator) return;

        bool moveReq = moveInput.sqrMagnitude > 1e-4f;
        bool runNow  = grounded && moveReq;
        Vector3 local = transform.InverseTransformDirection(desired);

        // «Холд» после удара — форсируем бег, даже если ещё не «схлопнулся» бленд
        if (Time.time < attackReleaseUntil)
        {
            animator.SetFloat(hMoveX, local.x);
            animator.SetFloat(hMoveY, local.z);
            animator.SetFloat(hSpeed,  runNow ? 1f : 0f);
            animator.SetBool (hIsMoving, runNow);
            return;
        }

        float damp = runNow ? animDampGround : animDampIdle;
        animator.SetFloat(hMoveX, local.x, damp, Time.deltaTime);
        animator.SetFloat(hMoveY, local.z, damp, Time.deltaTime);
        animator.SetFloat(hSpeed,  runNow ? 1f : 0f, damp, Time.deltaTime);
        animator.SetBool (hIsMoving, runNow);
    }

    // ===== Input =====
    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        lastJumpPressedTime = Time.time; // буферим нажатие
        DoJumpIfAllowed();
    }

    // ===== Jump =====
    private void DoJumpIfAllowed()
    {
        bool canUseCoyote = (Time.time - lastGroundTime) <= coyoteTime;
        bool canUseBuffer = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        bool canJump = grounded || canUseCoyote;
        if (!canJump || !canUseBuffer) return;

        velocity.y = jumpVelocity;
        lastJumpPressedTime = -999f;
        lastGroundTime = -999f;
        // Твой Animator параметр "Jump" не трогаю — ты им не управляешь напрямую
    }

    // ===== Attacks =====
    public void OnAttackPunch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryAttack(hPunch);
    }

    public void OnAttackOneHandSword(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryAttack(hSword);
    }

    private void TryAttack(int trigHash)
    {
        if (Time.time < nextAttackAllowed) return;
        if (isAttackLocked || IsUpperAttackPlaying()) return;

        if (animator)
        {
            animator.ResetTrigger(trigHash);
            animator.SetTrigger(trigHash);
            if (animator.layerCount > upperBodyLayerIndex)
                animator.SetLayerWeight(upperBodyLayerIndex, 1f);
        }

        isAttackLocked = true;
        nextAttackAllowed = Time.time + attackCooldown;
    }

    private bool IsUpperAttackPlaying()
    {
        if (!animator || animator.layerCount <= upperBodyLayerIndex) return false;
        var st = animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex);
        return st.length > 0f && st.normalizedTime < 1f && !st.IsName("Upper_Empty");
    }

    private void RecoverAttackIfFinished()
    {
        if (!isAttackLocked) return;
        if (!IsUpperAttackPlaying())
        {
            isAttackLocked = false;
            if (animator.layerCount > upperBodyLayerIndex)
                animator.SetLayerWeight(upperBodyLayerIndex, 0f);
            attackReleaseUntil = Time.time + postAttackBlendHold;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!groundCheck) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundProbeRadius);
    }
#endif
}
