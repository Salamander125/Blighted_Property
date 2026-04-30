using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI textoZona;
    public TextMeshProUGUI textoOro;
    public TextMeshProUGUI textoPiso;
    public TextMeshProUGUI textoEnemigos;
    public TextMeshProUGUI textoTiempo;

    [Header("Imágenes y Encuadre")]
    public Image imagenFondo;
    [Tooltip("El componente AspectRatioFitter que está en el mismo objeto que la Imagen de Fondo")]
    public AspectRatioFitter fondoFitter;
    public Image[] circulosRetratos; // Arrastra aquí los 3 Image de las caras (hijos de tu panel)
    [HideInInspector] public bool modoGuardado = false;

    [HideInInspector] public int matchIdAsignado;

    private MatchRequest miPartidaCompleta;

    // Esta función es llamada por el LoadManager para inyectar toda la información
    public void ConfigurarTarjeta(MatchRequest partida, string nombreZona, Sprite spriteFondo, Vector2 alineacion, Sprite[] retratos)
    {
        miPartidaCompleta = partida;
        matchIdAsignado = partida.matchId;

        // 1. Textos
        textoZona.text = nombreZona;
        textoPiso.text = $"{partida.floorReached}";
        textoOro.text = partida.goldCollected.ToString() + " G";
        textoEnemigos.text = partida.enemiesKilled.ToString();

        // Formateamos los segundos totales a formato MM:SS
        int minutos = partida.playTimeSeconds / 60;
        int segundos = partida.playTimeSeconds % 60;
        textoTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);

        // 2. Imagen de Fondo y Encuadre
        if (imagenFondo != null && spriteFondo != null)
        {
            imagenFondo.sprite = spriteFondo;

            // Calculamos la proporción real de la imagen para que no se deforme
            if (fondoFitter != null)
            {
                fondoFitter.aspectRatio = spriteFondo.rect.width / spriteFondo.rect.height;
            }

            // Aplicamos el encuadre personalizado (Pivot)
            RectTransform rtFondo = imagenFondo.GetComponent<RectTransform>();
            if (rtFondo != null)
            {
                rtFondo.pivot = alineacion;

                // Forzamos que se mantenga anclada al centro de su padre (el Mask) para que el pivot haga su magia
                rtFondo.anchorMin = new Vector2(0.5f, 0.5f);
                rtFondo.anchorMax = new Vector2(0.5f, 0.5f);
                rtFondo.anchoredPosition = Vector2.zero;
            }
        }

        // 3. Retratos de los Personajes
        for (int i = 0; i < circulosRetratos.Length; i++)
        {
            if (i < retratos.Length && retratos[i] != null)
            {
                circulosRetratos[i].sprite = retratos[i];
                circulosRetratos[i].gameObject.SetActive(true);
            }
            else
            {
                // Si la partida tiene menos personajes que círculos, apagamos los sobrantes
                circulosRetratos[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnClick_AccionSlot()
    {
        // BLINDAJE ABSOLUTO:
        // Si el PauseManager existe en la escena, significa que estamos en mitad del combate.
        // Por lógica, aquí es IMPOSIBLE que queramos cargar una partida. Siempre es Sobrescribir.
        if (PauseManager.instance != null)
        {
            Debug.Log("Sobrescribiendo la partida con ID: " + matchIdAsignado);
            PauseManager.instance.SeleccionarSlotParaSobrescribir(matchIdAsignado);
        }
        // Si el PauseManager NO existe, significa que estamos en el Menú Principal.
        // Por lo tanto, aquí la tarjeta solo sirve para Cargar.
        else
        {
            Debug.Log("Iniciando carga de la partida con ID: " + matchIdAsignado);
            PlayerSelectionData.PartidaCargada = miPartidaCompleta;
            SceneManager.LoadScene("Combat_scene");
        }
    }
    // Añadir al final de SaveSlotUI.cs
    public void SetHighlight(bool isFocused)
    {
        // 1. Te avisa por consola de que el teclado sí funciona
        if (isFocused) Debug.Log("Navegando... Tarjeta enfocada: " + matchIdAsignado);

        // 2. Intenta hacerla más grande
        transform.localScale = isFocused ? new Vector3(1.05f, 1.05f, 1.05f) : Vector3.one;

        // 3. NUEVO: Le cambiamos el color al fondo para que sea SÚPER evidente
        if (imagenFondo != null)
        {
            // Si está enfocada, le pone un tinte amarillento/dorado, si no, blanca/normal
            imagenFondo.color = isFocused ? new Color(1f, 0.9f, 0.6f) : Color.white;
        }
    }
}