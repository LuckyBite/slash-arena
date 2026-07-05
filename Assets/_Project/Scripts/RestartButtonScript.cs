using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RestartManager : MonoBehaviour
{
    public Button restartButton;
    private InputAction restartAction;

    void OnEnable()
    {
        restartAction = new InputAction(binding: "<Keyboard>/r");
        restartAction.performed += OnRestart;
        restartAction.Enable();
    }

    void OnDisable()
    {
        if (restartAction != null)
        {
            restartAction.performed -= OnRestart;
            restartAction.Disable();
            restartAction.Dispose();
            restartAction = null;
        }
    }

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);
    }

    void Restart()
    {
        // Единый путь рестарта — через GameManager (сбрасывает timeScale и playerIsAlive)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Restart();
        }
        else
        {
            CursorLocker.playerIsAlive = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnRestart(InputAction.CallbackContext ctx)
    {
        // R работает только на экране game over, чтобы случайное нажатие в бою не сбрасывало забег
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver) return;
        Restart();
    }
}
