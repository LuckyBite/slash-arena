using UnityEngine;

public class EnemyAtack : MonoBehaviour
{
    public int damage = 10;
    public float attackInterval = 1.5f;
    Transform player;
    float timer;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        player = p != null ? p.transform : null;
    }

    void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer >= attackInterval)
        {
            timer = 0f;
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}
