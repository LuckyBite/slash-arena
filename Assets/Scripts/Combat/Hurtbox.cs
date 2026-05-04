using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int amount);
}

public class Hurtbox : MonoBehaviour
{
    [SerializeField] private Component healthReceiver; // опционально проставь вручную (скрипт со здоровьем)

    public void ApplyHit(int damage, Vector3 hitPoint)
    {
        if (damage <= 0) return;

        if (healthReceiver != null)
        {
            // Интерфейс?
            if (healthReceiver is IDamageable idmg)
            {
                idmg.TakeDamage(damage);
                return;
            }
            // Популярные кейсы
            var mh = healthReceiver as MonoBehaviour;
            if (mh != null)
            {
                var recv = mh.GetComponent<IDamageable>();
                if (recv != null) { recv.TakeDamage(damage); return; }
            }
        }

        // Автопоиск по объекту
        var any = GetComponentInParent<IDamageable>();
        if (any != null) { any.TakeDamage(damage); return; }

        // Фоллбек
        SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}
