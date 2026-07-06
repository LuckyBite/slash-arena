using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Спавн оружия по карте. WaveManager дёргает SpawnForWave() в начале каждой волны —
/// докладывает оружие до maxActive. Без назначенных префабов визуал = кубик-заглушка.
/// </summary>
public class WeaponSpawner : MonoBehaviour
{
    [Header("Что спавнить")]
    [Tooltip("Пул оружия (случайный выбор на каждую точку)")]
    [SerializeField] private WeaponDefinition[] weapons;

    [Header("Где")]
    [Tooltip("Точки спавна (расставь пустышки по карте). Пусто = случайные точки на NavMesh вокруг этого объекта")]
    [SerializeField] private Transform[] points;
    [Tooltip("Радиус случайного спавна, если точки не заданы")]
    [SerializeField] private float spawnRadius = 14f;

    [Header("Сколько")]
    [Tooltip("Максимум одновременно лежащего на карте оружия")]
    [SerializeField] private int maxActive = 3;

    [SerializeField] private bool debugLog = false;

    private readonly List<WeaponPickup> active = new List<WeaponPickup>(8);

    /// <summary>Долить оружие на карту до maxActive (вызывается в начале волны).</summary>
    public void SpawnForWave(int wave)
    {
        PruneDead();
        int need = maxActive - active.Count;
        for (int i = 0; i < need; i++) SpawnOne();

        if (debugLog)
            Debug.Log($"[WeaponSpawner] Волна {wave}: оружия на карте {active.Count}");
    }

    private void SpawnOne()
    {
        if (weapons == null || weapons.Length == 0) return;
        var def = weapons[Random.Range(0, weapons.Length)];
        if (!def) return;

        if (!TryPickPosition(out Vector3 pos)) return;

        var go = new GameObject($"WeaponPickup_{def.id}");
        go.transform.position = pos + Vector3.up * 0.6f;

        // Визуал: префаб оружия или кубик-заглушка, пока ассеты не назначены
        GameObject visual;
        if (def.handPrefab)
        {
            visual = Instantiate(def.handPrefab, go.transform);
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.35f;
            var vc = visual.GetComponent<Collider>();
            if (vc) Destroy(vc); // визуал не должен ловить физику
        }
        visual.transform.localPosition = Vector3.zero;

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.1f;

        var pickup = go.AddComponent<WeaponPickup>();
        pickup.definition = def;
        pickup.Consumed += OnConsumed;
        active.Add(pickup);
    }

    private void OnConsumed(WeaponPickup p) => active.Remove(p);

    private void PruneDead()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] == null) active.RemoveAt(i);
    }

    private bool TryPickPosition(out Vector3 pos)
    {
        if (points != null && points.Length > 0)
        {
            var t = points[Random.Range(0, points.Length)];
            if (t) { pos = t.position; return true; }
        }

        // Случайная точка на NavMesh вокруг спавнера
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 c = Random.insideUnitCircle * spawnRadius;
            Vector3 probe = transform.position + new Vector3(c.x, 0f, c.y);
            if (NavMesh.SamplePosition(probe, out var hit, 3f, NavMesh.AllAreas))
            {
                pos = hit.position;
                return true;
            }
        }

        pos = Vector3.zero;
        return false;
    }
}
