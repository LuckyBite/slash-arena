using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD текущего оружия: имя, прочность, иконка, подсказка подбора.
/// Повесь на Canvas и привяжи слоты — игрока найдёт сам.
/// </summary>
public class WeaponHUD : MonoBehaviour
{
    [Header("Слоты UI (создай элементы на Canvas и привяжи)")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private Slider durabilitySlider;
    [SerializeField] private Image weaponIcon;
    [Tooltip("Подсказка «F: взять …»")]
    [SerializeField] private TMP_Text interactPromptText;

    private WeaponHolder holder;
    private PlayerInteractor interactor;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            holder = player.GetComponent<WeaponHolder>();
            interactor = player.GetComponent<PlayerInteractor>();
        }

        if (holder != null) { holder.Changed += Refresh; Refresh(); }
        if (interactor != null) { interactor.PromptChanged += OnPrompt; OnPrompt(""); }
    }

    private void OnDestroy()
    {
        if (holder != null) holder.Changed -= Refresh;
        if (interactor != null) interactor.PromptChanged -= OnPrompt;
    }

    private void Refresh()
    {
        bool has = holder != null && holder.HasWeapon;

        if (weaponNameText) weaponNameText.text = has ? holder.Current.displayName : "Кулаки";

        if (durabilitySlider)
        {
            durabilitySlider.gameObject.SetActive(has);
            if (has)
            {
                durabilitySlider.maxValue = holder.Current.maxDurability;
                durabilitySlider.value = holder.Durability;
            }
        }

        if (weaponIcon)
        {
            bool showIcon = has && holder.Current.icon != null;
            weaponIcon.enabled = showIcon;
            if (showIcon) weaponIcon.sprite = holder.Current.icon;
        }
    }

    private void OnPrompt(string text)
    {
        if (!interactPromptText) return;
        interactPromptText.text = text;
        interactPromptText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }
}
