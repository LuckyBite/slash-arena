using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Fist, Sword /* потом: Greatsword, Mace, Axe, ... */ }
public enum AttackStrength { Light, Heavy, Kick } // Kick не зависит от оружия — бьём ногой

[System.Serializable]
public struct HitShape
{
    public int damage;
    public float range;
    public float radius;
    public float verticalOffset;

    public HitShape(int dmg, float rng, float rad, float y)
    {
        damage = dmg; range = rng; radius = rad; verticalOffset = y;
    }
}

[System.Serializable]
public class WeaponConfig
{
    public HitShape light = new HitShape(10, 0.8f, 0.35f, 1.2f);
    public HitShape heavy = new HitShape(16, 1.0f, 0.45f, 1.1f);
}

public class PlayerAttack : MonoBehaviour
{
    [Header("Origin & Mask")]
    public Transform hitOrigin;
    public LayerMask enemyLayer;

    [Header("Weapon Profiles")]
    [Tooltip("Статы БЕЗ оружия. Статы оружия живут в WeaponDefinition-ассетах")]
    public WeaponConfig fist = new WeaponConfig();

    [Header("Kick (общий для всех оружий)")]
    [Tooltip("Хитшейп удара ногой — не зависит от оружия в руках")]
    public HitShape kick = new HitShape(12, 1.0f, 0.4f, 0.9f);

    [Header("Timing")]
    [Tooltip("Если ON — урон ждёт Animation Event (AnimEvent_DoHit). Если OFF — по нажатию/окну.")]
    public bool useAnimationEvents = false; // для окон стейта оставляем OFF

    [Header("SFX (слоты — назначь клипы)")]
    [Tooltip("Свист лёгкого удара")]
    [SerializeField] private AudioClip swingLightClip;
    [Tooltip("Свист тяжёлого удара")]
    [SerializeField] private AudioClip swingHeavyClip;
    [Tooltip("Свист кика")]
    [SerializeField] private AudioClip swingKickClip;
    [Tooltip("Звук попадания по врагу")]
    [SerializeField] private AudioClip hitImpactClip;

    [Header("Hitstop")]
    [Tooltip("Микро-заморозка времени при попадании (0 = выкл)")]
    [SerializeField] private float hitstopSeconds = 0.05f;
    [Tooltip("Хитстоп только на тяжёлых ударах")]
    [SerializeField] private bool hitstopHeavyOnly = true;

    [Header("Debug")]
    public bool drawGizmos = false;
    public bool debugLog   = false;

    // Animation Events (когда useAnimationEvents = true)
    private bool pendingHit;
    private HitShape pendingShape;
    private AttackStrength pendingStrength;
    private bool hitstopActive;

    // буфер под OverlapSphere, чтобы не аллоцировать каждый раз
    private const int MaxHits = 32;
    private readonly Collider[] _hits = new Collider[MaxHits];

    // де-дупликация врагов за один свинг
    private readonly HashSet<int> _damagedIds = new HashSet<int>();

    // Текущее оружие (может отсутствовать — тогда кулаки)
    private WeaponHolder holder;

    private void Awake()
    {
        holder = GetComponent<WeaponHolder>();
    }

    private void Start()
    {
        if (hitOrigin == null) hitOrigin = transform;
        int enemyIdx = LayerMask.NameToLayer("Enemy");
        if ((enemyLayer.value & (1 << enemyIdx)) == 0)
            Debug.LogWarning("[PlayerAttack] Enemy layer НЕ включён в маску enemyLayer.");
    }

    // Вызывается контроллером по кнопке (если нужно поведение «по нажатию/эвенту»)
    public void Attack(WeaponType weapon, AttackStrength strength)
    {
        var shape = SelectShape(weapon, strength);
        if (useAnimationEvents)
        {
            pendingShape = shape;
            pendingStrength = strength;
            pendingHit = true;
        }
        else
        {
            ImmediateAttack(weapon, strength);
        }
    }

    public void ImmediateAttack(WeaponType weapon, AttackStrength strength)
    {
        var shape = SelectShape(weapon, strength);
        DoHit(shape, strength);
    }

