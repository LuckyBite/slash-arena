using UnityEngine;

// Шаги по таймеру, скорость шагов зависит от фактической скорости движения.
// Клипы — слоты: пока не назначены, компонент молчит.
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("SFX (слоты — назначь клипы шагов)")]
    [Tooltip("Несколько вариантов шага, играются случайно")]
    [SerializeField] private AudioClip[] stepClips;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.6f;

    [Header("Timing")]
    [Tooltip("Интервал шага при беге (сек)")]
    [SerializeField] private float stepIntervalRun = 0.42f;
    [Tooltip("Интервал шага при спринте (сек)")]
    [SerializeField] private float stepIntervalSprint = 0.32f;
    [Tooltip("Мин. горизонтальная скорость, при которой считаем что идём")]
    [SerializeField] private float minSpeed = 1.0f;

    private CharacterController cc;
    private ThirdPersonController tpc;
    private float stepTimer;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        tpc = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (stepClips == null || stepClips.Length == 0) return;

        Vector3 v = cc.velocity; v.y = 0f;
        bool moving = cc.isGrounded && v.sqrMagnitude > minSpeed * minSpeed;
        if (!moving)
        {
            stepTimer = 0f; // следующий шаг сразу при старте движения
            return;
        }

        // Спринт = чаще шаги. Скорость берём по факту (спринт быстрее блока и ходьбы).
        float interval = (v.magnitude > (tpc ? tpc.moveSpeed * 1.2f : 5f)) ? stepIntervalSprint : stepIntervalRun;

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            stepTimer = interval;
            Sfx.PlayRandom(stepClips, transform.position, volume);
        }
    }
}
