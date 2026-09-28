using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LogoFade : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image logo;
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.6f;

    private IEnumerator Start()
    {
        if (background == null || logo == null)
        {
            Debug.LogError("LogoFade: Background and Logo images are required.", this);
            yield break;
        }

        background.gameObject.SetActive(true);
        logo.gameObject.SetActive(true);
        SetAlpha(background, 1f);
        yield return FadeLogoIn();
        yield return new WaitForSeconds(holdDuration);
        yield return FadeOutTogether();
        background.gameObject.SetActive(false);
    }

    private IEnumerator FadeLogoIn()
    {
        SetAlpha(logo, 0f);

        for (float time = 0f; time < fadeInDuration; time += Time.deltaTime)
        {
            SetAlpha(logo, time / fadeInDuration);
            yield return null;
        }

        SetAlpha(logo, 1f);
    }

    private IEnumerator FadeOutTogether()
    {
        for (float time = 0f; time < fadeOutDuration; time += Time.deltaTime)
        {
            float alpha = 1f - time / fadeOutDuration;
            SetAlpha(background, alpha);
            SetAlpha(logo, alpha);
            yield return null;
        }

        SetAlpha(background, 0f);
        SetAlpha(logo, 0f);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
