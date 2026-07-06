using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Обработка F (Interact): подбор оружия рядом. Подсказку отдаёт событием для HUD.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string actionInteract = "Interact";

    /// <summary>Текст подсказки («F: взять Меч»); пустая строка = скрыть.</summary>
    public event System.Action<string> PromptChanged;

    private PlayerInput pi;
    private WeaponHolder holder;
    private WeaponPickup candidate;

    private void Awake()
    {
        pi = GetComponent<PlayerInput>();
        holder = GetComponent<WeaponHolder>();

        var action = pi.actions.FindAction(actionInteract, false);
        if (action != null) action.performed += OnInteract;
        else Debug.LogWarning("[PlayerInteractor] Action 'Interact' не найден в Input Actions.");
    }

    private void OnDestroy()
    {
        if (pi && pi.actions != null)
        {
            var action = pi.actions.FindAction(actionInteract, false);
            if (action != null) action.performed -= OnInteract;
        }
    }

    public void SetCandidate(WeaponPickup pickup)
    {
        candidate = pickup;
        PromptChanged?.Invoke(pickup ? pickup.PromptText : "");
    }

    public void ClearCandidate(WeaponPickup pickup)
    {
        if (candidate != pickup) return;
        candidate = null;
        PromptChanged?.Invoke("");
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || candidate == null || holder == null) return;

        var used = candidate;
        candidate = null;
        PromptChanged?.Invoke("");
        used.Consume(holder);
    }
}
