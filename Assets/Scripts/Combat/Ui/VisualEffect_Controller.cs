using System.Collections;
using UnityEngine;

public class VisualEffect_Controller : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine currentFlashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // Método para el parpadeo de veneno
    public void FlashPoison()
    {
        if (currentFlashRoutine != null) StopCoroutine(currentFlashRoutine);
        currentFlashRoutine = StartCoroutine(FlashRoutine(new Color(0.6f, 0f, 1f))); // Púrpura
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        // Parpadeo rápido: Original -> Color -> Original
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.15f);
        spriteRenderer.color = originalColor;
    }
}