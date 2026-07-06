using UnityEngine;

/// <summary>
/// Оружие, лежащее на карте. Создаётся WeaponSpawner'ом (или кладётся руками в сцену:
/// объект с SphereCollider-триггером + этот компонент + визуал ребёнком).
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class WeaponPickup : MonoBehaviour
{
    [Tooltip("Какое оружие даёт этот пикап")]
    public WeaponDefinition definition;

    [Header("Идл-анимация пикапа")]
    [SerializeField] private float spinSpeed = 60f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobFrequency = 1.6f;

    public event System.Action<WeaponPickup> Consumed;

    private Vector3 basePos;
    private Transform visual; // первый ребёнок

    private void Awake()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        basePos = transform.position;
        if (transform.childCount > 0) visual = transform.GetChild(0);
    }

    private void Update()
    {
        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude);
        if (visual) visual.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor) interactor.SetCandidate(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var interactor = other.GetComponentInParent<PlayerInteractor>();
        if (interactor) interactor.ClearCandidate(this);
    }

    public string PromptText => definition ? $"F: взять {definition.displayName}" : "";

    public void Consume(WeaponHolder holder)
    {
        if (definition == null || holder == null) return;
        holder.Equip(definition);
        Consumed?.Invoke(this);
        Destroy(gameObject);
    }
}
