using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays short sound effects from anywhere: Sfx.Play("Click").
/// The sounds are in Assets/Resources/Audio. One hidden AudioSource plays them all,
/// and it survives scene changes, so a sound doesn't cut off when the scene loads.
/// </summary>
public static class Sfx
{
    private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private static AudioSource source;

    public static void Play(string name, float volume = 1f)
    {
        if (source == null)
        {
            var player = new GameObject("Sound Effects");
            Object.DontDestroyOnLoad(player);
            source = player.AddComponent<AudioSource>();
        }

        // Each clip is loaded from Resources once, and then taken from the dictionary.
        if (!clips.TryGetValue(name, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Audio/" + name);
            clips[name] = clip;
        }
        if (clip != null)
        {
            source.PlayOneShot(clip, volume);
        }
    }
}
