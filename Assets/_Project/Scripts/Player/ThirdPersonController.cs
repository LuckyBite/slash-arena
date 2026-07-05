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
    [Tooltip("Множитель скорости при зажатом Sprint")]
    [SerializeField] private float sprintMultiplier = 1.7f;

    [Header("Dodge / Roll")]
    [Tooltip("Скорость импульса во время переката (м/с)")]
    [SerializeField] private float dodgeImpulse = 8f;
    [Tooltip("Длительность импульса (сек) — должна быть короче клипа Roll")]
    [SerializeField] private float dodgeDuration = 0.30f;
    [Tooltip("Кулдаун между перекатами (сек)")]
    [SerializeField] private float dodgeCooldown = 0.80f;
    public float rotationSmoothTime = 0.10f;
    public float animDampGround = 0.02f;
    public float animDampIdle   = 0.10f;

    [Header("Gravity")]
    [Tooltip("Гравитация (м/с²), отрицательная")]
    [SerializeField] private float gravity = -30f;
    [Tooltip("Прилипание к земле")]
    public float groundStick = -2f;

    [Header("Block")]
    [Tooltip("Множитель скорости движения при зажатом блоке")]
    [SerializeField] private float blockMoveMultiplier = 0.4f;

    [Header("Ground Check")]
    [Tooltip("Радиус сферы у ног")]
    public float groundProbeRadius = 0.32f;
    [Tooltip("Макс. дистанция SphereCast вниз")]
    public float groundCastDistance = 0.8f;
    [Tooltip("Гашение дрожания статуса (сек)")]
    public float groundedGrace = 0.03f;
    [Tooltip("Если завис в воздухе без движения дольше этого времени — мягко сбросить velocity и попробовать переприжать")]
    [SerializeField] private float airStuckRecoverTime = 1.5f;

    [Header("Air")]
    [Tooltip("Разрешать управление в воздухе")]
    public bool allowAirControl = false;

    [Header("Input Action Names (как в твоём Input Actions)")]
    public string actionMove = "Move";
    public string actionSprint = "Sprint";
    public string pMoveX = "MoveX";
    public string pMoveY = "MoveY";
    public string pSpeed = "Speed";
    public string pIsMoving = "isMoving";
    public string pIsGrounded = "isGrounded";
    public string pTrigLight = "Attack_Light";
    public string pTrigHeavy = "Attack_Heavy";
    public string pTrigKick  = "Kick";
    public string pBoolBlock = "Block";
    public string pTrigDodge = "Dodge";

    public string actionAttackLight = "Attack_Light";
    public string actionAttackHeavy = "Attack_Heavy";
    public string actionBlock       = "Block";
    public string actionKick        = "Kick";
    public string actionDodge = "Dodge";

    [Header("UpperBody / Attacks")]
    public int   upperBodyLayerIndex = 1;
    public float attackCooldown = 0.35f;
    [Tooltip("После конца атаки анимация движения форсируется без демпфера столько секунд (сглаживает выход из удара в бег)")]
    public float postAttackBlendHold = 0.14f;

    // runtime
    private CharacterController cc;
    private PlayerInput pi;
    private Vector2 moveInput;
    private bool sprintHeld;
    private bool blockHeld;
    private Vector3 velocity;
    private float rotVel;
    private bool grounded;
    private float lastGroundTime = -999f;
    private float dodgeUntil = -1f;
    private float dodgeCooldownUntil = -1f;
    private Vector3 dodgeDirection;
    private bool IsDodging => Time.time < dodgeUntil;

    /// <summary>Игрок держит блок (для PlayerHealth и др.).</summary>
    public bool IsBlocking => blockHeld;

    private float nextAttackAllowed = -1f;
    private bool isAttackLocked = false;
    private float attackReleaseUntil = -1f;
    private float _stuckLogCooldown;
    private float _airStuckTimer;

    // animator hashes
    private int hMoveX, hMoveY, hSpeed, hIsMoving, hIsGrounded, hLight, hHeavy, hKick, hBlock, hDodge;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        pi = GetComponent<PlayerInput>();

        if (!animator) Debug.LogError("[ThirdPersonController] Animator не назначен.");
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        // Animator hashes
        hMoveX      = Animator.StringToHash(pMoveX);
        hMoveY      = Animator.StringToHash(pMoveY);
        hSpeed      = Animator.StringToHash(pSpeed);
        hIsMoving   = Animator.StringToHash(pIsMoving);
        hIsGrounded = Animator.StringToHash(pIsGrounded);
        hLight      = Animator.StringToHash(pTrigLight);
        hHeavy      = Animator.StringToHash(pTrigHeavy);
        hKick       = Animator.StringToHash(pTrigKick);
        hBlock      = Animator.StringToHash(pBoolBlock);
        hDodge      = Animator.StringToHash(pTrigDodge);

        // Привязка инпута (Invoke C# Events)
        var map = pi.actions;
        map[actionMove].performed += OnMove;
        map[actionMove].canceled  += OnMove;
        map[actionSprint].performed += OnSprintPerformed;
        map[actionSprint].canceled  += OnSprintCanceled;
        map[actionAttackLight].performed += OnAttackLight;
        map[actionAttackHeavy].performed += OnAttackHeavy;
        map[actionKick].performed += OnKick;
        map[actionBlock].performed += OnBlockPerformed;
        map[actionBlock].canceled  += OnBlockCanceled;
        map[actionDodge].performed += OnDodge;
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
                map[actionSprint].performed -= OnSprintPerformed;
                map[actionSprint].canceled  -= OnSprintCanceled;
                map[actionAttackLight].performed -= OnAttackLight;
                map[actionAttackHeavy].performed -= OnAttackHeavy;
                map[actionKick].performed -= OnKick;
                map[actionBlock].performed -= OnBlockPerformed;
                map[actionBlock].canceled  -= OnBlockCanceled;
                map[actionDodge].performed -= OnDodge;
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) return;
        sprintHeld = false;
        blockHeld = false;
        if (animator) animator.SetBool(hBlock, false);
        moveInput = Vector2.zero;
    }

    private void Update()
    {
        UpdateGround();
        ApplyVertical();
        ApplyHorizontal();
        RecoverAttackIfFinished();
        UpdateUpperBodyLayerWeight();

        // Self-diagnostic: input есть, но не двигаемся → лог раз в секунду
        if (moveInput.sqrMagnitude > 0.01f
            && cc.velocity.sqrMagnitude < 0.01f
            && grounded
            && Time.time > _stuckLogCooldown)
        {
            _stuckLogCooldown = Time.time + 1f;
            Debug.LogWarning($"[STUCK] mv={moveInput} sprintHeld={sprintHeld} attackLocked={isAttackLocked} releaseUntil={attackReleaseUntil:F2} velY={velocity.y:F2} grounded={grounded}");
        }
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

        if (!wasGrounded && grounded)
        {
            velocity.y = groundStick;
        }

        if (animator) animator.SetBool(hIsGrounded, grounded);

        // Recovery: если в воздухе и не падаем → попробуем зануляться
        if (!grounded && Mathf.Abs(velocity.y) < 0.05f)
        {
            _airStuckTimer += Time.deltaTime;
            if (_airStuckTimer >= airStuckRecoverTime)
            {
                velocity.y = -1f; // лёгкий толчок вниз, чтобы CharacterController «отлип»
                _airStuckTimer = 0f;
            }
        }
        else _airStuckTimer = 0f;
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

        // Override движения во время Roll
        if (IsDodging)
        {
            cc.Move(dodgeDirection * dodgeImpulse * Time.deltaTime);
            // Animator: передаём last input для Roll Blend Tree
            if (animator)
            {
                Vector3 localDodge = transform.InverseTransformDirection(dodgeDirection);
                animator.SetFloat(hMoveX, localDodge.x);
                animator.SetFloat(hMoveY, localDodge.z);
            }
            return;
        }

        // Блок побеждает спринт: под блоком двигаемся медленно
        float speedMult = blockHeld ? blockMoveMultiplier
                        : (sprintHeld && grounded ? sprintMultiplier : 1f);
        cc.Move(desired * (moveSpeed * speedMult) * Time.deltaTime);

        if (!animator) return;

        bool moveReq = moveInput.sqrMagnitude > 1e-4f;
        bool runNow  = grounded && moveReq;
        bool sprintAnim = sprintHeld && !blockHeld;
        Vector3 local = transform.InverseTransformDirection(desired);

        // «Холд» после удара — форсируем бег, даже если ещё не «схлопнулся» бленд
        if (Time.time < attackReleaseUntil)
        {
            animator.SetFloat(hMoveX, local.x);
            animator.SetFloat(hMoveY, local.z);
            animator.SetFloat(hSpeed,  runNow ? (sprintAnim ? 1.5f : 1f) : 0f);
            animator.SetBool (hIsMoving, runNow);
            return;
        }

        float damp = runNow ? animDampGround : animDampIdle;
        animator.SetFloat(hMoveX, local.x, damp, Time.deltaTime);
        animator.SetFloat(hMoveY, local.z, damp, Time.deltaTime);
        animator.SetFloat(hSpeed,  runNow ? (sprintAnim ? 1.5f : 1f) : 0f, damp, Time.deltaTime);
        animator.SetBool (hIsMoving, runNow);
    }

    // ===== Input =====
    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx)  => sprintHeld = false;

    private void OnDodge(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (Time.time < dodgeCooldownUntil) return;
        if (!grounded) return;
        if (IsDodging) return;

        // Направление переката: если игрок двигается — по input, иначе назад
        Vector3 dir;
        if (moveInput.sqrMagnitude > 1e-4f && cameraTransform)
        {
            Vector3 f = cameraTransform.forward; f.y = 0; f.Normalize();
            Vector3 r = cameraTransform.right;   r.y = 0; r.Normalize();
            dir = (f * moveInput.y + r * moveInput.x).normalized;
        }
        else
        {
            dir = -transform.forward;
        }

        dodgeDirection = dir;
        dodgeUntil = Time.time + dodgeDuration;
        dodgeCooldownUntil = Time.time + dodgeCooldown;

        if (animator)
        {
            animator.ResetTrigger(hDodge);
            animator.SetTrigger(hDodge);
        }
    }

    // ===== Attacks =====
    private void OnAttackLight(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryAttack(hLight);
    }

    private void OnAttackHeavy(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryAttack(hHeavy);
    }

    private void OnKick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryAttack(hKick);
    }

    private void OnBlockPerformed(InputAction.CallbackContext ctx)
    {
        blockHeld = true;
        if (animator) animator.SetBool(hBlock, true);
    }

    private void OnBlockCanceled(InputAction.CallbackContext ctx)
    {
        blockHeld = false;
        if (animator) animator.SetBool(hBlock, false);
    }

    private void TryAttack(int trigHash)
    {
        if (IsDodging) return;
        if (Time.time < nextAttackAllowed) return;
        if (isAttackLocked || IsUpperAttackPlaying()) return;

        if (animator)
        {
            animator.ResetTrigger(trigHash);
            animator.SetTrigger(trigHash);
        }

        isAttackLocked = true;
        nextAttackAllowed = Time.time + attackCooldown;
    }

    private bool IsUpperAttackPlaying()
    {
        if (animator == null || animator.layerCount <= upperBodyLayerIndex) return false;
        // Самая надёжная проверка: есть ли в текущем стейте верхнего слоя НАЗНАЧЕННЫЙ клип.
        // У Upper_Empty Motion = None → clip count == 0 → значит активен empty → weight в 0.
        // У LightAttack/HeavyAttack/Block/Kick клип есть → clip count > 0 → weight в 1.
        // Count-версии не аллоцируют (в отличие от GetCurrentAnimatorClipInfo).
        if (animator.GetCurrentAnimatorClipInfoCount(upperBodyLayerIndex) > 0) return true;
        // На время transition тоже учитываем
        if (animator.IsInTransition(upperBodyLayerIndex)
            && animator.GetNextAnimatorClipInfoCount(upperBodyLayerIndex) > 0) return true;
        return false;
    }

    private void RecoverAttackIfFinished()
    {
        if (!isAttackLocked) return;
        if (!IsUpperAttackPlaying())
        {
            isAttackLocked = false;
            attackReleaseUntil = Time.time + postAttackBlendHold;
        }
    }

    // Единое место управления весом UpperBody слоя.
    // Любой не-empty стейт верхнего слоя поднимает вес плавно в 1, иначе — в 0.
    private void UpdateUpperBodyLayerWeight()
    {
        if (animator == null || animator.layerCount <= upperBodyLayerIndex) return;

        bool upperActive = IsUpperAttackPlaying();
        float target = upperActive ? 1f : 0f;
        float current = animator.GetLayerWeight(upperBodyLayerIndex);
        // Поднимаем чуть быстрее (более резкая атака), опускаем чуть медленнее (мягкий возврат)
        float speed = upperActive ? 16f : 9f;
        float next = Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        animator.SetLayerWeight(upperBodyLayerIndex, next);
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
