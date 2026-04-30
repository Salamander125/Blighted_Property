using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy_Controller : MonoBehaviour
{
    public bool isTurn = false;
    private bool hasFinishedActing = false;

    private Enemy enemy;
    private Grid_Controller grid;

    [Header("Tiempos Cinemáticos (Ataque Enemigo)")]
    [SerializeField] private float tiempoEnfoqueYTexto = 1.2f;
    [SerializeField] private float tiempoAnimacionHardcoded = 0.8f;
    [SerializeField] private float tiempoRegresoCamara = 0.5f;
    [SerializeField] private float tiempoPostDaño = 0.5f;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        grid = FindFirstObjectByType<Grid_Controller>();
    }

    private void Update()
    {
        if (!isTurn || enemy.isBusy || hasFinishedActing) return;

        hasFinishedActing = true;
        StartCoroutine(ThinkAndAct());
    }

    private IEnumerator ThinkAndAct()
    {
        yield return new WaitForSeconds(0.6f);

        // FASE 1: DECIDIR LA ACCIÓN
        EnemyAbilityData accionElegida = DecidirAccion();

        // FASE 2: BUSCAR OBJETIVO (IA 2.0)
        Entity objetivo = BuscarObjetivoInteligente(accionElegida);

        if (objetivo == null)
        {
            EndEnemyTurn();
            yield break;
        }

        // FASE 3: MOVIMIENTO TÁCTICO INTELIGENTE (IA 2.0)
        yield return StartCoroutine(MovimientoInteligente(objetivo, accionElegida));

        // FASE 4: SECUENCIA CINEMÁTICA DE ATAQUE
        string nombreAtacante = enemy.name.Replace("(Clone)", "").Trim();
        string nombreObjetivo = objetivo.name.Replace("(Clone)", "").Trim();

        string message = "";
        if (accionElegida != null)
        {
            message = $"{nombreAtacante} usa <color=red>{accionElegida.abilityName}</color> contra {nombreObjetivo}";
        }
        else
        {
            message = $"{nombreAtacante} ataca a {nombreObjetivo}";
        }

        if (CameraFocus.instance != null) CameraFocus.instance.EnfocarAtacanteCine(enemy);
        if (BattleLogUI.instance != null) StartCoroutine(BattleLogUI.instance.ShowLogRoutine(message, enemy.transform));

        yield return new WaitForSeconds(tiempoEnfoqueYTexto);
        yield return new WaitForSeconds(tiempoAnimacionHardcoded);

        if (BattleLogUI.instance != null) BattleLogUI.instance.ClearLog();
        if (CameraFocus.instance != null) CameraFocus.instance.ResetearCamara();

        yield return new WaitForSeconds(tiempoRegresoCamara);

        if (accionElegida != null)
        {
            yield return StartCoroutine(LogicaHabilidadSinTexto(accionElegida, objetivo));
        }
        else
        {
            yield return StartCoroutine(LogicaAtaqueBasicoSinTexto(objetivo));
        }

        yield return new WaitForSeconds(tiempoPostDaño);
        EndEnemyTurn();
    }

    // ==========================================
    // IA 2.0: SELECCIÓN DE OBJETIVO Y MOVIMIENTO
    // ==========================================

    private EnemyAbilityData DecidirAccion()
    {
        if (enemy.abilities == null || enemy.abilities.Count == 0) return null;
        foreach (var habilidad in enemy.abilities)
        {
            if (Random.Range(0, 100) < habilidad.chanceToUse) return habilidad;
        }
        return null;
    }

    /// <summary>
    /// El enemigo ataca aleatoriamente, A MENOS que un aliado esté bajo de vida o sin defensa.
    /// </summary>
    private Entity BuscarObjetivoInteligente(EnemyAbilityData habilidad)
    {
        List<Entity> jugadoresVivos = Turn_Controller.instance.allEntities.Where(e => e.faction == Faction.Player && e.CurrentLife > 0).ToList();

        // Si la habilidad es para curarse a sí mismo o a un aliado, mantenemos la lógica básica
        if (habilidad != null && (habilidad.targetType == TargetType.Self || habilidad.targetType == TargetType.Ally))
        {
            List<Entity> aliadosVivos = Turn_Controller.instance.allEntities.Where(e => e.faction == Faction.Enemy && e.CurrentLife > 0).ToList();
            if (habilidad.targetType == TargetType.Self) return enemy;
            return aliadosVivos.OrderBy(e => e.CurrentLife).FirstOrDefault();
        }

        if (jugadoresVivos.Count == 0) return null;

        Entity mejorObjetivo = null;
        int mejorPuntuacion = -9999;

        foreach (Entity jugador in jugadoresVivos)
        {
            // 1. Base aleatoria (Para que si ambos están sanos, elija a uno al azar)
            int puntuacion = Random.Range(0, 30);

            // 2. Regla: Jugador por debajo del 50% de vida
            if (jugador.CurrentLife <= jugador.MaxLife * 0.5f)
            {
                puntuacion += 100; // Prioridad casi absoluta
            }

            // 3. Regla: Jugador con debufo de defensa (Su defensa actual es menor que la base)
            if (jugador.CurrentDefense < jugador.BaseDefense)
            {
                puntuacion += 80; // Prioridad muy alta
            }

            if (puntuacion > mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejorObjetivo = jugador;
            }
        }

        return mejorObjetivo;
    }

    /// <summary>
    /// Analiza el entorno y decide si quedarse quieto o moverse a una casilla adyacente según sus necesidades.
    /// </summary>
    private IEnumerator MovimientoInteligente(Entity target, EnemyAbilityData habilidad)
    {
        if (habilidad != null && habilidad.targetType != TargetType.Enemy) yield break; // No se mueve si se cura

        Vector2Int mejorCasilla = enemy.GridPosition;
        int mejorPuntuacion = -9999;

        // Opciones: Quedarse quieto, Arriba, Abajo, Izquierda, Derecha
        Vector2Int[] movimientos = {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        foreach (Vector2Int offset in movimientos)
        {
            Vector2Int posEvaluar = enemy.GridPosition + offset;

            // Filtros de validez: Si no existe o está ocupada (y no es nuestra propia casilla), la ignoramos
            if (!grid.IsValidCell(posEvaluar)) continue;
            if (offset != Vector2Int.zero && !grid.matrix[posEvaluar.x, posEvaluar.y].CanEnter()) continue;

            int score = 0;
            GridCell celda = grid.matrix[posEvaluar.x, posEvaluar.y];
            GridCell.CellEffectType efecto = celda.currentEffect;

            // --- A. EVALUAR DISTANCIA (Acercarse da puntos base) ---
            float distanciaAlObjetivo = Vector2Int.Distance(posEvaluar, target.GridPosition);
            score -= (int)(distanciaAlObjetivo * 15); // Penaliza estar lejos del objetivo

            // --- B. EVALUAR CASILLA DE ATAQUE ---
            if (efecto == GridCell.CellEffectType.BuffAttack) score += 40;

            // --- C. EVALUAR VELOCIDAD ---
            if (efecto == GridCell.CellEffectType.BuffSpeed)
            {
                // Solo la busca si es más lento que el objetivo, y si el bufo (asumimos +5) lo haría más rápido
                if (enemy.CurrentSpeed < target.CurrentSpeed && (enemy.CurrentSpeed + 5) > target.CurrentSpeed)
                {
                    score += 60;
                }
            }

            // --- D. EVALUAR VIDA (Modo Supervivencia / Kamikaze) ---
            if (enemy.CurrentLife <= enemy.MaxLife * 0.4f)
            {
                if (efecto == GridCell.CellEffectType.BuffDefense || efecto == GridCell.CellEffectType.Heal)
                {
                    score += 80; // Prioridad máxima sobrevivir
                }
                else
                {
                    // Kamikaze: Le da completamente igual la defensa, suma muchos puntos por estar muy cerca
                    score -= (int)(distanciaAlObjetivo * 30);
                }
            }

            // --- E. EVALUAR RIESGOS (Veneno, Fuego, Debuffs) ---
            bool esPeligrosa = efecto == GridCell.CellEffectType.Fire || efecto == GridCell.CellEffectType.Poison ||
                               efecto == GridCell.CellEffectType.DebuffAttack || efecto == GridCell.CellEffectType.DebuffDefense ||
                               efecto == GridCell.CellEffectType.DebuffSpeed;
            if (esPeligrosa)
            {
                // Si está sano, el riesgo le da un poco igual (-10). Si está herido, huye despavorido (-100)
                if (enemy.CurrentLife > enemy.MaxLife * 0.6f) score -= 10;
                else score -= 100;
            }

            // --- REGISTRAR LA MEJOR OPCIÓN ---
            if (score > mejorPuntuacion)
            {
                mejorPuntuacion = score;
                mejorCasilla = posEvaluar;
            }
        }

        // --- EJECUCIÓN DEL MOVIMIENTO ---
        Vector2Int offsetFinal = mejorCasilla - enemy.GridPosition;
        if (offsetFinal != Vector2Int.zero) // Si la mejor decisión es moverse (no quedarse quieto)
        {
            yield return grid.MoveEnemyCoroutine(enemy, offsetFinal);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // ==========================================
    // LÓGICA DE EJECUCIÓN PURA 
    // ==========================================

    private IEnumerator LogicaHabilidadSinTexto(EnemyAbilityData habilidad, Entity objetivo)
    {
        if (habilidad.animationPrefab != null) Instantiate(habilidad.animationPrefab, objetivo.transform.position, Quaternion.identity);
        if (habilidad.heal > 0) objetivo.HealCurrentLife(habilidad.heal);
        if (habilidad.damage > 0)
        {
            bool acerto = enemy.DoDamage(habilidad.damage, objetivo);
            if (acerto && habilidad.appliesState && objetivo.CurrentLife > 0)
                State.instance.ApplyNewStatus(objetivo, habilidad.stateToApply, habilidad.stateDuration, habilidad.stateIntensity);
        }
        else if (habilidad.appliesState)
        {
            State.instance.ApplyNewStatus(objetivo, habilidad.stateToApply, habilidad.stateDuration, habilidad.stateIntensity);
        }
        yield break;
    }

    private IEnumerator LogicaAtaqueBasicoSinTexto(Entity target)
    {
        float distancePenalty = 1f - (enemy.GridPosition.y * 0.3f);
        int finalDamage = Mathf.RoundToInt(enemy.CurrentAttack * distancePenalty);
        enemy.DoDamage(finalDamage, target);
        yield break;
    }

    private void EndEnemyTurn()
    {
        isTurn = false;
        hasFinishedActing = false;
        Turn_Controller.instance.EndTurn();
    }
}