using UnityEngine;
using System.Collections;

/// <summary>
/// Camouflage ability that fades renderer colors by changing alpha only.
/// Does NOT modify WorkflowMode, Metallic, Specular, Surface Type,
/// Blend Mode, Render Queue, or any shader settings.
/// Materials must already support transparency.
/// </summary>
public class CamouflageAbility : ActorAbility
{
    [Header("Camouflage Settings")]
    [Range(0f, 1f)]
    public float camouflageAlpha = 0.2f;

    [Min(0f)]
    public float fadeDuration = 0.4f;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFading;

    public override string AbilityName => "Camouflage";
    public override string IconName => "icon_camouflage";

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        CacheOriginalColors();
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                originalColors[i] = renderers[i].material.color;
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    public override void Activate()
    {
        if (isFading)
            return;

        IsActive = !IsActive;

        StopAllCoroutines();

        if (IsActive)
            StartCoroutine(FadeTo(camouflageAlpha));
        else
            StartCoroutine(FadeToOriginal());
    }

    public override void Deactivate()
    {
        StopAllCoroutines();
        RestoreOriginalColors();
        isFading = false;

        base.Deactivate();
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        isFading = true;

        float elapsed = 0f;
        float[] startAlphas = GetCurrentAlphas();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeDuration)
                : 1f;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null)
                    continue;

                Color c = renderers[i].material.color;
                c.a = Mathf.Lerp(startAlphas[i], targetAlpha, t);
                renderers[i].material.color = c;
            }

            yield return null;
        }

        isFading = false;
    }

    private IEnumerator FadeToOriginal()
    {
        isFading = true;

        float elapsed = 0f;
        float[] startAlphas = GetCurrentAlphas();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeDuration)
                : 1f;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].material == null)
                    continue;

                Color c = renderers[i].material.color;
                c.a = Mathf.Lerp(startAlphas[i], originalColors[i].a, t);
                renderers[i].material.color = c;
            }

            yield return null;
        }

        RestoreOriginalColors();
        isFading = false;
    }

    private float[] GetCurrentAlphas()
    {
        float[] alphas = new float[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
                alphas[i] = renderers[i].material.color.a;
            else
                alphas[i] = 1f;
        }

        return alphas;
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].material == null)
                continue;

            renderers[i].material.color = originalColors[i];
        }
    }
}