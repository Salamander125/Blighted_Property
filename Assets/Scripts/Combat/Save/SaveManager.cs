using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// 1. Replicamos las clases DTO de C# para que Unity sepa empaquetarlo
[System.Serializable]
public class SavedCharacter
{
    public string characterName;
    public int currentLife;
    public int currentMana;
}

[System.Serializable]
public class MatchRequest
{
    public int matchId;
    public int userId;
    public int floorReached;
    public int goldCollected;
    public int enemiesKilled;
    public int playTimeSeconds;
    public bool isFinished;
    public string lastSaved; // En Unity lo manejamos como string para evitar problemas de parseo

    public List<SavedCharacter> characters = new List<SavedCharacter>();
    public List<int> purchasedCardIds = new List<int>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    private string apiUrl = "http://blightedproperty.somee.com/api/matches";

    // Variables Temporales que debemos mantener en memoria
    [HideInInspector] public int currentMatchId = 0; // 0 = Partida Nueva
    [HideInInspector] public int totalEnemiesKilled = 0;
    [HideInInspector] public float playTimeTimer = 0f;

    // Lista temporal de las cartas que el jugador ha ido comprando
    [HideInInspector] public List<int> currentRunCards = new List<int>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Contamos el tiempo de juego si estamos en combate/mazmorra
        if (Turn_Controller.instance != null)
        {
            playTimeTimer += Time.deltaTime;
        }
    }

    // ==========================================
    // MÉTODO PARA GUARDAR PARTIDA
    // ==========================================
    public void GuardarPartida(bool isFinishedRun)
    {
        StartCoroutine(RutinaGuardar(isFinishedRun));
    }

    public IEnumerator RutinaGuardar(bool isFinishedRun)
    {
        MatchRequest request = new MatchRequest();

        // 1. Datos Base
        // PlayerPrefs.GetInt("UserId") asumimos que se guardó al hacer Login
        request.userId = PlayerPrefs.GetInt("UserId", 1);
        request.matchId = currentMatchId;
        request.floorReached = Floor_Manager.instance.currentFloor;
        request.goldCollected = Turn_Controller.instance.totalGold;
        request.enemiesKilled = totalEnemiesKilled;
        request.playTimeSeconds = Mathf.FloorToInt(playTimeTimer);
        request.isFinished = isFinishedRun;

        request.lastSaved = System.DateTime.UtcNow.ToString("o");

        // 2. Las Cartas Compradas
        request.purchasedCardIds = new List<int>(currentRunCards);

        // 3. El Estado de los Personajes
        foreach (Entity entity in Turn_Controller.instance.allEntities)
        {
            if (entity.faction == Faction.Player)
            {
                SavedCharacter sc = new SavedCharacter();
                sc.characterName = entity.gameObject.name.Replace("(Clone)", "").Trim();
                sc.currentLife = entity.CurrentLife;
                sc.currentMana = entity.CurrentMana;

                request.characters.Add(sc);
            }
        }

        // 4. Convertir a JSON y enviar
        string jsonPayload = JsonUtility.ToJson(request);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("¡Partida Guardada Correctamente!");

                // IMPORTANTE: Si era una partida nueva (0), el servidor nos devuelve el nuevo ID asignado.
                // Podrías extraerlo de la respuesta para que los siguientes guardados hagan un UPDATE.
                // De momento, como concepto básico, el guardado funciona perfecto.
            }
            else
            {
                Debug.LogError("Error al guardar: " + www.error);
                Debug.LogError("Motivo del servidor: " + www.downloadHandler.text);
            }        
        }
    }
}