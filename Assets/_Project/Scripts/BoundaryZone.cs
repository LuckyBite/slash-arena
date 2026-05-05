using UnityEngine;
using TMPro;
using System.Collections;

public class BoundaryZone : MonoBehaviour
{
    public GameObject warningPanel;
    public Transform respawnPoint; // точка, куда вернуть игрока
    public float delay = 3f;
    private Coroutine _running;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (_running == null)
                _running = StartCoroutine(HandleBoundary(other.gameObject));
        }
    }

    private IEnumerator HandleBoundary(GameObject player)
    {
        if (respawnPoint == null)
        {
            _running = null;
            yield break;
        }

        if (warningPanel != null)
            warningPanel.SetActive(true);

        // Подождать 3 секунды
        yield return new WaitForSeconds(delay);

        // Спрятать панель
        if (warningPanel != null)
            warningPanel.SetActive(false);

        // Вернуть игрока на точку
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false; // отключить перед перемещением
            player.transform.position = respawnPoint.position;
            controller.enabled = true; // включить обратно
        }
        else
        {
            player.transform.position = respawnPoint.position;
        }

        _running = null;
    }
}
