using UnityEngine;
using System.Collections.Generic;

public class Enemy : Entity
{
    // Estado de ocupado (movimiento o animación)
    [HideInInspector] public bool isBusy = false;

    [Header("Ajuste Visual")]
    [Tooltip("Ajuste para centrar el sprite en la celda del Grid")]
    [SerializeField] public Vector3 visualOffset;

    [Header("Recompensa")]
    public int goldValue = 15;

    public int baseSortingOrder = 0;

    // =============================
    // NUEVO: INTELIGENCIA ARTIFICIAL
    // =============================
    public enum EnemyArchetype { Agresivo, Tactico, Soporte }

    [Header("Cerebro e IA")]
    [Tooltip("Agresivo: Busca al débil. Táctico: Busca al más fuerte. Soporte: Random/Ayuda a aliados.")]
    public EnemyArchetype archetype;

    [Tooltip("Lista de habilidades que puede usar. Si decide no usar ninguna, hará un ataque básico.")]
    public List<EnemyAbilityData> abilities = new List<EnemyAbilityData>();

    protected override void Start()
    {
        base.Start();
    }

    public Vector3 GetVisualPosition(Vector3 gridWorldPos)
    {
        return gridWorldPos + visualOffset;
    }

    // =============================
    // GESTIÓN DE MUERTE
    // =============================
    public override void Die()
    {
        // Lógica específica de enemigo: quitar del grid
        if (Grid_Controller.instance != null)
        {
            Grid_Controller.instance.matrix[GridPosition.x, GridPosition.y].RemoveOccupant(this);
        }

        // Lógica común de gestión de turnos y contadores
        if (Turn_Controller.instance != null) Turn_Controller.instance.RemoveEntity(this);

        if (SaveManager.instance != null) SaveManager.instance.totalEnemiesKilled++;

        // Aquí sí destruimos el enemigo definitivamente
        Destroy(gameObject);
    }
}