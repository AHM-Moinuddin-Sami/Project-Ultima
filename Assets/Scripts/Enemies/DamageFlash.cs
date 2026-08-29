using System.Collections;
using UnityEngine;

/*
 * DamageFlash
 * -----------
 * Provides simple temporary visual feedback when an object takes damage.
 *
 * Responsibilities:
 * - briefly tint the target's renderers red
 * - restore their original colors afterwards
 *
 * This is temporary gameplay feedback and can later be replaced with
 * proper hit VFX, particles, decals, sound, and camera feedback.
 */

public class DamageFlash : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.1f;

    private Renderer[] renderers;
    private Color[] originalColors;

    private Coroutine flashRoutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    public void Flash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = Color.red;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }

        flashRoutine = null;
    }
}