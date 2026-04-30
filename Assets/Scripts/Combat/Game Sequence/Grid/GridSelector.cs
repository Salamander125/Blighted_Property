using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la lógica visual y de movimiento del cursor (cuadradito blanco) en el grid,
/// respetando las restricciones de habilidades y permitiendo sub-seleccionar entidades en una misma celda.
/// </summary>
public class GridSelector : MonoBehaviour
{
    public static GridSelector instance;
    [Header("Referencias")]
    [SerializeField] private Grid_Controller grid;
    [SerializeField] private Transform selectorVisual;

    private Vector2Int currentPos;
    private int occupantIndex = 0; // Para elegir enemigo dentro de la misma celda con Q y E

    private void Start()
    {
        instance = this;
        // Buscamos el grid al empezar
        grid = FindFirstObjectByType<Grid_Controller>();

        // El cuadradito blanco empieza apagado
        if (selectorVisual != null) selectorVisual.gameObject.SetActive(false);
    }

    // Activa o desactiva el cuadradito visual del suelo
    public void ShowSelector(bool value, Ability skill = null)
    {
        if (selectorVisual == null) return;
        selectorVisual.gameObject.SetActive(value);

        if (value)
        {
            currentPos = new Vector2Int(1, 1); // Siempre empezamos en el centro
            occupantIndex = 0; // Empezamos apuntando al primer enemigo de la celda
            UpdateSelectorVisual();

            Character_Controller controller = FindAnyObjectByType<Character_Controller>();
            if (controller != null)
            {
                // Pintamos el dibujo de la habilidad nada más abrir el selector
                UpdateSelectionArea(controller);
            }
        }
    }

