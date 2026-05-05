using UnityEngine;

/// <summary>
/// Глобальные настройки игры. Один .asset файл в Assets/_Project/Data/Configs/.
/// Это пилотный ScriptableObject — пример, как мы будем выносить данные из кода.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Slash Arena/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("Player")]
    [Tooltip("Стартовое здоровье игрока")]
    public int playerStartHealth = 100;

    [Tooltip("Базовая чувствительность мыши (множитель)")]
    [Range(0.1f, 5f)]
    public float mouseSensitivity = 1.0f;

    [Header("Run")]
    [Tooltip("Начисление очков за убийство обычного врага")]
    public int scorePerKill = 10;

    [Tooltip("Максимальное время забега (сек). 0 = без лимита.")]
    public float maxRunTimeSec = 0f;

    [Header("Debug")]
    [Tooltip("Показывать диагностический HUD в билде")]
    public bool showDiagnosticsHUD = false;
}
