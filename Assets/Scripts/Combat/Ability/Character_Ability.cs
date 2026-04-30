using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum MeridaAbility
{
    ArteCallejero,
}

public class Character_Ability : MonoBehaviour
{
    public static Character_Ability instance;

    [Header("Identidad del Personaje")]
    [Tooltip("Debe coincidir EXACTAMENTE con el owner_character de la Base de Datos")]
    public string characterName = "Merida";

    [Header("Habilidades Dinámicas")]
    // Cambiamos el array fijo por una lista dinámica
    public List<Ability> abilities = new List<Ability>();

    private Entity player;

    public void Start()
    {
        instance = this;
        player = GetComponent<Entity>();
    }
    public void CargarHabilidadesDeAPI()
    {
        if (ApiManager.instance == null || ApiManager.instance.habilidadesTotales.Count == 0)
        {
            Debug.LogWarning($"[{characterName}] intentó cargar habilidades, pero la API no está lista o está vacía.");
            return;
        }

        // Buscamos en la lista global de la API todas las que sean de este personaje
        abilities = ApiManager.instance.habilidadesTotales.FindAll(h => h.ownerCharacter == characterName);

        Debug.Log($"<color=magenta>Magia lista: {characterName} ha cargado {abilities.Count} habilidades desde el servidor.</color>");
    }
    public IEnumerator UseAbilityRoutine(Ability selectedAbility, Vector2Int centerPos, Grid_Controller gridController, Entity targetEntity = null, System.Action onComplete = null)
    {
        if (selectedAbility.targetsAllies)
        {
            player.ConsumMana(selectedAbility.manaCost);

            // Validamos que el objetivo exista (cuidado de nulos) y esté vivo
            if (targetEntity != null && targetEntity.CurrentLife > 0)
            {
                // 1. Curación 
                if (selectedAbility.healAmount > 0)
                {
                    // Llamamos a la función que acabamos de crear en Entity
                    targetEntity.HealCurrentLife(selectedAbility.healAmount);
                }

                // 2. Aplicar Estados (Buffos)
                if (selectedAbility.aplicaEstado)
                {
                    State.instance.ApplyNewStatus(targetEntity, selectedAbility.tipoEstado, selectedAbility.duracionEstado, selectedAbility.potenciaEstado);
                }

                // 3. Visuales: Spawneamos el prefab
                if (selectedAbility.prefab == null)
                {
                    Debug.LogError($"¡OJO! El prefab de {selectedAbility.nombre} es NULL. Revisa la ruta en la BDD y la carpeta Resources.");
                }
                else
                {
                    GameObject efecto = Instantiate(selectedAbility.prefab, targetEntity.transform.position + selectedAbility.spawnOffset, Quaternion.identity);
                    Debug.Log($"Instanciado {efecto.name} en la posición {efecto.transform.position}");
                }

                Debug.Log($"<color=green>Has lanzado {selectedAbility.nombre} sobre {targetEntity.name}</color>");
            }

            // Esperamos a que pase el efecto visual y pasamos turno
            yield return new WaitForSeconds(0.6f);
            onComplete?.Invoke();
          
            // El código nunca llegará a la parte del Grid.
            yield break;
        }

        // Ya no dependemos de un target Enemy, usamos directamente centerPos
        List<Vector2Int> affectedPositions = GetAffectedCells(centerPos, selectedAbility);

        player.ConsumMana(selectedAbility.manaCost);

        if (!selectedAbility.prefabPorCasilla && selectedAbility.prefab != null)
        {
            Instantiate(
                selectedAbility.prefab,
                gridController.matrix[centerPos.x, centerPos.y].transform.position + selectedAbility.spawnOffset,
                Quaternion.identity                
            );
        }

        // Procesar cada celda afectada
        foreach (Vector2Int pos in affectedPositions)
        {
            if (!gridController.IsValidCell(pos)) continue;

            GridCell currentCell = gridController.matrix[pos.x, pos.y];

            // --- LÓGICA DE PREFAB MÚLTIPLE ---
            if (selectedAbility.prefabPorCasilla && selectedAbility.prefab != null)
            {
                Instantiate(
                    selectedAbility.prefab,
                    currentCell.transform.position + selectedAbility.spawnOffset,
                    Quaternion.identity
                );
            }

            // Aplicar Daño y Estados a los que estén en la celda (si los hay)
            List<Enemy> targetsInCell = new List<Enemy>(currentCell.occupants);
            foreach (Enemy e in targetsInCell)
            {
                if (e == null) continue;

                bool acerto = player.DoDamage(selectedAbility.damage, e);

                // 2. APLICAR ESTADOS: Solo si el golpe acertó y el enemigo sigue vivo
                if (acerto && e != null && selectedAbility.aplicaEstado)
                {
                    State.instance.ApplyNewStatus(e, selectedAbility.tipoEstado, selectedAbility.duracionEstado, selectedAbility.potenciaEstado);
                }
            }

            // --- CONTROL SECUENCIAL ---
            if (selectedAbility.secuencial)
            {
                yield return new WaitForSeconds(selectedAbility.delayEntreCasillas);
            }
        }

        if (!selectedAbility.secuencial)
        {
            yield return new WaitForSeconds(0.6f);
        }

        // Movimiento (Vórtice/Empujón)
        if (selectedAbility.moveOffset != Vector2Int.zero)
        {
            List<Coroutine> movements = new List<Coroutine>();
            foreach (Vector2Int pos in affectedPositions)
            {
                if (!gridController.IsValidCell(pos)) continue;
                foreach (Enemy e in new List<Enemy>(gridController.matrix[pos.x, pos.y].occupants))
                    movements.Add(StartCoroutine(gridController.MoveEnemyCoroutine(e, selectedAbility.moveOffset)));
            }
            foreach (var m in movements) yield return m;
        }

        onComplete?.Invoke();
    }

    public List<Vector2Int> GetAffectedCells(Vector2Int center, Ability ability)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        cells.Add(center); // El centro siempre se incluye

        if (ability.shape == AbilityShape.Single) return cells;

        for (int i = 1; i <= ability.range; i++)
        {
            switch (ability.shape)
            {
                case AbilityShape.Horizontal: // Forma: —
                    cells.Add(center + new Vector2Int(i, 0));
                    cells.Add(center + new Vector2Int(-i, 0));
                    break;

                case AbilityShape.Vertical: // Forma: |
                    cells.Add(center + new Vector2Int(0, i));
                    cells.Add(center + new Vector2Int(0, -i));
                    break;

                case AbilityShape.Cross: // Forma: +
                    cells.Add(center + new Vector2Int(i, 0));
                    cells.Add(center + new Vector2Int(-i, 0));
                    cells.Add(center + new Vector2Int(0, i));
                    cells.Add(center + new Vector2Int(0, -i));
                    break;

                case AbilityShape.XShape: // Forma: X
                    cells.Add(center + new Vector2Int(i, i));
                    cells.Add(center + new Vector2Int(i, -i));
                    cells.Add(center + new Vector2Int(-i, i));
                    cells.Add(center + new Vector2Int(-i, -i));
                    break;

                case AbilityShape.Square: // Área completa (3x3, 5x5, etc)
                    for (int x = -i; x <= i; x++)
                        for (int y = -i; y <= i; y++)
                            if (!cells.Contains(center + new Vector2Int(x, y)))
                                cells.Add(center + new Vector2Int(x, y));
                    break;
            }
        }
        return cells;
    }
}