using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Prefabs Independientes")]
    [SerializeField] private TextMeshPro textPrefab;

    [Header("Ajustes")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float duration = 1f;

    public float DamageLifeTime => duration;
    private TextMeshPro instantiatedText;

    // Ahora acepta un color opcional (Color.white por defecto)
    public void Setup(string textValue, Color? customColor = null)
    {
        if (textPrefab != null)
        {
            instantiatedText = Instantiate(textPrefab, transform);
            instantiatedText.gameObject.SetActive(true);
            instantiatedText.text = textValue;

            // Aplicamos el color: si es nulo, se queda el del prefab o blanco
            Color targetColor = customColor ?? instantiatedText.color;
            targetColor.a = 1f; // Aseguramos que sea visible
            instantiatedText.color = targetColor;
        }

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float t = 0;
        while (t < duration)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            if (instantiatedText != null)
            {
                Color c = instantiatedText.color;
                c.a = Mathf.Lerp(1f, 0f, t / duration);
                instantiatedText.color = c;
            }

            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}