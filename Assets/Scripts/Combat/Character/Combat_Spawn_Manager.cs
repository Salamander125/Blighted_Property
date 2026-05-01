using UnityEngine;
using System.Collections.Generic;

public class Combat_Spawn_Manager : MonoBehaviour
{
    [Header("Posiciones de Aparición")]
    public Vector3[] spawnPositions;

    [Header("Cámaras de Retrato")]
    public Camera[] portraitCameras;

    [Header("Configuración Visual")]
    public string sortingLayerName = "Default";

    [Header("Base de Datos Global de Personajes")]
    public CharacterDataSO[] allPossibleCharacters;

    [Header("Managers Externos")]
    public Enemy_Spawn enemySpawnManager;

    private void Start()
    {
        if (PlayerSelectionData.PartidaCargada != null)
        {
            RestaurarPartidaGuardada(PlayerSelectionData.PartidaCargada);
            PlayerSelectionData.PartidaCargada = null; // Ahora es seguro limpiarlo
        }
        else
        {
            SpawnTeam();
            if (enemySpawnManager != null)
            {
                enemySpawnManager.SpawnRandomEnemies();
            }
        }
    }

    private void RestaurarPartidaGuardada(MatchRequest partida)
    {
        SaveManager.instance.currentMatchId = partida.matchId;
        SaveManager.instance.totalEnemiesKilled = partida.enemiesKilled;
        SaveManager.instance.playTimeTimer = partida.playTimeSeconds;
        SaveManager.instance.currentRunCards = new List<int>(partida.purchasedCardIds);

        if (Turn_Controller.instance != null) Turn_Controller.instance.totalGold = partida.goldCollected;

        // --- ORDENES DIRECTAS AL FLOOR MANAGER ANTES DE NADA ---
        if (Floor_Manager.instance != null)
        {
            Floor_Manager.instance.currentFloor = partida.floorReached;
            Floor_Manager.instance.isLoadedGame = true; // Blindamos las casillas y los spawns
            Floor_Manager.instance.ForcePoolSetup(partida.floorReached); // Pedimos la bolsa de enemigos YA
        }

        // 1. RECONSTRUIR EL TABLERO
        if (Grid_Controller.instance != null)
        {
            Grid_Controller.instance.CargarSaveDataGrid(partida.gridCells);
        }

        // 2. SPAWNEAR ALIADOS
        int total = Mathf.Min(partida.characters.Count, spawnPositions.Length);
        for (int i = 0; i < total; i++)
        {
            SavedCharacter savedChar = partida.characters[i];
            CharacterDataSO data = System.Array.Find(allPossibleCharacters, c => c.nombre == savedChar.characterName);

            if (data != null && data.prefab != null)
            {
                GameObject obj = Instantiate(data.prefab, spawnPositions[i], Quaternion.identity);
                obj.name = data.nombre;

                string dynamicLayerName = "Portrait_char" + (i + 1);
                int layerID = LayerMask.NameToLayer(dynamicLayerName);
                if (layerID == -1) layerID = 0;

                ConfigurarVisualesRecursivo(obj, layerID, i);
                AjustarCamaraSlot(obj, i);

                if (UIManager_Combat.instance != null) UIManager_Combat.instance.AsignarSlotAPersonaje(obj, i);

                Entity entityScript = obj.GetComponent<Entity>();
                if (entityScript != null)
                {
                    entityScript.InitializeStatsIfNeeded();
                    entityScript.SetStatsFromLoad(savedChar);
                }

                Character characterScript = obj.GetComponent<Character>();
                if (characterScript != null && Turn_Controller.instance != null)
                {
                    Turn_Controller.instance.allEntities.Add(characterScript);
                }
            }
        }

        // 3. SPAWNEAR ENEMIGOS (¡Ahora la bolsa sí tiene prefabs!)
        if (enemySpawnManager != null && partida.enemies != null && partida.enemies.Count > 0)
        {
            enemySpawnManager.SpawnLoadedEnemies(partida.enemies);
        }
    }

    void SpawnTeam()
    {
        if (PlayerSelectionData.ChosenCharacters.Count == 0) return;
        int total = Mathf.Min(PlayerSelectionData.ChosenCharacters.Count, spawnPositions.Length);

        for (int i = 0; i < total; i++)
        {
            CharacterDataSO data = PlayerSelectionData.ChosenCharacters[i];
            if (data != null && data.prefab != null)
            {
                GameObject obj = Instantiate(data.prefab, spawnPositions[i], Quaternion.identity);
                obj.name = data.nombre;

                string dynamicLayerName = "Portrait_char" + (i + 1);
                int layerID = LayerMask.NameToLayer(dynamicLayerName);
                if (layerID == -1) layerID = 0;

                ConfigurarVisualesRecursivo(obj, layerID, i);
                AjustarCamaraSlot(obj, i);

                if (UIManager_Combat.instance != null) UIManager_Combat.instance.AsignarSlotAPersonaje(obj, i);

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
        if (index >= portraitCameras.Length || portraitCameras[index] == null) return;
        Transform anchor = personaje.transform.Find("FaceAnchor");
        if (anchor != null)
        {
            Camera cam = portraitCameras[index];
            Vector3 nuevaPos = anchor.position;
            nuevaPos.z = cam.transform.position.z;
            cam.transform.position = nuevaPos;
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
        foreach (Transform child in obj.transform) ConfigurarVisualesRecursivo(child.gameObject, newLayer, exactOrder);
    }
}