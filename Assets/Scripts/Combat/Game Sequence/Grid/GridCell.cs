using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    // --- TIPOS DE CASILLAS ESPECIALES ---
    public enum CellEffectType
    {
        Normal, BuffAttack, DebuffAttack, BuffSpeed, DebuffSpeed,
        BuffDefense, DebuffDefense, Fire, Poison, Heal, DoubleGold
    }

    [Header("Efectos de Casilla")]
    public CellEffectType currentEffect = CellEffectType.Normal;
    [Tooltip("El SpriteRenderer del SUELO para teñirlo según su efecto")]
    [SerializeField] private SpriteRenderer floorRenderer;

    [Header("Estado")]
    public List<Enemy> occupants = new List<Enemy>();
    public bool isBlocked = false;
    public int blockedTurns = 0;
    public Vector2Int coords;

    [Header("Configuración Visual")]
    [SerializeField] private GameObject blockVisual; // Un prefab de "X" o suelo roto
    [SerializeField] private float innerOffset = 0.3f; // Distancia entre los 4 enemigos

    [SerializeField] private SpriteRenderer highlightRenderer;

    // Definimos 4 sub-posiciones dentro de la celda
    private Vector3[] localOffsets;

    private void Awake()
    {
        // Posiciones: Arriba-Izquierda, Arriba-Derecha, Abajo-Izquierda, Abajo-Derecha
        localOffsets = new Vector3[]
        {
            new Vector3(-innerOffset, innerOffset, 0),
            new Vector3(innerOffset, innerOffset, 0),
            new Vector3(-innerOffset, -innerOffset, 0),
            new Vector3(innerOffset, -innerOffset, 0)
        };
        if (highlightRenderer != null) highlightRenderer.enabled = false;
        UpdateVisuals();
    }

    public bool CanEnter()
    {
        return !isBlocked && occupants.Count < 4;
    }

    public void UpdateTick()
    {
        if (blockedTurns > 0)
        {
            blockedTurns--;
            if (blockedTurns <= 0) SetBlock(false);
        }

        // Si alguien se queda parado en fuego o curación, recibe el efecto al pasar turno
        foreach (var occ in occupants)
        {
            if (occ != null) ApplyEffectToEntity(occ);
        }
    }

    public void SetBlock(bool value, int duration = 0)
    {
        isBlocked = value;
        blockedTurns = duration;
        if (blockVisual != null) blockVisual.SetActive(value);
    }

    public Vector3 GetPositionForOccupant(Enemy enemy)
    {
        int index = occupants.IndexOf(enemy);
        if (index == -1) index = Mathf.Clamp(occupants.Count, 0, 3);
        return transform.position + localOffsets[index];
    }

    public void AddOccupant(Enemy enemy)
    {
        if (!occupants.Contains(enemy)) occupants.Add(enemy);
    }

    public void RemoveOccupant(Enemy enemy)
    {
        if (occupants.Contains(enemy)) occupants.Remove(enemy);
    }

    public IEnumerator ReorganizeOccupants()
    {
        float elapsed = 0;
        float duration = 0.25f;

        List<Enemy> enemiesToMove = new List<Enemy>(occupants);
        List<Vector3> startPositions = new List<Vector3>();

        foreach (var v in enemiesToMove)
        {
            if (v != null) startPositions.Add(v.transform.position);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curve = t * t * (3f - 2f * t);

            for (int i = 0; i < enemiesToMove.Count; i++)
            {
                if (enemiesToMove[i] == null) continue;

                Vector3 targetSlotPos = (transform.position + localOffsets[i]) + enemiesToMove[i].visualOffset;
                enemiesToMove[i].transform.position = Vector3.Lerp(startPositions[i], targetSlotPos, curve);
            }
            yield return null;
        }

        for (int i = 0; i < occupants.Count; i++)
        {
            if (occupants[i] != null)
            {
                occupants[i].transform.position = (transform.position + localOffsets[i]) + occupants[i].visualOffset;
            }
        }
    }

    public void SetHighlight(bool active, Color color)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = active;
            highlightRenderer.color = new Color(color.r, color.g, color.b, 0.5f);
        }
    }

    // ==========================================
    // SISTEMA DE EFECTOS DE CASILLA
    // ==========================================

    public void ChangeCellEffect(CellEffectType newEffect)
    {
        currentEffect = newEffect;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (floorRenderer == null) return;

        // 1. Guardamos la opacidad original que tú le hayas puesto en el Inspector
        float originalAlpha = floorRenderer.color.a;

        // 2. Preparamos el color base
        Color colorDestino = Color.white;

        switch (currentEffect)
        {
            case CellEffectType.Normal: colorDestino = Color.white; break;
            case CellEffectType.Fire: colorDestino = new Color(1f, 0.4f, 0f); break;
            case CellEffectType.Poison: colorDestino = new Color(0.6f, 0f, 1f); break;
            case CellEffectType.Heal: colorDestino = Color.green; break;
            case CellEffectType.BuffAttack: colorDestino = Color.red; break;
            case CellEffectType.DebuffAttack: colorDestino = new Color(0.5f, 0.3f, 0.3f); break;
            case CellEffectType.BuffSpeed: colorDestino = Color.cyan; break;
            case CellEffectType.DebuffSpeed: colorDestino = new Color(0f, 0f, 0.5f); break;
            case CellEffectType.BuffDefense: colorDestino = Color.gray; break;
            case CellEffectType.DebuffDefense: colorDestino = new Color(0.3f, 0.3f, 0.1f); break;
            case CellEffectType.DoubleGold: colorDestino = Color.yellow; break;
        }

        // 3. Le aplicamos tu opacidad a ese nuevo color y lo asignamos
        colorDestino.a = originalAlpha;
        floorRenderer.color = colorDestino;
    }

    public void ApplyEffectToEntity(Entity entity)
    {
        if (entity == null || currentEffect == CellEffectType.Normal) return;

        switch (currentEffect)
        {
            case CellEffectType.Fire:
                State.instance.ApplyNewStatus(entity, State.StateType.Quemadura, 2, 5);
                break;

            case CellEffectType.Poison:
                State.instance.ApplyNewStatus(entity, State.StateType.Veneno, 3, 2);
                break;

            case CellEffectType.Heal:
                // Usamos el nuevo Heal() para que dispare el popup verde en la UI
                entity.HealCurrentLife(15);
                break;

            case CellEffectType.BuffAttack:
                // Furia sube el ataque, pero baja la defensa (le da un toque más estratégico a la casilla)
                State.instance.ApplyNewStatus(entity, State.StateType.Furia, 2, 10);
                break;

            case CellEffectType.DebuffAttack:
                // Como en la nueva lista quitamos el "Weakened", usamos Ceguera para que tengan un 50% de fallar
                State.instance.ApplyNewStatus(entity, State.StateType.Ceguera, 2, 50);
                break;

            case CellEffectType.DebuffSpeed:
                State.instance.ApplyNewStatus(entity, State.StateType.Pesadez, 2, 5);
                break;

            case CellEffectType.DebuffDefense:
                // Fractura baja la defensa porcentualmente (le pasamos 20 para que sea un 20%)
                State.instance.ApplyNewStatus(entity, State.StateType.Fractura, 2, 20);
                break;

            case CellEffectType.DoubleGold:
                if (entity is Enemy enemyObj)
                {
                    enemyObj.goldValue *= 2;
                    ChangeCellEffect(CellEffectType.Normal); // Se consume la moneda
                }
                break;

            case CellEffectType.BuffSpeed:
                // También le podríamos poner State.StateType.Prisa, pero si quieres que sea instantáneo lo dejamos así:
                entity.ControlSpeed(5, true);
                ChangeCellEffect(CellEffectType.Normal); // Se consume el bufo
                break;

            case CellEffectType.BuffDefense:
                entity.ControlDefense(0.1f, true);
                ChangeCellEffect(CellEffectType.Normal);
                break;
        }
    }

    // ==========================================
    // SISTEMA VISUAL DE LA CASILLA
    // ==========================================

    // Enciende o apaga el dibujo del suelo de esta casilla
    public void ToggleVisual(bool show)
    {
        if (floorRenderer != null)
        {
            floorRenderer.enabled = show;
        }
    }
}