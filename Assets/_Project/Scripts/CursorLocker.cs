using UnityEngine;
using UnityEngine.InputSystem;

public class CursorLocker : MonoBehaviour
{
    public static bool playerIsAlive = true;

    private InputAction leftClickAction;
    private InputAction escKeyAction;

    void OnEnable()
    {
        leftClickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        escKeyAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/escape");

        leftClickAction.performed += OnLeftClick;
        escKeyAction.performed += OnEsc;

        leftClickAction.Enable();
        escKeyAction.Enable();
    }

    void OnDisable()
    {
        if (leftClickAction != null)
        {
            leftClickAction.performed -= OnLeftClick;
            leftClickAction.Disable();
            leftClickAction.Dispose();
            leftClickAction = null;
        }

        if (escKeyAction != null)
        {
            escKeyAction.performed -= OnEsc;
            escKeyAction.Disable();
            escKeyAction.Dispose();
            escKeyAction = null;
        }
    }

    void Start()
    {
        // Пришли из меню (или запустили сцену напрямую) — сразу лочим курсор,
        // чтобы обзор мышью работал без предварительного клика
        if (playerIsAlive) TryLock();
    }

    private void OnLeftClick(InputAction.CallbackContext ctx) => TryLock();
    private void OnEsc(InputAction.CallbackContext ctx) => UnlockCursor();

    void TryLock()
    {
        if (playerIsAlive && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