    // Aquí es donde se mueve el selector con las flechas o WASD
    public void HandleSelectorInput(Character_Controller controller)
    {
        if (!selectorVisual.gameObject.activeSelf) return;

        // Comprobamos si el PlayerTurnController está en modo inspección
        PlayerTurnController ptc = controller.GetComponent<PlayerTurnController>();
        bool isInspecting = (ptc != null && ptc.inspecting);

        // --- ACTUALIZACIÓN ---
        // Ahora leemos 'selectedAbility' en lugar de 'selectedAbilitySO'
        Ability skill = isInspecting ? null : controller.selectedAbility;

        MovementType movType = (skill != null) ? skill.tipoMovimiento : MovementType.Free;

        // --- MOVIMIENTO ENTRE CELDAS (Con Restricciones) ---
        Vector2Int delta = Vector2Int.zero;

        // Solo procesamos el movimiento si la habilidad no está bloqueada al centro
        if (movType != MovementType.LockedAtCenter)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) delta = Vector2Int.up;
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) delta = Vector2Int.down;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) delta = Vector2Int.right;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) delta = Vector2Int.left;

            if (delta != Vector2Int.zero)
            {
                // Por defecto dejamos mover, a menos que la restricción diga lo contrario
                bool movimientoPermitido = true;

                // Si la habilidad solo permite moverse en su fila (Eje X)
                if (movType == MovementType.RowOnly && delta.y != 0) movimientoPermitido = false;

                // Si la habilidad solo permite moverse en su columna (Eje Y)
                if (movType == MovementType.ColumnOnly && delta.x != 0) movimientoPermitido = false;

                if (movimientoPermitido)
                {
                    Vector2Int newPos = currentPos + delta;
                    if (grid.IsValidCell(newPos))
                    {
                        currentPos = newPos;
                        occupantIndex = 0; // Si cambiamos de casilla, volvemos al primer enemigo
                        ResetAllEntitiesHighlights(); // Limpiamos el color rojo de los enemigos de la casilla anterior
                    }
                }
            }
        }
        else
        {
            // Si la habilidad obliga a estar en el centro, nos aseguramos de que el cursor no se mueva de ahí
            currentPos = new Vector2Int(1, 1);
        }

        // --- SUB-SELECCIÓN DE ENTIDAD (Ataque básico o Unitarget o Inspección) ---
        // Permitimos navegar con Q y E si es ataque normal, unitarget o SI ESTAMOS INSPECCIONANDO
        if (skill == null || skill.esUnitarget || isInspecting)
        {
            GridCell currentCell = grid.matrix[currentPos.x, currentPos.y];
            int totalOccupants = currentCell.occupants.Count;

            // Si hay más de un enemigo en la misma casilla...
            if (totalOccupants > 1)
            {
                // Usamos Q y E para saltar de un enemigo a otro dentro de la casilla
                if (Input.GetKeyDown(KeyCode.Q)) occupantIndex = (occupantIndex - 1 + totalOccupants) % totalOccupants;
                if (Input.GetKeyDown(KeyCode.E)) occupantIndex = (occupantIndex + 1) % totalOccupants;
            }

            // Pintamos de rojo al enemigo que tenemos seleccionado ahora mismo
            UpdateOccupantHighlight(currentCell);
        }

        // Actualizamos la posición visual del cuadradito y los dibujos rojos del suelo
        UpdateSelectorVisual();
        UpdateSelectionArea(controller);

        // Si pulsamos Enter, confirmamos que queremos pegar ahí (SOLO si no estamos inspeccionando)
        if (!isInspecting && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            ConfirmSelection(controller);
        }
    }

    // Hace que el enemigo seleccionado brille en rojo
    private void UpdateOccupantHighlight(GridCell cell)
    {
        // Primero "apagamos" a todos los de la celda
        foreach (var occ in cell.occupants) if (occ != null) occ.SetHighlight(false);

        // Solo "encendemos" al que estamos apuntando con Q o E
        if (cell.occupants.Count > 0 && occupantIndex < cell.occupants.Count)
        {
            if (cell.occupants[occupantIndex] != null)
                cell.occupants[occupantIndex].SetHighlight(true);
        }
    }

    // Quita el color rojo de todos los personajes del combate
    public void ResetAllEntitiesHighlights()
    {
        foreach (var ent in Turn_Controller.instance.allEntities)
        {
            if (ent != null) ent.SetHighlight(false);
        }
    }

    // Terminamos la selección y mandamos los datos para atacar
    private void ConfirmSelection(Character_Controller controller)
    {
        ResetAllEntitiesHighlights(); // Limpiamos brillos rojos
        grid.ClearAllHighlights(); // Limpiamos el suelo rojo

        // Sacamos cuál es el enemigo exacto al que queremos pegar
        Entity finalTarget = null;
        GridCell cell = grid.matrix[currentPos.x, currentPos.y];
        if (cell.occupants.Count > 0) finalTarget = cell.occupants[occupantIndex];

        // Le pasamos la posición y el bicho concreto al controller del jugador
        controller.GetComponent<PlayerTurnController>().TargetSelected(currentPos, finalTarget);

        // Apagamos el selector
        selectorVisual.gameObject.SetActive(false);
    }

    // Mueve el cuadradito blanco a la posición del grid
    private void UpdateSelectorVisual()
    {
        if (selectorVisual != null && grid != null)
        {
            selectorVisual.position = grid.GetWorldPosition(currentPos);
        }
    }

    // Dibuja las casillas rojas en el suelo según la habilidad
    private void UpdateSelectionArea(Character_Controller controller)
    {
        PlayerTurnController ptc = controller.GetComponent<PlayerTurnController>();
        bool isInspecting = (ptc != null && ptc.inspecting);

        // --- ACTUALIZACIÓN ---
        // Leemos selectedAbility en lugar de selectedAbilitySO
        Ability skill = isInspecting ? null : controller.selectedAbility;
        if (grid == null) return;

        List<Vector2Int> area;

        // --- ACTUALIZACIÓN ---
        // Ahora usamos 'characterAbility' en lugar del viejo 'meridaAbility'
        if (skill != null && controller.characterAbility != null)
        {
            area = controller.characterAbility.GetAffectedCells(currentPos, skill);
        }
        else
        {
            // Si es ataque normal o inspección, solo pintamos la casilla actual
            area = new List<Vector2Int> { currentPos };
        }

        // Le decimos al grid que pinte el área de rojo
        grid.HighlightArea(area, Color.red);
    }

    public Vector2Int GetCurrentPos()
    {
        return currentPos;
    }

    /// <summary>
    /// Devuelve exactamente la entidad que estamos señalando con el selector, 
    /// teniendo en cuenta si hemos navegado con Q/E dentro de la celda.
    /// </summary>
    public Entity GetSelectedEntity()
    {
        GridCell cell = grid.matrix[currentPos.x, currentPos.y];
        if (cell.occupants.Count > 0 && occupantIndex < cell.occupants.Count)
        {
            return cell.occupants[occupantIndex];
        }
        return null;
    }
}