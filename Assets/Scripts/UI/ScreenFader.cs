using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Changes scenes with a fade: the screen goes black, the new scene loads, and the black fades away.
/// Call ScreenFader.FadeAndLoad("Landfill"). The black screen survives the scene change (DontDestroyOnLoad).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const float FadeOutSeconds = 0.45f;
    private const float FadeInSeconds = 0.6f;

    private static bool isFading;

    // Domain reload is off in the project settings, so a static keeps its value from the last Play.
    // If Play was stopped in the middle of a fade, isFading would stay true and block every fade after it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        isFading = false;
    }

    public static void FadeAndLoad(string scene)
    {
        if (isFading)
        {
            return; // A second call during a fade (like a double click on Play again) would load the scene twice.
        }
        isFading = true;

        Canvas canvas = UiFactory.CreateCanvas("Screen Fader", UiTheme.FaderLayer);
        DontDestroyOnLoad(canvas.gameObject);
        var black = UiFactory.CreateImage(canvas.transform, "Black", null, Color.black);
        UiFactory.Stretch(black.rectTransform);
        black.raycastTarget = true; // Blocks clicks during the fade.

        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        canvas.gameObject.AddComponent<ScreenFader>().StartCoroutine(FadeRoutine(group, scene));
    }

    private static IEnumerator FadeRoutine(CanvasGroup group, string scene)
    {
        yield return UiAnim.Fade(group, 1f, FadeOutSeconds);
        yield return SceneManager.LoadSceneAsync(scene);
        isFading = false;
        yield return UiAnim.Fade(group, 0f, FadeInSeconds);
        ObjectRemoval.Remove(group.gameObject);
    }
}
