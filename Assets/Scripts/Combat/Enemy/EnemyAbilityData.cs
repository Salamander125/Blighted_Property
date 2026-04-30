using UnityEngine;

// Define a quién va dirigida esta habilidad
public enum TargetType { Enemy, Ally, Self }

[CreateAssetMenu(fileName = "NuevaHabilidadEnemiga", menuName = "Juego/Habilidad Enemigo")]
public class EnemyAbilityData : ScriptableObject
{
    [Header("Identificación")]
    public string abilityName;

    // El efecto visual que aparecerá sobre el objetivo (una explosión, un rayo de luz verde...)
    public GameObject animationPrefab;

    [Header("Toma de Decisiones (IA)")]
    // Si la habilidad es para pegar a Natalia, para curar a otro duende, o para bufarse a sí mismo
    public TargetType targetType;

    // Probabilidad (0 a 100) de que el enemigo decida usar esto en su turno en lugar de un ataque básico
    [Range(0, 100)] public int chanceToUse = 30;

    [Header("Efectos Directos")]
    public int damage; // Si hace daño
    public int heal;   // Si cura vida

    [Header("Efectos Secundarios (Estados)")]
    public bool appliesState = false;
    public State.StateType stateToApply;
    public int stateDuration;
    public int stateIntensity;
}