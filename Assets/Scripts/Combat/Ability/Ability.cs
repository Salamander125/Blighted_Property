using UnityEngine;

public enum AbilityShape { Single, Horizontal, Vertical, Cross, XShape, Square }
public enum MovementType { Free, LockedAtCenter, RowOnly, ColumnOnly }

[System.Serializable]
public class Ability
{
    [Header("Datos de la API")]
    public string ownerCharacter; // Ej: "Merida"

    [Header("Datos Básicos")]
    public string nombre;
    public int damage;
    public int manaCost;
    public int healAmount;
    public bool targetsAllies;

    [Header("Forma de la Habilidad")]
    public AbilityShape shape;
    public int range = 1;

    [Header("Estados")]
    public bool aplicaEstado;
    public State.StateType tipoEstado;
    public int duracionEstado;
    public int potenciaEstado;

    [Header("Movimiento y Visuals")]
    public Vector2Int moveOffset;
    public Vector3 spawnOffset;

    // --- EL PUENTE DEL PREFAB ---
    public string prefabPath; // El nombre del archivo que viene de la base de datos

    [System.NonSerialized] // Ocultamos el prefab del Inspector porque se llenará solo
    public GameObject prefab;

    [Header("Ajustes de Prefab")]
    public bool prefabPorCasilla = true;

    [Header("Ajustes de Secuencia")]
    public bool secuencial = false;
    public float delayEntreCasillas = 0.1f;
    public bool esUnitarget;

    [Header("Restricciones del Selector")]
    public MovementType tipoMovimiento;

    /// <summary>
    /// Busca el archivo visual en la carpeta Resources de Unity basándose en el nombre de la BD.
    /// </summary>
    public void CargarPrefabDesdeResources()
    {
        if (!string.IsNullOrEmpty(prefabPath))
        {
            prefab = Resources.Load<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"¡Error! La API pidió el prefab '{prefabPath}' para la habilidad '{nombre}', pero no existe en la carpeta Resources.");
            }
        }
    }
}