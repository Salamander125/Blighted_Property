using TMPro;
using UnityEngine;

public class Speed_Manager : MonoBehaviour
{
    public static Speed_Manager instance;

    [Header("Configuración de Velocidades")]
    public float[] multiplicadores = { 1f, 2f, 3f }; // x1, x2, x3
    private int indiceActual = 0;

    [Header("UI (Opcional)")]
    public TextMeshProUGUI textoVelocidad; // Para arrastrar un texto de tu Canvas

    private void Awake()
    {
        // Singleton básico para que puedas llamarlo desde un botón de la UI si quieres
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ActualizarVelocidad();
    }

    private void Update()
    {
        // Cambiar de velocidad al pulsar el tabulador
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            CambiarVelocidad();
        }
    }

    public void CambiarVelocidad()
    {
        // Avanzamos al siguiente multiplicador, si llegamos al final, volvemos a 0 (bucle)
        indiceActual++;
        if (indiceActual >= multiplicadores.Length)
        {
            indiceActual = 0;
        }

        ActualizarVelocidad();
    }

    public void ActualizarVelocidad()
    {
        // ESTA ES LA MAGIA. Esto acelera TODO el motor de Unity.
        Time.timeScale = multiplicadores[indiceActual];

        // Actualizamos la UI si le hemos asignado un texto
        if (textoVelocidad != null)
        {
            textoVelocidad.text = "X" + multiplicadores[indiceActual].ToString("0");

            // Opcional: Cambiar de color para que destaque
            textoVelocidad.color = (indiceActual == 0) ? Color.white : Color.yellow;
        }

        Debug.Log("Velocidad global cambiada a: x" + Time.timeScale);
    }

}
