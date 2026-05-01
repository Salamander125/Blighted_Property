using UnityEngine;
using System.Collections.Generic;

public class Enemy_Spawn : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Grid_Controller gridController;
    [SerializeField] private Enemy_pool[] enemyPool;
    [SerializeField] private int minEnemies;
    [SerializeField] private int maxEnemies;

    // Generación Aleatoria (Para Pisos Nuevos)
    public void SpawnRandomEnemies()
    {
        if (enemyPool == null || enemyPool.Length == 0) return;

        List<GameObject> enemyDeck = new List<GameObject>();

        foreach (var card in enemyPool)
        {
            for (int i = 0; i < card.copiesInDeck; i++)
            {
                enemyDeck.Add(card.prefab);
            }
        }

        for (int i = 0; i < enemyDeck.Count; i++)
        {
            GameObject temp = enemyDeck[i];
            int randomIndex = Random.Range(i, enemyDeck.Count);
            enemyDeck[i] = enemyDeck[randomIndex];
            enemyDeck[randomIndex] = temp;
        }

        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            if (enemyDeck.Count == 0) break;

            List<Vector2Int> emptyCells = new List<Vector2Int>();
            List<Vector2Int> availableWithSpace = new List<Vector2Int>();

            int ancho = gridController.matrix.GetLength(0);
            int alto = gridController.matrix.GetLength(1);

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    GridCell cell = gridController.matrix[x, y];
                    if (cell == null || cell.isBlocked) continue;

                    if (cell.occupants.Count == 0)
                        emptyCells.Add(new Vector2Int(x, y));
                    else if (cell.CanEnter())
                        availableWithSpace.Add(new Vector2Int(x, y));
                }
            }

            Vector2Int chosenPos;
            if (emptyCells.Count > 0)
            {
                chosenPos = emptyCells[Random.Range(0, emptyCells.Count)];
            }
            else if (availableWithSpace.Count > 0)
            {
                chosenPos = availableWithSpace[Random.Range(0, availableWithSpace.Count)];
            }
            else
            {
                Debug.LogWarning("Enemy_Spawn: ¡No hay más espacio en el tablero para tantos enemigos!");
                break;
            }

            GameObject prefabToSpawn = enemyDeck[0];
            enemyDeck.RemoveAt(0);

            SpawnEnemyAt(chosenPos, prefabToSpawn);
        }
    }

    // ==========================================
    // NUEVO: CARGAR ENEMIGOS DESDE PARTIDA GUARDADA
    // ==========================================
    public void SpawnLoadedEnemies(List<EnemySaveData> savedEnemies)
    {
        if (enemyPool == null || enemyPool.Length == 0 || savedEnemies == null) return;

        // Diccionario para buscar rápidamente el prefab por su nombre/ID
        Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();
        foreach (var poolItem in enemyPool)
        {
            if (!prefabDictionary.ContainsKey(poolItem.enemyName))
            {
                prefabDictionary.Add(poolItem.enemyName, poolItem.prefab);
            }
        }

        foreach (EnemySaveData data in savedEnemies)
        {
            if (prefabDictionary.TryGetValue(data.enemyID, out GameObject prefabToSpawn))
            {
                Vector2Int pos = new Vector2Int(data.gridX, data.gridY);

                GameObject enemyObject = Instantiate(prefabToSpawn);
                Enemy enemy = enemyObject.GetComponent<Enemy>();

                if (enemy != null)
                {
                    // 1. Inyección masiva de stats y estados alterados
                    enemy.InitializeStatsIfNeeded();
                    enemy.CargarSaveData(data);

                    // 2. Colocar en el grid con el SEGURO ANTI-BUFO (triggerEffect = false)
                    gridController.PlaceEnemy(enemy, pos, false);

                    // 3. Registrar en el sistema de turnos
                    if (Turn_Controller.instance != null)
                    {
                        Turn_Controller.instance.allEntities.Add(enemy);
                    }
                }
            }
            else
            {
                Debug.LogError($"Enemy_Spawn: No se encontró el prefab para '{data.enemyID}'. Asegúrate de rellenar los IDs.");
            }
        }
    }

    private void SpawnEnemyAt(Vector2Int pos, GameObject prefabToSpawn)
    {
        GameObject enemyObject = Instantiate(prefabToSpawn);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
        {
            // Generación normal sí dispara el efecto de la casilla (true)
            gridController.PlaceEnemy(enemy, pos, true);

            // Registrar en el sistema de turnos (Los enemigos nuevos también deben registrarse)
            if (Turn_Controller.instance != null)
            {
                Turn_Controller.instance.allEntities.Add(enemy);
            }
        }
    }

    public void SetNewPool(Enemy_pool[] newPool)
    {
        enemyPool = newPool;
    }
}