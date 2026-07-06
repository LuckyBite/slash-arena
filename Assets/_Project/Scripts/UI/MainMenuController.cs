using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Главное меню: Играть / Гардероб / Выход. Весь UI строится кодом в Start —
/// сцене MainMenu не нужны ассеты вообще. Позже можно заменить на «красивый» Canvas,
/// собранный руками, — логика останется той же.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Сцены")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Гардероб")]
    [Tooltip("Список скинов (те же ассеты, что в SkinApplier на игроке — порядок важен)")]
    [SerializeField] private SkinDefinition[] skins;

    [Header("Стиль")]
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.10f);
    [SerializeField] private Color buttonColor = new Color(0.17f, 0.17f, 0.21f);
    [SerializeField] private Color accentColor = new Color(0.85f, 0.30f, 0.20f);

    [Header("Музыка (слот)")]
    [SerializeField] private AudioClip music;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;

    private GameObject mainPanel;
    private GameObject wardrobePanel;
    private TMP_Text skinLabel;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        EnsureCamera();
        EnsureEventSystem();

        try
        {
            BuildUi();
            Debug.Log("[Menu] UI построен");
        }
        catch (System.Exception e)
        {
            // Если UI не построился — покажи мне этот лог, починю точечно
            Debug.LogError($"[Menu] Ошибка построения UI: {e}");
        }

        if (music)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = music;
            src.loop = true;
            src.volume = musicVolume;
            src.Play();
        }
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        camGo.AddComponent<AudioListener>();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>(); // проект на новом Input System
    }

    private void BuildUi()
    {
        // Canvas
        var canvasGo = new GameObject("MenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Фон
        var bg = MakeRect("Background", canvasGo.transform);
        Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = backgroundColor;

        // Заголовок
        var title = MakeText(canvasGo.transform, "SLASH ARENA", 96, FontStyles.Bold);
        Place(title.rectTransform, 0, 320, 1200, 120);
        title.color = accentColor;

        // Рекорд
        var save = SaveService.Data;
        string recordLine = save.bestScore > 0
            ? $"Рекорд: {save.bestScore} очков · волна {save.bestWave}"
            : "Рекордов пока нет — самое время!";
        var record = MakeText(canvasGo.transform, recordLine, 32, FontStyles.Normal);
        Place(record.rectTransform, 0, 210, 1200, 50);
        record.color = new Color(1f, 1f, 1f, 0.6f);

        // Главная панель
        mainPanel = MakeRect("MainPanel", canvasGo.transform).gameObject;
        Stretch(mainPanel.GetComponent<RectTransform>());
        MakeButton(mainPanel.transform, "Играть", 40, StartGame);
        MakeButton(mainPanel.transform, "Гардероб", -60, OpenWardrobe);
        MakeButton(mainPanel.transform, "Выход", -160, QuitGame);

        // Панель гардероба
        wardrobePanel = MakeRect("WardrobePanel", canvasGo.transform).gameObject;
        Stretch(wardrobePanel.GetComponent<RectTransform>());

        var wardrobeTitle = MakeText(wardrobePanel.transform, "Гардероб", 56, FontStyles.Bold);
        Place(wardrobeTitle.rectTransform, 0, 120, 800, 80);

        skinLabel = MakeText(wardrobePanel.transform, "", 44, FontStyles.Normal);
        Place(skinLabel.rectTransform, 0, 30, 700, 70);

        MakeSmallButton(wardrobePanel.transform, "<", -420, 30, () => CycleSkin(-1));
        MakeSmallButton(wardrobePanel.transform, ">", 420, 30, () => CycleSkin(+1));
        MakeButton(wardrobePanel.transform, "Назад", -140, CloseWardrobe);

        var hint = MakeText(wardrobePanel.transform, "Скин применится в игре (материалы скинов назначаются в ассетах Data/Skins)", 24, FontStyles.Italic);
        Place(hint.rectTransform, 0, -50, 1100, 40);
        hint.color = new Color(1f, 1f, 1f, 0.45f);

        wardrobePanel.SetActive(false);
        RefreshSkinLabel();
    }

    // ===== Кнопки меню =====
    private void StartGame()
    {
        CursorLocker.playerIsAlive = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenWardrobe()
    {
        mainPanel.SetActive(false);
        wardrobePanel.SetActive(true);
        RefreshSkinLabel();
    }

    private void CloseWardrobe()
    {
        wardrobePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private void QuitGame()
    {
        Application.Quit();
        Debug.Log("[Menu] Выход (в редакторе игнорируется)");
    }

    // ===== Гардероб =====
    private void CycleSkin(int dir)
    {
        if (skins == null || skins.Length == 0) return;
        int idx = SaveService.Data.selectedSkin;
        idx = (idx + dir + skins.Length) % skins.Length;
        SaveService.Data.selectedSkin = idx;
        SaveService.Save();
        RefreshSkinLabel();
    }

    private void RefreshSkinLabel()
    {
        if (skinLabel == null) return;
        if (skins == null || skins.Length == 0)
        {
            skinLabel.text = "Скины не настроены";
            return;
        }
        int idx = Mathf.Clamp(SaveService.Data.selectedSkin, 0, skins.Length - 1);
        var s = skins[idx];
        skinLabel.text = s ? $"{s.displayName}  ({idx + 1}/{skins.Length})" : "—";
    }

    // ===== UI-фабрика =====
    private static RectTransform MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static TMP_Text MakeText(Transform parent, string text, float size, FontStyles style)
    {
        var rt = MakeRect("Text_" + text, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private void MakeButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect("Button_" + label, parent);
        Place(rt, 0, y, 380, 72);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = buttonColor;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var text = MakeText(rt, label, 36, FontStyles.Bold);
        Stretch(text.rectTransform);
    }

    private void MakeSmallButton(Transform parent, string label, float x, float y, UnityEngine.Events.UnityAction onClick)
    {
        var rt = MakeRect("Button_" + label, parent);
        Place(rt, x, y, 80, 80);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = buttonColor;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var text = MakeText(rt, label, 44, FontStyles.Bold);
        Stretch(text.rectTransform);
    }
}
