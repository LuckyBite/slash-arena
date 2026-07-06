using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Данные сейва. Расширяем полями по мере надобности — JsonUtility стерпит старые файлы
/// (отсутствующие поля получат значения по умолчанию).
/// </summary>
[Serializable]
public class SaveData
{
    public int bestScore;
    public int bestWave;
    public int selectedSkin;
    public string nickname = "";
}

/// <summary>
/// Сейвы — JSON в Application.persistentDataPath (решение D3). Не PlayerPrefs.
/// Использование: SaveService.Data.bestScore = 10; SaveService.Save();
/// </summary>
public static class SaveService
{
    private static SaveData cache;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static SaveData Data
    {
        get
        {
            if (cache == null) Load();
            return cache;
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                cache = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveService] Не удалось прочитать сейв: {e.Message}");
        }
        if (cache == null) cache = new SaveData();
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveService] Не удалось записать сейв: {e.Message}");
        }
    }
}
