using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Сервис спавна врагов: точки + префабы. КОГДА спавнить — решает WaveManager (D8).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Points")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("Включить лог в консоль при спавне")]
    [SerializeField] private bool debugLog = false;

    /// <summary>
    /// Заспавнить одного врага с множителем HP (сложность волны). Возвращает объект или null.
    /// </summary>
    public GameObject SpawnOne(float hpMultiplier)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning($"[EnemySpawner] {name}: пустой массив enemyPrefabs");
            return null;
        }
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"[EnemySpawner] {name}: пустой массив spawnPoints");
            return null;
        }

        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (!prefab || !point)
        {
            Debug.LogWarning($"[EnemySpawner] {name}: null-элемент в enemyPrefabs/spawnPoints");
            return null;
        }

        Vector3 pos = point.position;
        if (TryFindNavmeshPosition(pos, 2.0f, out var navPos))
            pos = navPos;

        var go = Instantiate(prefab, pos, point.rotation);

        // Масштабируем HP сразу после Instantiate (до первого кадра)
        if (hpMultiplier > 1f)
        {
            var eh = go.GetComponent<EnemyHealth>();
            if (eh) eh.ApplyHealthMultiplier(hpMultiplier);
        }

        if (debugLog)
            Debug.Log($"[EnemySpawner] Spawned: {go.name} at {pos} (hp x{hpMultiplier:F2})");

        return go;
    }

    private bool TryFindNavmeshPosition(Vector3 origin, float range, out Vector3 result)
    {
        if (NavMesh.SamplePosition(origin, out var hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }
        result = origin;
        return false;
    }
}