    // Вызывается AttackStateWindow в момент старта стейта атаки — свист замаха
    public void OnSwingStarted(AttackStrength strength)
    {
        Vector3 pos = hitOrigin ? hitOrigin.position : transform.position;

        // У оружия может быть свой свист (кроме кика — это нога)
        if (strength != AttackStrength.Kick && holder != null && holder.HasWeapon && holder.Current.swingClip)
        {
            Sfx.Play(holder.Current.swingClip, pos);
            return;
        }

        switch (strength)
        {
            case AttackStrength.Heavy: Sfx.Play(swingHeavyClip, pos); break;
            case AttackStrength.Kick:  Sfx.Play(swingKickClip,  pos); break;
            default:                   Sfx.Play(swingLightClip, pos); break;
        }
    }

    private HitShape SelectShape(WeaponType weapon, AttackStrength strength)
    {
        if (strength == AttackStrength.Kick) return kick;

        // Статы берём из текущего WeaponDefinition; без оружия — кулаки.
        // (enum weapon оставлен для совместимости с настройками AttackStateWindow)
        WeaponConfig cfg;
        if (holder != null && holder.HasWeapon) cfg = holder.CurrentConfig;
        else cfg = fist;

        return strength == AttackStrength.Light ? cfg.light : cfg.heavy;
    }

    // Animation Event
    public void AnimEvent_DoHit()
    {
        if (!pendingHit) return;
        DoHit(pendingShape, pendingStrength);
        pendingHit = false;
    }

    private void DoHit(HitShape shape, AttackStrength strength)
    {
        Transform origin = hitOrigin ? hitOrigin : transform;
        Vector3 center = origin.position + origin.forward * shape.range + Vector3.up * shape.verticalOffset;

        _damagedIds.Clear(); // новый свинг — чистим

        int count = Physics.OverlapSphereNonAlloc(center, shape.radius, _hits, enemyLayer, QueryTriggerInteraction.Collide);

        if (debugLog)
            Debug.Log($"[PlayerAttack] Hit: dmg={shape.damage}, r={shape.range}, R={shape.radius}, y={shape.verticalOffset}, found={count}");

        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col) continue;

            // Бьём любого, кто умеет получать урон (IDamageable), а не только EnemyHealth
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg == null) continue;

            var comp = dmg as Component;
            int id = comp ? comp.GetInstanceID() : dmg.GetHashCode();
            if (_damagedIds.Contains(id)) continue; // тот же враг уже получал урон этим свингом

            _damagedIds.Add(id);
            dmg.TakeDamage(shape.damage);
        }

        if (_damagedIds.Count > 0)
            OnHitLanded(center, strength);
    }

    // Попали хотя бы по одному врагу этим свингом
    private void OnHitLanded(Vector3 at, AttackStrength strength)
    {
        // Звук попадания: у оружия приоритет
        AudioClip impact = hitImpactClip;
        if (strength != AttackStrength.Kick && holder != null && holder.HasWeapon && holder.Current.hitClip)
            impact = holder.Current.hitClip;
        Sfx.Play(impact, at);

        // Прочность: тратится только попаданиями оружием (кик/кулаки бесплатны)
        if (strength != AttackStrength.Kick && holder != null && holder.HasWeapon)
            holder.ConsumeDurability(holder.Current.durabilityLossPerHit);

        bool allowHitstop = hitstopSeconds > 0f && (!hitstopHeavyOnly || strength == AttackStrength.Heavy);
        if (allowHitstop && !hitstopActive)
            StartCoroutine(HitstopRoutine());
    }

    private System.Collections.IEnumerator HitstopRoutine()
    {
        hitstopActive = true;
        float prev = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitstopSeconds);
        // Не воскрешаем время, если игра успела встать на паузу game over
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            Time.timeScale = prev;
        hitstopActive = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Transform origin = hitOrigin ? hitOrigin : transform;

        Vector3 c1 = origin.position + origin.forward * fist.light.range + Vector3.up * fist.light.verticalOffset;
        Vector3 c2 = origin.position + origin.forward * fist.heavy.range + Vector3.up * fist.heavy.verticalOffset;

        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(c1, fist.light.radius);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(c2, fist.heavy.radius);
    }
#endif
}
