using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    public static ApiManager instance;

    [Header("Configuración API")]
    [Tooltip("URL de tu servidor Somee que devuelve TODAS las habilidades")]
    public string apiUrl = "https://blightedproperty.somee.com/api/abilities";

    [Header("Datos Descargados")]
    public List<Ability> habilidadesTotales = new List<Ability>();

    private void Awake()
    {
        // Configuramos el Singleton para que otros scripts puedan acceder a ApiManager.instance
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Opcional: Para que sobreviva entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Al arrancar el juego, llamamos a la API automáticamente
        StartCoroutine(DescargarHabilidades());
    }

    private IEnumerator DescargarHabilidades()
    {
        Debug.Log("<color=cyan>Conectando con la API en Somee...</color>");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            // Enviamos la petición y esperamos a que el servidor responda
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error al conectar con la API: " + webRequest.error);
            }
            else
            {
                // ¡Éxito! Tenemos el texto JSON
                string jsonResponse = webRequest.downloadHandler.text;
                Debug.Log("<color=green>¡Habilidades descargadas correctamente del servidor!</color>");

                ProcesarDatos(jsonResponse);
            }
        }
    }

    private void ProcesarDatos(string json)
    {
        // 1. Limpiamos la lista por si estamos recargando
        habilidadesTotales.Clear();

        // 2. Convertimos el JSON de la API a una lista de modelos DTO intermedios
        AbilityModelDTO[] modelosAPI = JsonHelper.FromJson<AbilityModelDTO>(json);

        if (modelosAPI == null || modelosAPI.Length == 0)
        {
            Debug.LogWarning("La API respondió, pero el JSON estaba vacío o no se pudo leer.");
            return;
        }

        // 3. Convertimos esos modelos de texto a nuestras clases "Ability" reales de Unity
        foreach (var dto in modelosAPI)
        {
            Ability nuevaHabilidad = new Ability
            {
                ownerCharacter = dto.ownerCharacter,
                nombre = dto.nombre,
                descripcion = dto.descripcion,
                damage = dto.damage,
                manaCost = dto.manaCost,        
                healAmount = dto.healAmount,
                targetsAllies = dto.targetsAllies,

                // Blindaje: Si viene vacío o nulo (cosa que puede pasar con aliados), le ponemos Single por defecto
                shape = string.IsNullOrEmpty(dto.shape) ? AbilityShape.Single : (AbilityShape)System.Enum.Parse(typeof(AbilityShape), dto.shape),
                range = dto.range,

                aplicaEstado = dto.aplicaEstado,
                tipoEstado = string.IsNullOrEmpty(dto.tipoEstado) || dto.tipoEstado == "None"
                             ? State.StateType.None
                             : (State.StateType)System.Enum.Parse(typeof(State.StateType), dto.tipoEstado),
                duracionEstado = dto.duracionEstado,
                potenciaEstado = dto.potenciaEstado,

                // Si dto.moveOffset es null (por algún error de la API), creamos uno en 0,0
                moveOffset = dto.moveOffset != null ? new Vector2Int(dto.moveOffset.x, dto.moveOffset.y) : Vector2Int.zero,
                spawnOffset = dto.spawnOffset != null ? new Vector3(dto.spawnOffset.x, dto.spawnOffset.y, dto.spawnOffset.z) : Vector3.zero,

                prefabPath = dto.prefabPath,
                prefabPorCasilla = dto.prefabPorCasilla,
                secuencial = dto.secuencial,
                delayEntreCasillas = dto.delayEntreCasillas,
                esUnitarget = dto.esUnitarget,

                // Blindaje igual que el shape
                tipoMovimiento = string.IsNullOrEmpty(dto.tipoMovimiento) ? MovementType.Free : (MovementType)System.Enum.Parse(typeof(MovementType), dto.tipoMovimiento)
            };

            // ¡Buscamos el prefab visual en la carpeta Resources!
            nuevaHabilidad.CargarPrefabDesdeResources();

            // La añadimos a nuestra gran biblioteca de habilidades
            habilidadesTotales.Add(nuevaHabilidad);
        }

        Debug.Log($"<color=yellow>Se han procesado {habilidadesTotales.Count} habilidades. ¡Repartiendo a los personajes!</color>");

        // 4. --- AVISAR A LOS PERSONAJES ---
        // Buscamos a todos los personajes en la escena y les decimos que cojan sus habilidades
        Character_Ability[] todosLosPersonajes = FindObjectsByType<Character_Ability>(FindObjectsSortMode.None);
        foreach (var personaje in todosLosPersonajes)
        {
            personaje.CargarHabilidadesDeAPI();
        }
    }
}

// ==========================================
// CLASES AUXILIARES PARA MAPEO JSON
// ==========================================

// Estas clases son idénticas a la estructura que devuelve tu API
[System.Serializable]
public class AbilityModelDTO
{
    public string ownerCharacter;
    public string nombre;
    public string descripcion;
    public int damage;
    public int manaCost;
    public int healAmount;
    public bool targetsAllies;

    public string shape;
    public int range;
    public bool aplicaEstado;
    public string tipoEstado;
    public int duracionEstado;
    public int potenciaEstado;
    public Vector2IntModel moveOffset;
    public Vector3Model spawnOffset;
    public string prefabPath;
    public bool prefabPorCasilla;
    public bool secuencial;
    public float delayEntreCasillas;
    public bool esUnitarget;
    public string tipoMovimiento;
}

[System.Serializable]
public class Vector2IntModel { public int x; public int y; }
[System.Serializable]
public class Vector3Model { public float x; public float y; public float z; }

// Truco nativo de Unity para leer arrays JSON [ {..}, {..} ] que vienen de internet
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"Items\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.Items;
    }

    [System.Serializable]
    private class Wrapper<T> { public T[] Items; }
}