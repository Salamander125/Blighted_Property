using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

// ==========================================
// ESTRUCTURAS DTO (Deben coincidir con tu API en Somee)
// ==========================================
[System.Serializable]
public class SavedEffect
{
    public State.StateType type;
    public int duration;
    public int intensity;
}

[System.Serializable]
public class SavedCharacter
{
    public string characterName;
    public int currentLife;
    public int currentMana;
    public int maxLife;
    public int maxMana;
    public int attack;
    public float defense;
    public int speed;
    public int critChance;
    public int evasion;
    public List<SavedEffect> activeEffects = new List<SavedEffect>();
}

// Preparado para la Fase 3 de los enemigos
[System.Serializable]
public class EnemySaveData
{
    public string enemyID;

    // Posición en el Tablero
    public int gridX;
    public int gridY;

    // Estadísticas Actuales y Máximas (Igual que los aliados)
    public int currentLife;
    public int currentMana;
    public int maxLife;
    public int maxMana;

    public int attack;
    public float defense;
    public int speed;
    public int critChance;
    public int evasion;

    // Efectos Alterados
    public List<SavedEffect> activeEffects = new List<SavedEffect>();
}

[System.Serializable]
public class SavedGridCell
{
    public int x;
    public int y;
    public int effect;
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
    public string lastSaved;

    // COMPRUEBA QUE ESTAS TRES LISTAS EXISTAN EN TU SCRIPT DE UNITY:
    public List<SavedCharacter> characters = new List<SavedCharacter>();
    public List<EnemySaveData> enemies = new List<EnemySaveData>();
    public List<SavedGridCell> gridCells = new List<SavedGridCell>();

    public List<int> purchasedCardIds = new List<int>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    // OJO: Usamos HTTPS para evitar el error de Mixed Content
    private string apiUrl = "https://blightedproperty.somee.com/api/matches";

    [HideInInspector] public int currentMatchId = 0;
    [HideInInspector] public int totalEnemiesKilled = 0;
    [HideInInspector] public float playTimeTimer = 0f;
    [HideInInspector] public List<int> currentRunCards = new List<int>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Turn_Controller.instance != null)
        {
            playTimeTimer += Time.deltaTime;
        }
    }

    public void GuardarPartida(bool isFinishedRun)
    {
        StartCoroutine(RutinaGuardar(isFinishedRun));
    }

    public IEnumerator RutinaGuardar(bool isFinishedRun)
    {
        MatchRequest request = new MatchRequest();

        request.userId = PlayerPrefs.GetInt("UserId", 1);
        request.matchId = currentMatchId;
        request.floorReached = Floor_Manager.instance.currentFloor;
        request.goldCollected = Turn_Controller.instance.totalGold;
        request.enemiesKilled = totalEnemiesKilled;
        request.playTimeSeconds = Mathf.FloorToInt(playTimeTimer);
        request.isFinished = isFinishedRun;
        request.lastSaved = System.DateTime.UtcNow.ToString("o");   

        request.purchasedCardIds = new List<int>(currentRunCards);

        // RECOPILACIÓN DE ALIADOS, ENEMIGOS Y CASILLAS
        foreach (Entity entity in Turn_Controller.instance.allEntities)
        {
            if (entity.faction == Faction.Player)
            {
                request.characters.Add(entity.GenerarSaveDataAliado());
            }
            else if (entity.faction == Faction.Enemy && entity is Enemy enemy)
            {
                // Dejamos esto listo para cuando hagamos la parte de los enemigos
                request.enemies.Add(enemy.GenerarSaveData());
            }
            request.gridCells = Grid_Controller.instance.GenerarSaveDataGrid();
        }

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
            }
            else
            {
                Debug.LogError("Error al guardar: " + www.error);
                Debug.LogError("Motivo: " + www.downloadHandler.text);
            }
        }
    }
}