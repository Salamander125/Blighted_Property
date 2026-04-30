using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
    public static State instance;
    private void Awake() => instance = this;

    // --- NUESTRA NUEVA LIBRERÍA DE ESTADOS ---
    public enum StateType
    {
        None,
        // Daño Continuo (DoTs)
        Veneno, Radiacion, Quemadura, Hemorragia,
        // Regeneración (HoTs)
        Vitalidad, Lucidez,
        // Buffs (Mejoras)
        Prisa, Certeza, Coraza, Furia, Espejismo, Sifon, Baluarte,
        // Debuffs (Penalizaciones)
        Fractura, Pesadez, Ceguera, Fragilidad, Fatiga, Silencio,
        // Control de Masas (CC)
        Sueno, Escarcha, Cepo
    }

    [System.Serializable]
    public class ActiveStatus
    {
        public StateType type;
        public int duration;
        public int intensity;

        public ActiveStatus(StateType type, int duration, int intensity)
        {
            this.type = type;
            this.duration = duration;
            this.intensity = intensity;
        }
    }

    // ==========================================
    // 1. EFECTOS DE TURNO (DoTs y HoTs)
    // Se ejecuta al inicio del turno de la entidad
    // ==========================================
    public void ProcessEffects(Entity entity)
    {
        for (int i = entity.activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatus status = entity.activeEffects[i];

            switch (status.type)
            {
                // --- DAÑO (DoTs) ---
                case StateType.Veneno:
                    entity.ApplyEffectDamage(status.intensity, new Color(0.6f, 0f, 1f)); // Morado
                    break;

                case StateType.Radiacion:
                    entity.ApplyEffectDamage(status.intensity, new Color(0.2f, 1f, 0.2f)); // Verde Toxico
                    break;

                case StateType.Quemadura:
                    entity.ApplyEffectDamage(status.intensity, new Color(1f, 0.45f, 0f)); // Naranja
                    break;

                case StateType.Hemorragia:
                    entity.ApplyEffectDamage(status.intensity, Color.red); // Rojo
                    status.intensity += 5; // La hemorragia empeora cada turno si no se cura
                    break;

                // --- CURACIÓN (HoTs) ---
                case StateType.Vitalidad:
                    entity.HealCurrentLife(status.intensity); // Usa el Heal que actualiza la UI
                    break;

                case StateType.Lucidez:
                    entity.RestoreCurrentMana(status.intensity);
                    // Opcional: Podrías hacer un ShowPopup azul aquí para el maná
                    break;
            }

            // Restamos turno
            status.duration--;

            // Limpieza
            if (status.duration <= 0)
            {
                RemoveStatusStats(entity, status);
                entity.activeEffects.RemoveAt(i);
            }
        }
    }

    // ==========================================
    // 2. APLICAR MODIFICADORES DE STATS
    // ==========================================
    public void ApplyNewStatus(Entity target, StateType type, int duration, int intensity)
    {
        ActiveStatus existingStatus = target.activeEffects.Find(s => s.type == type);

        if (existingStatus != null)
        {
            existingStatus.duration = duration;
            // Solo actualizamos la intensidad si el nuevo es más fuerte
            if (intensity > existingStatus.intensity) existingStatus.intensity = intensity;
            return;
        }

        ActiveStatus newStatus = new ActiveStatus(type, duration, intensity);
        target.activeEffects.Add(newStatus);

        switch (type)
        {
            // --- BUFFS ---
            case StateType.Prisa:
                target.ControlSpeed(intensity, true);
                target.ControlEvasion(intensity / 2, true); // Da un poco de evasión también
                break;
            case StateType.Certeza:
                target.ControlCritChance(intensity, true);
                break;
            case StateType.Coraza:
                target.ControlDefense(intensity / 100f, true); // Convertimos int a float
                break;
            case StateType.Furia:
                target.ControlAttack(intensity, true);
                // Aquí podrías guardar la defensa actual para quitarla, pero por simplicidad bajamos un valor fijo
                target.ControlDefense(0.5f, false);
                break;
            case StateType.Espejismo:
                target.ControlEvasion(100, true); // Evasión total
                break;

            // --- DEBUFFS ---
            case StateType.Fractura:
                target.ControlDefense(intensity / 100f, false);
                break;
            case StateType.Pesadez:
                target.ControlSpeed(intensity, false);
                target.ControlEvasion(intensity, false);
                break;
            case StateType.Ceguera:
                target.ControlCritChance(intensity, false);
                break;
            case StateType.Cepo:
                target.ControlSpeed(999, false); // Velocidad a 0 (inmóvil)
                break;
            case StateType.Radiacion:
                target.ControlDefense(0.2f, false); // La radiación baja defensa pasivamente
                break;
        }
    }

    // ==========================================
    // 3. REVERTIR MODIFICADORES DE STATS
    // ==========================================
    private void RemoveStatusStats(Entity target, ActiveStatus status)
    {
        switch (status.type)
        {
            case StateType.Prisa:
                target.ControlSpeed(status.intensity, false);
                target.ControlEvasion(status.intensity / 2, false);
                break;
            case StateType.Certeza:
                target.ControlCritChance(status.intensity, false);
                break;
            case StateType.Coraza:
                target.ControlDefense(status.intensity / 100f, false);
                break;
            case StateType.Furia:
                target.ControlAttack(status.intensity, false);
                target.ControlDefense(0.5f, true);
                break;
            case StateType.Espejismo:
                target.ControlEvasion(100, false);
                break;

            case StateType.Fractura:
                target.ControlDefense(status.intensity / 100f, true);
                break;
            case StateType.Pesadez:
                target.ControlSpeed(status.intensity, true);
                target.ControlEvasion(status.intensity, true);
                break;
            case StateType.Ceguera:
                target.ControlCritChance(status.intensity, true);
                break;
            case StateType.Cepo:
                target.ControlSpeed(999, true); // Devuelve la velocidad
                break;
            case StateType.Radiacion:
                target.ControlDefense(0.2f, true);
                break;
        }
    }

    public void RemoveSpecificState(Entity target, StateType type)
    {
        ActiveStatus status = target.activeEffects.Find(s => s.type == type);
        if (status != null)
        {
            RemoveStatusStats(target, status); // Le devolvemos sus stats normales
            target.activeEffects.Remove(status); // Lo borramos de la lista
        }
    }
}