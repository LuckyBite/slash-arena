using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD текущего оружия: имя + прочность и подсказка подбора «F: взять …».
/// Слоты можно привязать вручную; пустые слоты HUD достроит сам на найденном Canvas.
/// </summary>
public class WeaponHUD : MonoBehaviour
{
    [Header("Слоты UI (пусто = создадим сами)")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private Slider durabilitySlider;   // опционально
    [SerializeField] private Image weaponIcon;          // опционально
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

        EnsureUi();

        if (holder != null) { holder.Changed += Refresh; Refresh(); }
        if (interactor != null) { interactor.PromptChanged += OnPrompt; OnPrompt(""); }
    }

    private void EnsureUi()
    {
        if (weaponNameText != null && interactPromptText != null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (weaponNameText == null)
        {
            weaponNameText = CreateText(canvas.transform, "WeaponName (auto)", 30);
            var rt = weaponNameText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20, 20);
            rt.sizeDelta = new Vector2(420, 44);
            weaponNameText.alignment = TextAlignmentOptions.BottomRight;
        }

        if (interactPromptText == null)
        {
            interactPromptText = CreateText(canvas.transform, "InteractPrompt (auto)", 32);
            var rt = interactPromptText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 140);
            rt.sizeDelta = new Vector2(700, 46);
            interactPromptText.alignment = TextAlignmentOptions.Center;
        }
    }

    private static TMP_Text CreateText(Transform parent, string name, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void OnDestroy()
    {
        if (holder != null) holder.Changed -= Refresh;
        if (interactor != null) interactor.PromptChanged -= OnPrompt;
    }

    private void Refresh()
    {
        bool has = holder != null && holder.HasWeapon;

        if (weaponNameText)
            weaponNameText.text = has
                ? $"{holder.Current.displayName} · прочность {holder.Durability}/{holder.Current.maxDurability}"
                : "Кулаки";

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
