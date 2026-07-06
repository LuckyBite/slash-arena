using System;
using UnityEngine;

/// <summary>
/// Текущее оружие игрока: экипировка, прочность, визуал в руке, подмена аниматора.
/// Кулаки = отсутствие оружия (статы кулаков живут в PlayerAttack.fist).
/// </summary>
public class WeaponHolder : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Сокет в правой руке. Пусто = создадим сами на кости RightHand (модель Humanoid)")]
    [SerializeField] private Transform handSocket;
    [Tooltip("Animator персонажа; пусто — возьмём из ThirdPersonController или детей")]
    [SerializeField] private Animator animator;

    [Header("Хват (локальный оффсет модели оружия в руке — подстрой в инспекторе)")]
    [SerializeField] private Vector3 gripLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 gripLocalEuler = Vector3.zero;

    /// <summary>Экип / поломка / трата прочности — для HUD.</summary>
    public event Action Changed;

    public WeaponDefinition Current { get; private set; }
    public int Durability { get; private set; }
    public bool HasWeapon => Current != null;
    public WeaponConfig CurrentConfig => Current != null ? Current.config : null;

    private GameObject visualInstance;
    private RuntimeAnimatorController baseController;

    private void Awake()
    {
        if (!animator)
        {
            var tpc = GetComponent<ThirdPersonController>();
            animator = (tpc && tpc.animator) ? tpc.animator : GetComponentInChildren<Animator>();
        }
        if (animator) baseController = animator.runtimeAnimatorController;
    }

    private void Start()
    {
        // Сокет не назначен — создаём сами на кости правой кисти (модель Humanoid)
        if (handSocket == null && animator != null && animator.isHuman)
        {
            var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (hand != null)
            {
                var socket = new GameObject("RightHandSocket (auto)");
                socket.transform.SetParent(hand, false);
                handSocket = socket.transform;
            }
            else
            {
                Debug.LogWarning("[Weapon] Кость RightHand не найдена — оружие в руке не появится.");
            }
        }
    }

    public void Equip(WeaponDefinition def)
    {
        if (def == null) return;

        ClearVisual();
        Current = def;
        Durability = Mathf.Max(1, def.maxDurability);

        if (def.handPrefab && handSocket)
        {
            visualInstance = Instantiate(def.handPrefab, handSocket);
            visualInstance.transform.localPosition = gripLocalPosition;
            visualInstance.transform.localRotation = Quaternion.Euler(gripLocalEuler);
        }

        // ВНИМАНИЕ: смена RuntimeAnimatorController сбрасывает текущее состояние аниматора.
        // Делается в момент подбора (не в бою в замахе) — приемлемо.
        if (animator && def.overrideController)
            animator.runtimeAnimatorController = def.overrideController;

        Sfx.Play(def.pickupClip, transform.position);
        Changed?.Invoke();
        Debug.Log($"[Weapon] Взято: {def.displayName} (прочность {Durability})");
    }

    /// <summary>Трата прочности за попадание. Кулаки/кик прочность не тратят (PlayerAttack сам решает).</summary>
    public void ConsumeDurability(int amount)
    {
        if (!HasWeapon || amount <= 0) return;

        Durability -= amount;
        if (Durability <= 0) Break();
        else Changed?.Invoke();
    }

    private void Break()
    {
        if (!HasWeapon) return;

        Sfx.Play(Current.breakClip, transform.position);
        Debug.Log($"[Weapon] Сломалось: {Current.displayName} — снова кулаки");

        ClearVisual();
        Current = null;
        Durability = 0;

        if (animator && baseController)
            animator.runtimeAnimatorController = baseController;

        Changed?.Invoke();
    }

    private void ClearVisual()
    {
        if (visualInstance) Destroy(visualInstance);
        visualInstance = null;
    }
}
