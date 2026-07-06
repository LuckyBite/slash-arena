using UnityEngine;

/// <summary>
/// Оружие как данные (D1): статы, прочность, визуал, анимации, звуки.
/// Один .asset на каждое оружие в Assets/_Project/Data/Weapons/.
/// </summary>
[CreateAssetMenu(fileName = "Weapon_", menuName = "Slash Arena/Weapon Definition", order = 1)]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "sword";
    public string displayName = "Меч";
    [Tooltip("Иконка для HUD (слот)")]
    public Sprite icon;

    [Header("Visual")]
    [Tooltip("Префаб модели в руку (слот — PurePoly позже). Также используется как визуал пикапа на карте")]
    public GameObject handPrefab;

    [Header("Combat")]
    public WeaponConfig config = new WeaponConfig();

    [Header("Durability")]
    [Tooltip("Сколько ПОПАДАНИЙ выдерживает оружие (промахи бесплатны)")]
    public int maxDurability = 20;
    [Tooltip("Сколько прочности тратит одно попадание")]
    public int durabilityLossPerHit = 1;

    [Header("Animator")]
    [Tooltip("Animator Override Controller: подменяет клипы атак под это оружие (слот)")]
    public RuntimeAnimatorController overrideController;

    [Header("SFX (слоты)")]
    [Tooltip("Свист замаха этим оружием (пусто = общий свист из PlayerAttack)")]
    public AudioClip swingClip;
    [Tooltip("Звук попадания этим оружием (пусто = общий из PlayerAttack)")]
    public AudioClip hitClip;
    public AudioClip pickupClip;
    public AudioClip breakClip;
}
