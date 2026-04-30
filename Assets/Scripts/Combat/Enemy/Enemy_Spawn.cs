using UnityEngine;
using System.Collections.Generic;

// Se encarga de instanciar los enemigos al inicio de cada piso mediante un sistema de "Mazo".
public class Enemy_Spawn : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Grid_Controller gridController;
    [SerializeField] private Enemy_pool[] enemyPool;
    [SerializeField] private int minEnemies;
    [SerializeField] private int maxEnemies;

    public void SpawnRandomEnemies()
    {
        if (enemyPool == null || enemyPool.Length == 0) return;

        // --- CREACIÓN Y BARAJADO DEL MAZO ---
        List<GameObject> enemyDeck = new List<GameObject>();

        // Llenamos la bolsa opaca con la cantidad exacta de enemigos configurada
        foreach (var card in enemyPool)
        {
            for (int i = 0; i < card.copiesInDeck; i++)
            {
                enemyDeck.Add(card.prefab);
            }
        }

        // Agitamos la bolsa (Algoritmo de barajado Fisher-Yates)
        for (int i = 0; i < enemyDeck.Count; i++)
        {
            GameObject temp = enemyDeck[i];
            int randomIndex = Random.Range(i, enemyDeck.Count);
            enemyDeck[i] = enemyDeck[randomIndex];
            enemyDeck[randomIndex] = temp;
        }

        // --- FASE DE GENERACIÓN EN EL TABLERO ---
        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            // Medida de seguridad: Si nos quedamos sin cartas en la bolsa, dejamos de generar
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

            // Sacamos la primera carta de la parte superior del mazo ya barajado
            GameObject prefabToSpawn = enemyDeck[0];
            enemyDeck.RemoveAt(0);

            // Le pasamos ese prefab específico a la función de spawn
            SpawnEnemyAt(chosenPos, prefabToSpawn);
        }
    }

    private void SpawnEnemyAt(Vector2Int pos, GameObject prefabToSpawn)
    {
        GameObject enemyObject = Instantiate(prefabToSpawn);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy != null)
        {
            gridController.PlaceEnemy(enemy, pos);
        }
    }

    public void SetNewPool(Enemy_pool[] newPool)
    {
        enemyPool = newPool;
    }
}