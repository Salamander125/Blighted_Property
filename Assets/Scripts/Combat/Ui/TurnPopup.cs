using System.Collections;
using TMPro;
using UnityEngine;

public class TurnPopup : MonoBehaviour
{
    [Header("Prefabs Independientes")]
    [SerializeField] private TextMeshPro textPrefab;       // El prefab de texto suelto
    [SerializeField] private SpriteRenderer bgPrefab;      // El prefab del fondo suelto

    [Header("Ajustes")]
    [SerializeField] private float scaleTime = 0.2f;
    [SerializeField] private float lifeTime = 1.2f;

    private TextMeshPro instantiatedText;
    private SpriteRenderer instantiatedBg;

    public float TotalDuration => lifeTime + scaleTime;

    public void Setup(string message)
    {
        // 1. Instanciar visuales como hijos
        if (textPrefab != null)
        {
            instantiatedText = Instantiate(textPrefab, transform);
            instantiatedText.gameObject.SetActive(true);
            instantiatedText.text = message;
            instantiatedText.color = new Color(instantiatedText.color.r, instantiatedText.color.g, instantiatedText.color.b, 1f);
        }

        if (bgPrefab != null)
        {
            instantiatedBg = Instantiate(bgPrefab, transform);
            instantiatedBg.gameObject.SetActive(true);
            instantiatedBg.color = new Color(instantiatedBg.color.r, instantiatedBg.color.g, instantiatedBg.color.b, 1f);
        }

        transform.localScale = Vector3.zero;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        // Aparecer
        float t = 0;
        while (t < scaleTime)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(0, 1.5f, t / scaleTime);
            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(lifeTime * 0.5f);

        // Desvanecer
        t = 0;
        float fadeDur = lifeTime * 0.5f;
        while (t < fadeDur)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDur);
            if (instantiatedText != null) instantiatedText.alpha = alpha;
            if (instantiatedBg != null)
            {
                Color c = instantiatedBg.color;
                c.a = alpha;
                instantiatedBg.color = c;
            }
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}