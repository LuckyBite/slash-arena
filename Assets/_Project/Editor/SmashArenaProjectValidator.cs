// Tools ▸ SmashArena ▸ Validate Setup
// Нужен UnityEditor
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SmashArenaProjectValidator : EditorWindow
{
    [MenuItem("Tools/SmashArena/Validate Setup")]
    public static void Validate()
    {
        Debug.Log("=== SmashArena Validate Setup ===");

        // --- Player ---
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player) { Debug.LogError("Player (tag=Player) не найден."); return; }

        var anim = player.GetComponentInChildren<Animator>();
        var cc   = player.GetComponent<CharacterController>();
        var tpc  = player.GetComponent("ThirdPersonController");
        var pia  = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (!anim) Debug.LogError("У Player нет Animator.");
        if (!cc)   Debug.LogError("У Player нет CharacterController.");
        if (!tpc)  Debug.LogWarning("ThirdPersonController не найден (проверь название скрипта/объект).");
        if (!pia)  Debug.LogWarning("PlayerInput не найден.");

        // Animator params — ожидаемые (по твоему проекту)
        string[] playerParams =
        {
            "Speed","MoveX","MoveY","isMoving","isGrounded",
            "Attack_Light","Attack_Heavy","Kick","Block","Dodge"
        };
        if (anim)
        {
            var names = anim.parameters.Select(p => p.name).ToHashSet();
            foreach (var p in playerParams)
                if (!names.Contains(p))
                    Debug.LogWarning($"[Player Animator] Нет параметра: {p}");
        }

        // GroundCheck / groundMask
        var groundCheck = player.transform.Find("GroundCheck");
        if (!groundCheck) Debug.LogWarning("У Player нет трансформа GroundCheck (дочерний). Создай пустой объект у стоп.");

        if (tpc)
        {
            var so = new SerializedObject(player.GetComponent(tpc.GetType()));
            var maskProp = so.FindProperty("groundMask");
            if (maskProp != null)
            {
                int mask = maskProp.intValue;
                bool hasPlayer = (mask & (1 << LayerMask.NameToLayer("Player"))) != 0;
                bool hasEnemy  = (mask & (1 << LayerMask.NameToLayer("Enemy")))  != 0;
                if (hasPlayer || hasEnemy)
                    Debug.LogError("[ThirdPersonController] groundMask включает Player/Enemy. ДОЛЖЕН быть только Ground.");
            }
        }

        // Input map базовые действия
        if (pia)
        {
            string[] actions = { "Move", "Sprint", "Attack_Light", "Attack_Heavy", "Kick", "Block", "Dodge" };
            foreach (var a in actions)
                if (pia.actions.FindAction(a, throwIfNotFound:false) == null)
                    Debug.LogError($"[PlayerInput] Нет action: {a}");
        }

        // --- Enemies ---
        var enemyPrefabs = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
            .Where(go => go && go.name.ToLower().Contains("enemy"))
            .ToArray();

        foreach (var e in enemyPrefabs)
        {
            var eAnim = e.GetComponentInChildren<Animator>();
            if (!eAnim) { Debug.LogWarning($"[Enemy Prefab] {e.name} без Animator"); continue; }

            var pnames = eAnim.parameters.Select(p => p.name).ToHashSet();
            if (!pnames.Contains("Speed"))
                Debug.LogWarning($"[Enemy Animator] {e.name}: нет параметра 'Speed' (float) — проверяй контроллер.");
            // Триггер атаки не обязателен — только предупреждение, если его ожидает AI
        }

        Debug.Log("=== Validate done ===");
    }
}
