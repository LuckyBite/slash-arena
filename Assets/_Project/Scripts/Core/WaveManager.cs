using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Бесконечные волны (D8): считает волну, наращивает количество и HP врагов,
/// даёт паузу между волнами, дёргает спавнеры врагов и оружия.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Размер волн")]
    [Tooltip("Врагов в первой волне")]
    [SerializeField] private int baseEnemies = 4;
    [Tooltip("Прирост врагов за волну")]
    [SerializeField] private int enemiesGrowthPerWave = 2;
    [Tooltip("Потолок врагов в волне (перф-предохранитель)")]
    [SerializeField] private int maxEnemiesPerWave = 40;

    [Header("Сложность")]
    [Tooltip("Прирост HP врагов за волну (0.12 = +12% за волну)")]
    [SerializeField] private float hpGrowthPerWave = 0.12f;

    [Header("Темп")]
    [Tooltip("Пауза между волнами (сек)")]
    [SerializeField] private float intermissionSeconds = 6f;
    [Tooltip("Интервал между спавнами внутри волны (сек)")]
    [SerializeField] private float spawnInterval = 0.6f;

    [Header("Очки")]
    [Tooltip("Бонус за волну = это значение × номер волны")]
    [SerializeField] private int waveClearBonusBase = 25;

    [Header("Refs (пусто = найдём сами при старте)")]
    [SerializeField] private EnemySpawner[] spawners;
    [SerializeField] private WeaponSpawner weaponSpawner;

    [Tooltip("Подробный лог волн (диагностика). Выключишь, когда волны устаканятся")]
    [SerializeField] private bool debugLog = true;

    public int CurrentWave { get; private set; }

    private readonly List<EnemyHealth> alive = new List<EnemyHealth>(64);

    private void Awake()
    {
        if (spawners == null || spawners.Length == 0)
            spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        if (!weaponSpawner)
            weaponSpawner = FindFirstObjectByType<WeaponSpawner>();
    }

    private void Start()
    {
        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogError("[WaveManager] В сцене нет EnemySpawner — волны не стартуют.");
            enabled = false;
            return;
        }
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        CurrentWave = 0;
        var pollWait = new WaitForSeconds(0.25f);

        while (true)
        {
            CurrentWave++;

            // Объявление волны + доложить оружие на карту
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReportWaveReached(CurrentWave);
                GameManager.Instance.SetWaveLabel($"Волна {CurrentWave}");
            }
            if (weaponSpawner)
            {
                try { weaponSpawner.SpawnForWave(CurrentWave); }
                catch (System.Exception e) { Debug.LogError($"[WaveManager] Ошибка спавна оружия (волны продолжаются): {e}"); }
            }

            int count = Mathf.Min(baseEnemies + enemiesGrowthPerWave * (CurrentWave - 1), maxEnemiesPerWave);
            float hpMult = 1f + hpGrowthPerWave * (CurrentWave - 1);

            if (debugLog)
                Debug.Log($"[WaveManager] Волна {CurrentWave}: план {count} врагов, HP x{hpMult:F2}");

            // Спавним волну порциями, спавнеры по кругу.
            // try/catch на каждом спавне: одна ошибка не должна убивать цикл волн навсегда.
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var spawner = spawners[i % spawners.Length];
                    var go = spawner ? spawner.SpawnOne(hpMult) : null;
                    if (go)
                    {
                        spawned++;
                        var eh = go.GetComponent<EnemyHealth>();
                        if (eh)
                        {
                            alive.Add(eh);
                            eh.Died += OnEnemyDied;
                        }
                    }
                    else if (debugLog)
                    {
                        Debug.LogWarning($"[WaveManager] Спавнер #{i % spawners.Length} не заспавнил врага (см. лог EnemySpawner)");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[WaveManager] Ошибка спавна: {e}");
                }
                yield return new WaitForSeconds(spawnInterval);
            }

            if (debugLog)
                Debug.Log($"[WaveManager] Волна {CurrentWave}: заспавнено {spawned}/{count}, живых {AliveCount()}");

            // Ждём зачистки волны
            while (AliveCount() > 0)
                yield return pollWait;

            if (debugLog)
                Debug.Log($"[WaveManager] Волна {CurrentWave} зачищена");

            // Бонус за волну и передышка
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(waveClearBonusBase * CurrentWave);
                GameManager.Instance.SetWaveLabel($"Волна {CurrentWave + 1} через {Mathf.RoundToInt(intermissionSeconds)} сек…");
            }

            yield return new WaitForSeconds(intermissionSeconds);
        }
    }

    private void OnEnemyDied(EnemyHealth eh)
    {
        eh.Died -= OnEnemyDied;
        alive.Remove(eh);
    }

    // Подчищаем возможные «тихие» уничтожения (без события Died)
    private int AliveCount()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            var eh = alive[i];
            if (eh == null || eh.IsDead) alive.RemoveAt(i);
        }
        return alive.Count;
    }
}
