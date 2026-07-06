// Tools ▸ SmashArena ▸ Play From MainMenu (галка)
// Включено: кнопка Play в редакторе всегда стартует со сцены MainMenu,
// какая бы сцена ни была открыта. Выключено: обычное поведение Unity
// (Play запускает ТЕКУЩУЮ открытую сцену — поэтому меню «не видно», когда открыта арена).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayFromMainMenu
{
    private const string MenuPath = "Tools/SmashArena/Play From MainMenu";
    private const string PrefKey = "SmashArena.PlayFromMainMenu";
    private const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        Apply();
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, false));
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
        return true;
    }

    private static void Apply()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorSceneManager.playModeStartScene = scene;
            if (!scene) Debug.LogWarning($"[PlayFromMainMenu] Сцена не найдена: {ScenePath}");
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
