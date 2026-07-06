using UnityEngine;

// Простейший one-shot проигрыватель звуков.
// Все клипы в проекте — СЛОТЫ: пока клип не назначен в Inspector, вызов молча ничего не делает.
// TODO (этап 5): громкость из настроек, пул AudioSource вместо PlayClipAtPoint.
public static class Sfx
{
    public static void Play(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (!clip) return;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }

    // Случайный клип из набора (вариативность шагов/ударов)
    public static void PlayRandom(AudioClip[] clips, Vector3 pos, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        Play(clips[Random.Range(0, clips.Length)], pos, volume);
    }
}
