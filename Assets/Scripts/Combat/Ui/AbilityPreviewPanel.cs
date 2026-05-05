using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityPreviewPanel : MonoBehaviour
{
    [Header("Texto de UI Central")]
    // Reemplazamos los 4 textos antiguos por uno solo
    [SerializeField] private TextMeshProUGUI descripcionText;

    [Header("Animación Persiana (Fill Amount)")]
    [SerializeField] private Image imagenFondo;
    [SerializeField] private float animDuration = 0.15f;

    private Coroutine animCoroutine;

    private void Awake()
    {
        if (imagenFondo != null) imagenFondo.fillAmount = 0f;
        AlternarTextos(false); // Apagado por defecto
    }

    public void MostrarInfo(Ability skill, int dañoBasico)
    {
        gameObject.SetActive(true);

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(SecuenciaCambioInformacion(skill, dañoBasico));
    }

    public void Ocultar()
    {
        if (gameObject.activeInHierarchy)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(CerrarYApagar());
        }
    }

    private IEnumerator SecuenciaCambioInformacion(Ability skill, int dañoBasico)
    {
        if (imagenFondo == null)
        {
            Debug.LogError("¡ERROR! 'imagenFondo' no está asignada en el inspector de AbilityPreviewPanel.");
            ActualizarTextosInterno(skill, dañoBasico);
            AlternarTextos(true);
            yield break;
        }

        if (imagenFondo.fillAmount > 0.05f)
        {
            AlternarTextos(false);
            float startFill = imagenFondo.fillAmount;
            float elapsedCierre = 0f;
            float tiempoCierre = animDuration * 0.5f;

            while (elapsedCierre < tiempoCierre)
            {
                elapsedCierre += Time.deltaTime;
                imagenFondo.fillAmount = Mathf.Lerp(startFill, 0f, elapsedCierre / tiempoCierre);
                yield return null;
            }
            imagenFondo.fillAmount = 0f;
        }

        ActualizarTextosInterno(skill, dañoBasico);
        AlternarTextos(true);

        Canvas.ForceUpdateCanvases();

        float elapsedApertura = 0f;
        while (elapsedApertura < animDuration)
        {
            elapsedApertura += Time.deltaTime;
            float t = elapsedApertura / animDuration;
            t = t * t * (3f - 2f * t);

            imagenFondo.fillAmount = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        imagenFondo.fillAmount = 1f;
    }

    private IEnumerator CerrarYApagar()
    {
        if (imagenFondo == null) yield break;

        AlternarTextos(false);
        float startFill = imagenFondo.fillAmount;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            imagenFondo.fillAmount = Mathf.Lerp(startFill, 0f, t);
            yield return null;
        }

        imagenFondo.fillAmount = 0f;
        gameObject.SetActive(false);
    }

    private void ActualizarTextosInterno(Ability skill, int dañoBasico)
    {
        if (skill != null)
        {
            // 1. Empezamos con la descripción base de la habilidad
            string textoFinal = skill.descripcion;

            // 2. Si aplica un estado, se lo sumamos al final del texto
            if (skill.aplicaEstado)
            {
                // Agregamos un doble salto de línea para separar la descripción del estado
                textoFinal += $"\n\naplica <color=#FFDD44>{TraducirEstado(skill.tipoEstado)}</color> durante {skill.duracionEstado} turnos";
            }

            // Asignamos el resultado final construido al único campo de texto
            descripcionText.text = textoFinal;
        }
        else
        {
            // Texto por defecto para el ataque básico
            descripcionText.text = $"Ataque Básico.\nInflige <color=#FF4444>{dañoBasico}</color> de daño.";
        }
    }

    private void AlternarTextos(bool estado)
    {
        if (descripcionText != null) descripcionText.gameObject.SetActive(estado);
    }

    private string TraducirEstado(State.StateType estado)
    {
        switch (estado)
        {
            case State.StateType.Quemadura: return "Quemadura";
            case State.StateType.Veneno: return "Veneno";
            case State.StateType.Radiacion: return "Radiación";
            case State.StateType.Hemorragia: return "Hemorragia";
            case State.StateType.Vitalidad: return "Vitalidad";
            case State.StateType.Lucidez: return "Lucidez";
            case State.StateType.Prisa: return "Prisa";
            case State.StateType.Certeza: return "Certeza";
            case State.StateType.Coraza: return "Coraza";
            case State.StateType.Furia: return "Furia";
            case State.StateType.Espejismo: return "Espejismo";
            case State.StateType.Sifon: return "Sifón";
            case State.StateType.Baluarte: return "Baluarte";
            case State.StateType.Fractura: return "Fractura";
            case State.StateType.Pesadez: return "Pesadez";
            case State.StateType.Ceguera: return "Ceguera";
            case State.StateType.Fragilidad: return "Fragilidad";
            case State.StateType.Fatiga: return "Fatiga";
            case State.StateType.Silencio: return "Silencio";
            case State.StateType.Sueno: return "Sueño";
            case State.StateType.Escarcha: return "Escarcha";
            case State.StateType.Cepo: return "Cepo";
            default: return estado.ToString();
        }
    }
}