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
    [SerializeField] public string enemyID;


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

    // ==========================================
    // SISTEMA DE GUARDADO Y CARGA (ENEMIGOS)
    // ==========================================

    /// <summary>
    /// Empaqueta todas las estadísticas, efectos y la POSICIÓN del enemigo para guardarlo en disco.
    /// </summary>
    public EnemySaveData GenerarSaveData()
    {
        InitializeStatsIfNeeded();

        EnemySaveData data = new EnemySaveData
        {
            enemyID = this.enemyID,
            gridX = this.GridPosition.x,
            gridY = this.GridPosition.y,

            // Stats completas heredadas de Entity
            currentLife = this._currentLife,
            currentMana = this._currentMana,
            maxLife = this._maxLife,
            maxMana = this._maxMana,
            attack = this._currentAttack,
            defense = this._currentDefense,
            speed = this._currentSpeed,
            critChance = this._currentCritChance,
            evasion = this._currentEvasion,
            activeEffects = new List<SavedEffect>()
        };

        // Guardamos los efectos limpios
        foreach (var effect in activeEffects)
        {
            data.activeEffects.Add(new SavedEffect
            {
                type = effect.type,
                duration = effect.duration,
                intensity = effect.intensity
            });
        }

        return data;
    }

    /// <summary>
    /// Desempaqueta y reconstruye al enemigo. La posición se la inyecta el Enemy_Spawn.
    /// </summary>
    public void CargarSaveData(EnemySaveData savedData)
    {
        _currentLife = savedData.currentLife;
        _currentMana = savedData.currentMana;
        _maxLife = savedData.maxLife;
        _maxMana = savedData.maxMana;
        _currentAttack = savedData.attack;
        _currentDefense = savedData.defense;
        _currentSpeed = savedData.speed;
        _currentCritChance = savedData.critChance;
        _currentEvasion = savedData.evasion;

        // Reconstrucción de Estados Alterados
        activeEffects.Clear();
        if (savedData.activeEffects != null && savedData.activeEffects.Count > 0)
        {
            foreach (var effectData in savedData.activeEffects)
            {
                if (State.instance != null)
                {
                    State.instance.ApplyNewStatus(this, effectData.type, effectData.duration, effectData.intensity);
                }
            }
        }

        if (_currentLife <= 0)
        {
            Die();
        }
    }
}