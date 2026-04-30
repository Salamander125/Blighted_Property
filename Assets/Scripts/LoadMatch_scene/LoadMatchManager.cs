using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LoadMatchManager : MonoBehaviour
{
    [System.Serializable]
    public struct ZonaUI
    {
        public int pisoMinimo; // Ej: 1
        public string nombreZona; // Ej: "Bosque Oscuro"
        public Sprite imagenFondo;
        [Tooltip("Valores 0 a 1. Centro=(0.5, 0.5), Arriba=(0.5, 1), Abajo=(0.5, 0)")]
        public Vector2 alineacionImagen;
    }

    [System.Serializable]
    public struct RetratoPersonaje
    {
        public string nombreExacto; // Ej: "Merida"
        public Sprite cara;
    }

    [Header("Base de Datos Visual")]
    public List<ZonaUI> zonasPorPiso;
    public List<RetratoPersonaje> retratosDisponibles;

    [Header("Configuración de Instanciación")]
    public GameObject saveSlotPrefab;
    public Transform contenedorLayout; // Arrastra el padre con el Vertical Layout Group

    private string apiUrl = "https://blightedproperty.somee.com/api/matches/active/";

    private void Start()
    {
        // 1. Limpiamos cualquier tarjeta de prueba que haya quedado en el editor
        foreach (Transform child in contenedorLayout)
        {
            Destroy(child.gameObject);
        }

        // 2. Conectamos con el backend para buscar las partidas del usuario
        StartCoroutine(DescargarPartidas());
    }

    private IEnumerator DescargarPartidas()
    {
        int userId = PlayerPrefs.GetInt("UserId", 1);
        Debug.Log($"Pidiendo partidas para el Usuario ID: {userId}");

        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl + userId))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                // 1. EL CHIVATO PRINCIPAL: ¿Qué nos ha enviado el servidor exactamente?
                Debug.Log("<color=cyan>JSON CRUDO DEL SERVIDOR:</color>\n" + www.downloadHandler.text);

                // Envolvemos el array JSON que devuelve C#
                string jsonString = "{\"matches\":" + www.downloadHandler.text + "}";

                // 2. Comprobamos cómo queda envuelto
                Debug.Log("<color=yellow>JSON ENVUELTO PARA UNITY:</color>\n" + jsonString);

                MatchListWrapper wrapper = JsonUtility.FromJson<MatchListWrapper>(jsonString);

                if (wrapper != null && wrapper.matches != null)
                {
                    Debug.Log($"<color=green>¡Éxito!</color> Unity ha parseado {wrapper.matches.Length} partidas.");
                    GenerarTarjetas(wrapper.matches);
                }
                else
                {
                    Debug.LogError("El JSON llegó bien, pero JsonUtility no pudo parsearlo. Revisa que los nombres de las variables de MatchRequest coincidan EXACTAMENTE con el JSON.");
                }
            }
            else
            {
                Debug.LogError("Error al descargar partidas: " + www.error);
                Debug.LogError("Respuesta del servidor: " + www.downloadHandler.text);
            }
        }
    }

    private void GenerarTarjetas(MatchRequest[] partidasGuardadas)
    {
        // Forzamos el límite estricto de 3 partidas máximas
        int limite = Mathf.Min(partidasGuardadas.Length, 3);

        for (int i = 0; i < limite; i++)
        {
            MatchRequest partida = partidasGuardadas[i];

            // 1. Deducir qué zona visual y encuadre le toca según el piso
            ZonaUI zonaEncontrada = ResolverZona(partida.floorReached);

            // 2. Extraer las caras de los personajes guardados en esa partida
            List<Sprite> spritesCaras = new List<Sprite>();
            foreach (var character in partida.characters)
            {
                Sprite cara = BuscarRetrato(character.characterName);
                if (cara != null) spritesCaras.Add(cara);
            }

            // 3. Instanciar la plantilla e inyectar los datos visuales
            GameObject nuevaTarjeta = Instantiate(saveSlotPrefab, contenedorLayout);
            SaveSlotUI slotUI = nuevaTarjeta.GetComponent<SaveSlotUI>();

            slotUI.ConfigurarTarjeta(
                partida,
                zonaEncontrada.nombreZona,
                zonaEncontrada.imagenFondo,
                zonaEncontrada.alineacionImagen,
                spritesCaras.ToArray()
            );
        }

        // Si el jugador tiene menos de 3 partidas, aquí podríamos instanciar un botón extra
        // que diga "Empezar Nueva Partida" para llenar los huecos restantes.
        if (limite < 3)
        {
            Debug.Log($"El jugador aún tiene {3 - limite} ranuras vacías.");
        }
    }

    // ==========================================
    // HELPERS DE BÚSQUEDA
    // ==========================================
    private ZonaUI ResolverZona(int piso)
    {
        if (zonasPorPiso == null || zonasPorPiso.Count == 0) return new ZonaUI();

        ZonaUI zonaElegida = zonasPorPiso[0]; // Por defecto cogemos la primera

        // Recorremos buscando la zona más avanzada que cumpla el requisito del piso
        foreach (var zona in zonasPorPiso)
        {
            if (piso >= zona.pisoMinimo)
            {
                zonaElegida = zona;
            }
        }
        return zonaElegida;
    }

    private Sprite BuscarRetrato(string nombre)
    {
        foreach (var retrato in retratosDisponibles)
        {
            // Usamos Contains para mayor seguridad (por si Unity guarda "(Clone)" o espacios en blanco)
            if (nombre.Contains(retrato.nombreExacto)) return retrato.cara;
        }
        return null;
    }

    // ==========================================
    // CLASE ENVOLTORIO PARA JSONUTILITY
    // ==========================================
    [System.Serializable]
    private class MatchListWrapper
    {
        public MatchRequest[] matches;
    }
}