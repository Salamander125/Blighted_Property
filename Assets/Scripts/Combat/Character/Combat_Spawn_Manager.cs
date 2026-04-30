using UnityEngine;
using System.Collections.Generic;

public class Combat_Spawn_Manager : MonoBehaviour
{
    [Header("Posiciones de Aparición")]
    public Vector3[] spawnPositions;

    [Header("Cámaras de Retrato")]
    [Tooltip("Arrastra aquí las 3 cámaras de retrato en orden (1, 2 y 3)")]
    public Camera[] portraitCameras;

    [Header("Configuración Visual")]
    public string sortingLayerName = "Default";

    [Header("Base de Datos Global de Personajes")]
    public CharacterDataSO[] allPossibleCharacters;

    private void Start()
    {
        if (PlayerSelectionData.PartidaCargada != null)
        {
            // --- MODO CARGAR PARTIDA ---
            RestaurarPartidaGuardada(PlayerSelectionData.PartidaCargada);
            PlayerSelectionData.PartidaCargada = null; // Limpiamos para el futuro
        }
        else
        {
            // --- MODO PARTIDA NUEVA ---
            SpawnTeam();
        }
    }

    private void RestaurarPartidaGuardada(MatchRequest partida)
    {
        // 1. INYECTAR VARIABLES GLOBALES AL JUEGO
        SaveManager.instance.currentMatchId = partida.matchId; // ESTO PERMITE SOBRESCRIBIR LA MISMA PARTIDA
        SaveManager.instance.totalEnemiesKilled = partida.enemiesKilled;
        SaveManager.instance.playTimeTimer = partida.playTimeSeconds;
        SaveManager.instance.currentRunCards = new List<int>(partida.purchasedCardIds);

        // Aseguramos que Floor_Manager y Turn_Controller existan y aplicamos sus valores
        if (Floor_Manager.instance != null) Floor_Manager.instance.currentFloor = partida.floorReached;
        if (Turn_Controller.instance != null) Turn_Controller.instance.totalGold = partida.goldCollected;

        // 2. SPAWNEAR PERSONAJES
        int total = Mathf.Min(partida.characters.Count, spawnPositions.Length);

        for (int i = 0; i < total; i++)
        {
            SavedCharacter savedChar = partida.characters[i];

            // Buscamos el SO original basándonos en el texto (ej: "Merida")
            CharacterDataSO data = System.Array.Find(allPossibleCharacters, c => c.nombre == savedChar.characterName);

            if (data != null && data.prefab != null)
            {
                // Instanciamos
                GameObject obj = Instantiate(data.prefab, spawnPositions[i], Quaternion.identity);
                obj.name = data.nombre;

                // Aplicar visuales, cámara y UI (igual que en SpawnTeam)
                string dynamicLayerName = "Portrait_char" + (i + 1);
                int layerID = LayerMask.NameToLayer(dynamicLayerName);
                if (layerID == -1) layerID = 0;

                ConfigurarVisualesRecursivo(obj, layerID, i);
                AjustarCamaraSlot(obj, i);

                if (UIManager_Combat.instance != null)
                    UIManager_Combat.instance.AsignarSlotAPersonaje(obj, i);

                // --- LA INYECCIÓN DE VIDA Y MANÁ ---
                Entity entityScript = obj.GetComponent<Entity>();
                if (entityScript != null)
                {
                    entityScript.InitializeStatsIfNeeded();
                    entityScript.SetStatsFromLoad(savedChar.currentLife, savedChar.currentMana);
                }

                Character characterScript = obj.GetComponent<Character>();
                if (characterScript != null && Turn_Controller.instance != null)
                {
                    Turn_Controller.instance.allEntities.Add(characterScript);
                }
            }
            else
            {
                Debug.LogError($"No se encontró el ScriptableObject para el personaje guardado: {savedChar.characterName}");
            }
        }
    }

    void SpawnTeam()
    {
        if (PlayerSelectionData.ChosenCharacters.Count == 0) return;

        // Seguridad: No intentar spawnear más de lo que permiten las posiciones
        int total = Mathf.Min(PlayerSelectionData.ChosenCharacters.Count, spawnPositions.Length);

        for (int i = 0; i < total; i++)
        {
            CharacterDataSO data = PlayerSelectionData.ChosenCharacters[i];

            if (data != null && data.prefab != null)
            {
                // 1. Instanciar
                GameObject obj = Instantiate(data.prefab, spawnPositions[i], Quaternion.identity);
                obj.name = data.nombre;

                // 2. Definir Capa Dinámica
                string dynamicLayerName = "Portrait_char" + (i + 1);
                int layerID = LayerMask.NameToLayer(dynamicLayerName);

                if (layerID == -1)
                {
                    Debug.LogWarning($"La capa {dynamicLayerName} no existe. Usando 'Default'.");
                    layerID = 0;
                }

                // 3. Aplicar Capa y Sorting Order
                ConfigurarVisualesRecursivo(obj, layerID, i);

                // --- NUEVO: 4. Ajustar Cámara de Retrato al FaceAnchor ---
                AjustarCamaraSlot(obj, i);

                // 5. Asignación de Slot en la UI
                if (UIManager_Combat.instance != null)
                {
                    UIManager_Combat.instance.AsignarSlotAPersonaje(obj, i);
                }

                // 6. Registro en Turnos
                Character characterScript = obj.GetComponent<Character>();
                if (characterScript != null && Turn_Controller.instance != null)
                {
                    Turn_Controller.instance.allEntities.Add(characterScript);
                }
            }
        }
    }

    private void AjustarCamaraSlot(GameObject personaje, int index)
    {
        // Verificamos que tengamos una cámara para este slot
        if (index >= portraitCameras.Length || portraitCameras[index] == null) return;

        // Buscamos el hijo llamado "FaceAnchor"
        Transform anchor = personaje.transform.Find("FaceAnchor");

        if (anchor != null)
        {
            Camera cam = portraitCameras[index];
            // Movemos la cámara a la posición del anchor
            // Mantenemos la Z original de la cámara para no pegarnos al sprite
            Vector3 nuevaPos = anchor.position;
            nuevaPos.z = cam.transform.position.z;
            cam.transform.position = nuevaPos;
        }
        else
        {
            Debug.LogWarning($"El prefab {personaje.name} no tiene un hijo llamado 'FaceAnchor'. La cámara {portraitCameras[index].name} no se ha movido.");
        }
    }

    private void ConfigurarVisualesRecursivo(GameObject obj, int newLayer, int exactOrder)
    {
        obj.layer = newLayer;

        SpriteRenderer sRenderer = obj.GetComponent<SpriteRenderer>();
        if (sRenderer != null)
        {
            sRenderer.sortingLayerName = sortingLayerName;
            sRenderer.sortingOrder = exactOrder;
        }

        foreach (Transform child in obj.transform)
        {
            ConfigurarVisualesRecursivo(child.gameObject, newLayer, exactOrder);
        }
    }
}