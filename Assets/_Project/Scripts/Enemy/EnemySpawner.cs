using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Points")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Progression")]
    [Tooltip("Сколько минут до достижения максимальной сложности")]
    [SerializeField] private float minutesToMaxDifficulty = 12f;

    [Tooltip("Стартовый лимит живых врагов")]
    [SerializeField] private int baseMaxAlive = 4;

    [Tooltip("Максимальный лимит живых врагов на пике")]
    [SerializeField] private int maxMaxAlive = 35;

    [Tooltip("Стартовый интервал между попытками спавна (сек)")]
    [SerializeField] private float baseSpawnInterval = 4.0f;

    [Tooltip("Минимальный интервал на пике сложности (сек)")]
    [SerializeField] private float minSpawnInterval = 1.0f;

    [Tooltip("Максимальный размер батча (за один тик)")]
    [SerializeField] private int maxBatchSize = 3;

    [Tooltip("Включить лог в консоль при спавне")]
    [SerializeField] private bool debugLog = false;

    // runtime
    private readonly List<GameObject> _alive = new List<GameObject>(128);
    private float _startTime;
    private Coroutine _loop;
    private Coroutine _cleanupLoop;

    private void OnEnable()
    {
        _startTime = Time.time;
        if (_loop == null) _loop = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;

        if (_cleanupLoop != null) StopCoroutine(_cleanupLoop);
        _cleanupLoop = null;
    }

    private IEnumerator SpawnLoop()
    {
        // Простой фоновый клинер, чтобы убирать null из списка
        if (_cleanupLoop == null) _cleanupLoop = StartCoroutine(CleanupLoop());

        while (true)
        {
            float progress = GetDifficultyProgress01();
            int currentMaxAlive = Mathf.RoundToInt(Mathf.Lerp(baseMaxAlive, maxMaxAlive, progress));
            float currentInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);

            PruneNulls();

            int aliveCount = _alive.Count;
            int deficit = Mathf.Max(0, currentMaxAlive - aliveCount);

            if (deficit > 0)
            {
                int batch = Mathf.Clamp(deficit, 1, maxBatchSize);
                SpawnBatch(batch);
            }

            yield return new WaitForSeconds(currentInterval);
        }
    }

    private float GetDifficultyProgress01()
    {
        if (minutesToMaxDifficulty <= 0.01f) return 1f;
        float elapsedMin = (Time.time - _startTime) / 60f;
        return Mathf.Clamp01(elapsedMin / minutesToMaxDifficulty);
    }

    private void SpawnBatch(int count)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        for (int i = 0; i < count; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Vector3 pos = point.position;
            if (TryFindNavmeshPosition(pos, 2.0f, out var navPos))
                pos = navPos;

            var go = Instantiate(prefab, pos, point.rotation);
            _alive.Add(go);

            if (debugLog)
                Debug.Log($"[EnemySpawner] Spawned: {go.name} at {pos} (alive={_alive.Count})");
        }
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

    private IEnumerator CleanupLoop()
    {
        var wait = new WaitForSeconds(2f);
        while (true)
        {
            PruneNulls();
            yield return wait;
        }
    }

    private void PruneNulls()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null)
                _alive.RemoveAt(i);
        }
    }
}
