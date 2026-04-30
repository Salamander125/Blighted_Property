using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid_Controller : MonoBehaviour
{
    public static Grid_Controller instance;

    [Header("Configuración del Grid")]
    public GridCell[,] matrix = new GridCell[3, 3];
    public float moveSpeed = 5f;

    [SerializeField] private GameObject cellPrefab;

    private Vector3[,] worldPositions = new Vector3[3, 3]
    {
        { new Vector3(-0.5f,  2f, 0), new Vector3(-3f,   2f, 0), new Vector3(-5.5f,  2f, 0) },
        { new Vector3(-1f, -0.5f, 0), new Vector3(-3.5f,-0.5f,0), new Vector3(-6f,  -0.5f,0) },
        { new Vector3(-1.5f,-3f, 0), new Vector3(-4f,  -3f, 0), new Vector3(-6.5f, -3f, 0) }
    };

    private void Awake()
    {
        instance = this;
        InitializeGrid();

        ToggleGridVisuals(false);
        GenerateRandomFloorEffects(3);
    }

    private void InitializeGrid()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, worldPositions[x, y], Quaternion.identity, transform);
                GridCell cell = cellObj.GetComponent<GridCell>();
                cell.coords = new Vector2Int(x, y);
                matrix[x, y] = cell;
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos) => worldPositions[gridPos.x, gridPos.y];

    public void PlaceEnemy(Enemy enemy, Vector2Int gridPosition)
    {
        if (!IsValidCell(gridPosition)) return;

        GridCell targetCell = matrix[gridPosition.x, gridPosition.y];
        targetCell.AddOccupant(enemy);
        enemy.SetGridPosition(gridPosition);

        enemy.transform.position = targetCell.GetPositionForOccupant(enemy) + enemy.visualOffset;
        UpdateSortingOrder(enemy);

        // Disparamos la trampa al aparecer
        targetCell.ApplyEffectToEntity(enemy);
    }

    public IEnumerator MoveEnemyCoroutine(Enemy enemy, Vector2Int movementOffset)
    {
        if (enemy == null || enemy.isBusy) yield break;

        Vector2Int oldPos = enemy.GridPosition;
        Vector2Int targetGridPos = oldPos + movementOffset;

        if (!CanMove(enemy, movementOffset)) yield break;

        enemy.isBusy = true;

        Vector3 cellCenterWithOffset = matrix[targetGridPos.x, targetGridPos.y].transform.position + enemy.visualOffset;

        while (Vector3.Distance(enemy.transform.position, cellCenterWithOffset) > 0.01f)
        {
            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                cellCenterWithOffset,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        enemy.transform.position = cellCenterWithOffset;

        matrix[oldPos.x, oldPos.y].RemoveOccupant(enemy);
        matrix[targetGridPos.x, targetGridPos.y].AddOccupant(enemy);
        enemy.SetGridPosition(targetGridPos);
        UpdateSortingOrder(enemy);

        // Disparamos la trampa al pisar
        matrix[targetGridPos.x, targetGridPos.y].ApplyEffectToEntity(enemy);

        yield return StartCoroutine(matrix[targetGridPos.x, targetGridPos.y].ReorganizeOccupants());

        enemy.isBusy = false;
    }

    public bool CanMove(Enemy enemy, Vector2Int offset)
    {
        Vector2Int target = enemy.GridPosition + offset;
        return IsValidCell(target) && matrix[target.x, target.y].CanEnter();
    }

    public void UpdateSortingOrder(Enemy enemy)
    {
        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            int cellIndex = matrix[enemy.GridPosition.x, enemy.GridPosition.y].occupants.IndexOf(enemy);
            sr.sortingOrder = (enemy.GridPosition.x * 100) + enemy.baseSortingOrder + cellIndex;
        }
    }

    public bool IsValidCell(Vector2Int pos) => pos.x >= 0 && pos.x < 3 && pos.y >= 0 && pos.y < 3;

    public void ClearCell(Vector2Int pos)
    {
        if (IsValidCell(pos)) matrix[pos.x, pos.y].occupants.Clear();
    }

    public void TickAllCells()
    {
        foreach (var cell in matrix) cell.UpdateTick();
    }

    public Enemy GetEnemyAtCell(Vector2Int pos)
    {
        if (!IsValidCell(pos)) return null;
        GridCell cell = matrix[pos.x, pos.y];
        if (cell.occupants.Count > 0) return cell.occupants[0];
        return null;
    }

    public Vector2Int GetFirstEnemyCell()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (matrix[x, y].occupants.Count > 0)
                {
                    if (matrix[x, y].occupants[0].CurrentLife > 0) return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(1, 1);
    }

    public bool IsCellOccupied(Vector2Int pos)
    {
        if (!IsValidCell(pos)) return false;
        GridCell cell = matrix[pos.x, pos.y];
        return cell.occupants.Count > 0 || cell.isBlocked;
    }

    public void ClearAllHighlights()
    {
        foreach (GridCell cell in matrix)
        {
            if (cell != null) cell.SetHighlight(false, Color.white);
        }
    }

    public void HighlightArea(List<Vector2Int> positions, Color color)
    {
        ClearAllHighlights();
        foreach (Vector2Int pos in positions)
        {
            if (IsValidCell(pos)) matrix[pos.x, pos.y].SetHighlight(true, color);
        }
    }

    public Entity GetEntityAt(Vector2Int pos)
    {
        if (!IsValidCell(pos)) return null;
        GridCell cell = matrix[pos.x, pos.y];
        if (cell.occupants.Count > 0) return cell.occupants[0] as Entity;
        return null;
    }

    // ==========================================
    // GENERACIÓN PROCEDURAL DE TABLERO
    // ==========================================

    public void GenerateRandomFloorEffects(int maxSpecialCells = 3)
    {
        Debug.Log($"<color=cyan>--- INICIANDO GENERACIÓN DE {maxSpecialCells} TRAMPAS ---</color>");

        // 1. Limpiamos las trampas del piso anterior
        foreach (GridCell cell in matrix)
        {
            if (cell != null) cell.ChangeCellEffect(GridCell.CellEffectType.Normal);
        }

        // 2. Tiramos dados para las nuevas trampas de forma segura
        int trampasColocadas = 0;
        int intentosMaximos = 50; // Seguridad anti-bucles infinitos

        while (trampasColocadas < maxSpecialCells && intentosMaximos > 0)
        {
            intentosMaximos--;

            int randomX = Random.Range(0, 3);
            int randomY = Random.Range(0, 3);

            // Evitamos poner trampas en la columna central
            if (randomX == 1) continue;

            GridCell targetCell = matrix[randomX, randomY];

            // SOLO colocamos la trampa si esa casilla estaba "Normal" (vacía de trampas)
            if (targetCell.currentEffect == GridCell.CellEffectType.Normal)
            {
                System.Array allEffects = System.Enum.GetValues(typeof(GridCell.CellEffectType));
                int randomIndex = Random.Range(1, allEffects.Length); // Empieza en 1 para saltar 'Normal'
                GridCell.CellEffectType randomEffect = (GridCell.CellEffectType)allEffects.GetValue(randomIndex);

                targetCell.ChangeCellEffect(randomEffect);

                // CHIVATO DE CONSOLA
                Debug.Log($"ÉXITO: Trampa [<color=yellow>{randomEffect}</color>] colocada en la coordenada X:{randomX}, Y:{randomY}");

                // Sumamos 1 solo cuando hemos colocado una trampa con éxito
                trampasColocadas++;
            }
        }

        if (intentosMaximos <= 0)
        {
            Debug.LogWarning("Se agotaron los intentos máximos para poner trampas. ¿El tablero es muy pequeño?");
        }
        else
        {
            Debug.Log($"<color=green>--- GENERACIÓN COMPLETADA: {trampasColocadas} trampas listas ---</color>");
        }
    }

    // ==========================================
    // CONTROL VISUAL DEL TABLERO
    // ==========================================

    // Botón maestro para mostrar u ocultar el tablero entero
    public void ToggleGridVisuals(bool show)
    {
        foreach (GridCell cell in matrix)
        {
            if (cell != null)
            {
                cell.ToggleVisual(show);
            }
        }
    }

    public void ResetGridForNewFloor()
    {
        foreach (GridCell cell in matrix)
        {
            if (cell != null)
            {
                cell.occupants.Clear(); // Limpiamos la lista de la celda
                cell.isBlocked = false; // Desbloqueamos por si acaso
                cell.ChangeCellEffect(GridCell.CellEffectType.Normal); // Quitamos trampas
            }
        }
    }
}