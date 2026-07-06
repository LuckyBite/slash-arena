using UnityEngine;

/// <summary>
/// На старте сцены применяет выбранный в гардеробе скин (индекс из сейва).
/// Скин без материала (слот пуст) — молча оставляет вид по умолчанию.
/// </summary>
public class SkinApplier : MonoBehaviour
{
    [Header("Скины (те же ассеты, что в гардеробе — порядок важен)")]
    [SerializeField] private SkinDefinition[] skins;

    [Header("Куда применять")]
    [Tooltip("Рендерер тела; пусто = первый SkinnedMeshRenderer в детях")]
    [SerializeField] private Renderer targetRenderer;

    private void Start()
    {
        if (skins == null || skins.Length == 0) return;

        int idx = Mathf.Clamp(SaveService.Data.selectedSkin, 0, skins.Length - 1);
        var skin = skins[idx];
        if (skin == null || skin.bodyMaterial == null) return;

        if (!targetRenderer) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (!targetRenderer)
        {
            Debug.LogWarning("[SkinApplier] Не нашёл SkinnedMeshRenderer для скина.");
            return;
        }

        targetRenderer.sharedMaterial = skin.bodyMaterial;
        Debug.Log($"[Skin] Применён скин: {skin.displayName}");
    }
}
