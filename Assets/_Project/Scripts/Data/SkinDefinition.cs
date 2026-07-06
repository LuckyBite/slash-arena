using UnityEngine;

/// <summary>
/// Скин персонажа как данные (D1). Один .asset на скин в Assets/_Project/Data/Skins/.
/// Пока скин = материал тела (перекраска палитры Blink); позже можно добавить меш/префаб.
/// </summary>
[CreateAssetMenu(fileName = "Skin_", menuName = "Slash Arena/Skin Definition", order = 2)]
public class SkinDefinition : ScriptableObject
{
    public string displayName = "Скин";

    [Tooltip("Материал тела (слот — сделай вариант палитры Blink: Body_Red.mat и т.п.). Пусто = не менять")]
    public Material bodyMaterial;

    [Tooltip("Превью для гардероба (слот)")]
    public Sprite preview;
}
