using UnityEngine;

/// <summary>
/// Фоновая музыка сцены: зациклённый трек с плавным стартом.
/// Трек — слот; пусто = тишина.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    [Tooltip("Трек (слот). Ассеты лежат в Assets/10 Fantasy Medieval Ambient Tracks/")]
    [SerializeField] private AudioClip track;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

    private void Start()
    {
        if (!track) return;
        var src = gameObject.AddComponent<AudioSource>();
        src.clip = track;
        src.loop = true;
        src.volume = volume;
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.Play();
    }
}
