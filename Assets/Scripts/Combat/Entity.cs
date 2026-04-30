using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Clase base para cualquier ser vivo en el juego (Jugadores y Enemigos).
public class Entity : MonoBehaviour
{
    [Header("Configuración General")]
    public Faction faction;
    public GameObject damagePopupPrefab;
    public Vector3 damagePopupOffset = new Vector3(0, 1.5f, 0);
    public List<State.ActiveStatus> activeEffects = new List<State.ActiveStatus>();
    public Sprite portraitIcon;

    [Header("Base Stats (De Fábrica)")]
    [SerializeField] protected int life, attack, speed, mana;
    [SerializeField] protected float defense;

    [SerializeField] protected int critChance;
    [SerializeField] protected int evasion;

    [SerializeField] float textSpeed = 0.05f;

    [Header("Regeneración Pasiva")]
    public int lifeRegenPerFloor = 0;
    public int manaRegenPerFloor = 0;

    [Header("Muerte y Resurrección")]
    public Sprite deadSprite; // Arrastra aquí el sprite del cadáver desde el inspector
    [HideInInspector] public bool isDead = false;

    // --- ESTADÍSTICAS BASE (Constantes de referencia) ---
    public int BaseMaxLife => life;
    public int BaseMaxMana => mana;
    public int BaseAttack => attack;
    public int BaseSpeed => speed;
    public float BaseDefense => defense;
    public int BaseCritChance => critChance;
    public int BaseEvasion => evasion;

    // --- ESTADÍSTICAS MÁXIMAS MODIFICADAS (El tope actual) ---
    protected int _maxLife;
    protected int _maxMana;

    // NUEVO: Al pedir el valor, nos aseguramos de que esté inicializado primero
    public int MaxLife { get { InitializeStatsIfNeeded(); return _maxLife; } }
    public int MaxMana { get { InitializeStatsIfNeeded(); return _maxMana; } }

    // --- ESTADÍSTICAS ACTUALES ---
    protected int _currentLife;
    protected int _currentMana;
    protected int _currentAttack;
    protected int _currentSpeed;
    protected float _currentDefense;
    protected int _currentCritChance;
    protected int _currentEvasion;

    public int CurrentLife { get { InitializeStatsIfNeeded(); return _currentLife; } }
    public int CurrentMana { get { InitializeStatsIfNeeded(); return _currentMana; } }
    public int CurrentAttack { get { InitializeStatsIfNeeded(); return _currentAttack; } }
    public int CurrentSpeed { get { InitializeStatsIfNeeded(); return _currentSpeed; } }
    public float CurrentDefense { get { InitializeStatsIfNeeded(); return _currentDefense; } }
    public int CurrentCritChance { get { InitializeStatsIfNeeded(); return _currentCritChance; } }
    public int CurrentEvasion { get { InitializeStatsIfNeeded(); return _currentEvasion; } }

    public Vector2Int GridPosition { get; protected set; }
    public void SetGridPosition(Vector2Int pos) => GridPosition = pos;

    public Combat_Controller combat;

    // --- CONTROL DE INICIALIZACIÓN ---
    private bool statsInitialized = false;

    protected virtual void Start()
    {
        InitializeStatsIfNeeded();
    }

    /// <summary>
    /// Esta función blinda la entidad. Carga los stats de fábrica una única vez.
    /// Si el Floor_Manager mete un bufo antes del Start(), esta función se ejecuta sola,
    /// carga las bases, y permite que el bufo se sume encima sin perderse.
    /// </summary>
    public void InitializeStatsIfNeeded()
    {
        if (statsInitialized) return;
        statsInitialized = true;

        if (combat == null) combat = GetComponent<Combat_Controller>();

        _maxLife = life;
        _maxMana = mana;

        _currentLife = _maxLife;
        _currentMana = _maxMana;

        _currentAttack = attack;
        _currentSpeed = speed;
        _currentDefense = defense;

        _currentCritChance = critChance;
        _currentEvasion = evasion;
    }

    // ==========================================
    // MOTOR DE COMBATE
    // ==========================================

    public virtual bool DoDamage(int baseDamage, Entity target)
    {
        if (target == null) return false;
        InitializeStatsIfNeeded();

        // --- ESTADO REACTIVO: CEGUERA ---
        if (activeEffects.Find(s => s.type == State.StateType.Ceguera) != null)
        {
            if (Random.Range(0, 100) < 50)
            {
                ShowPopup("¡Falló!", Color.gray);
                return false;
            }
        }

        bool isCritical = Random.Range(0, 100) < _currentCritChance;
        int finalDamage = isCritical ? baseDamage * 2 : baseDamage;

        // 1. GUARDAMOS LA VIDA DEL OBJETIVO ANTES DEL GOLPE
        int vidaEnemigoAntes = target.CurrentLife;

        // 2. Le enviamos el golpe al objetivo
        bool acerto = target.TakeDamage(finalDamage, isCritical);

        // 3. --- ESTADO REACTIVO: SIFÓN (Robo de vida) ---
        if (acerto)
        {
            var sifon = target.activeEffects.Find(s => s.type == State.StateType.Sifon);

            if (sifon != null)
            {
                // Calculamos el daño EXACTO que le acabamos de quitar (Adiós Overkill)
                int danoRealInfligido = vidaEnemigoAntes - target.CurrentLife;

                // Curamos en base al daño real
                int curacion = Mathf.Max(1, danoRealInfligido * sifon.intensity / 100);
                HealCurrentLife(curacion);
            }
        }

        return acerto;
    }

    public virtual bool TakeDamage(int incomingDamage, bool isCritical = false)
    {
        InitializeStatsIfNeeded();

        // --- REACCIONES DEFENSIVAS ANTES DEL GOLPE ---

        // BALUARTE (Bloquea 1 golpe 100% y desaparece)
        if (activeEffects.Find(s => s.type == State.StateType.Baluarte) != null)
        {
            ShowPopup("¡Bloqueado!", Color.blue);
            State.instance.RemoveSpecificState(this, State.StateType.Baluarte);
            return false;
        }

        // ESPEJISMO (Esquiva 1 golpe 100% y desaparece)
        if (activeEffects.Find(s => s.type == State.StateType.Espejismo) != null)
        {
            ShowPopup("¡Espejismo!", Color.cyan);
            State.instance.RemoveSpecificState(this, State.StateType.Espejismo);
            return false;
        }

        // FRAGILIDAD (El golpe que recibes se vuelve Crítico obligado y el estado desaparece)
        if (activeEffects.Find(s => s.type == State.StateType.Fragilidad) != null)
        {
            isCritical = true;
            State.instance.RemoveSpecificState(this, State.StateType.Fragilidad);
        }

        // Cálculo normal de esquivar (si no tuvimos ayudas mágicas)
        bool dodged = Random.Range(0, 100) < _currentEvasion;
        if (dodged)
        {
            ShowPopup("Esquivado!", Color.cyan);
            Debug.Log($"{name} esquivó el ataque.");
            return false;
        }

        // --- CÁLCULO DE DAÑO ---
        int trueDamage = DamageCalculator(incomingDamage);

        // ESCARCHA (Al recibir un golpe físico, el hielo se rompe y hace daño extra)
        var escarcha = activeEffects.Find(s => s.type == State.StateType.Escarcha);
        if (escarcha != null)
        {
            trueDamage += escarcha.intensity;
            ShowPopup("¡Roto!", Color.cyan);
            State.instance.RemoveSpecificState(this, State.StateType.Escarcha);
        }

        _currentLife = Mathf.Max(_currentLife - trueDamage, 0);

        string damageText = isCritical ? $"-{trueDamage}!" : $"-{trueDamage}";
        Color textColor = isCritical ? Color.red : Color.white;
        ShowPopup(damageText, textColor);

        // --- REACCIONES DESPUÉS DEL GOLPE ---

        // SUEÑO (Se despierta al recibir la hostia)
        if (activeEffects.Find(s => s.type == State.StateType.Sueno) != null)
        {
            ShowPopup("¡Despierta!", Color.white);
            State.instance.RemoveSpecificState(this, State.StateType.Sueno);
        }

        // Actualizamos la UI si el que recibió el golpe fue un aliado
        if (faction == Faction.Player && TryGetComponent<Character_Controller>(out var controller))
        {
            controller.RefreshUI();
        }

        if (_currentLife <= 0) Die();

        return true;
    }

    protected void ShowPopup(string text, Color color)
    {
        if (damagePopupPrefab != null)
        {
            Collider2D col = GetComponent<Collider2D>();
            Vector3 spawnPos = (col != null)
                ? new Vector3(col.bounds.center.x, col.bounds.max.y + 0.5f, transform.position.z)
                : transform.position + damagePopupOffset;

            GameObject popupObj = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
            DamagePopup popup = popupObj.GetComponent<DamagePopup>();

            if (popup != null) popup.Setup(text, color);
        }
    }

        public virtual void Die()
        {
            Debug.Log($"{name} ha caído inconsciente.");
            isDead = true;
            _currentLife = 0;
            
            // 1. Limpiamos sus estados para que no resucite envenenado o sangrando
            activeEffects.Clear();

            // 2. El Truco del Sprite: Apagamos el motor de animación y forzamos la imagen
            Animator anim = GetComponentInChildren<Animator>();
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

            if (anim != null) anim.enabled = false;

            if (sr != null)
            {
                if (deadSprite != null) sr.sprite = deadSprite;
                sr.color = Color.gray; // Un tono apagado para dar dramatismo
            }

            // Actualizamos UI
            if (TryGetComponent<Character_Controller>(out var controller)) controller.RefreshUI();
        }

    // ==========================================
    // SISTEMA DE RESURRECCIÓN
    // ==========================================
    public virtual void Revive(int healAmount)
    {
        // Si intentan curar a alguien vivo con una carta de revivir, no hace nada
        if (!isDead) return;

        isDead = false;
        _currentLife = healAmount;
        if (_currentLife > _maxLife) _currentLife = _maxLife;

        // 1. Despertamos el motor de animaciones (volverá automáticamente al IdleBase)
        Animator anim = GetComponentInChildren<Animator>();
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();

        if (anim != null) anim.enabled = true;
        if (sr != null) sr.color = Color.white; // Le devolvemos el color vivo

        // 2. Lo volvemos a meter en la cola de turnos
        if (Turn_Controller.instance != null)
        {
            // Asumo que tu Turn_Controller tiene una lista global de entidades
            if (!Turn_Controller.instance.allEntities.Contains(this))
            {
                Turn_Controller.instance.allEntities.Add(this);
            }
        }

        ShowPopup("¡Revivido!", Color.yellow);

        // 3. Refrescamos su UI
        if (TryGetComponent<Character_Controller>(out var controller))
        {
            controller.RefreshUI();
        }
    }

    public void ApplyEffectDamage(int damage, Color effectColor)
    {
        InitializeStatsIfNeeded();
        _currentLife -= damage;
        _currentLife = Mathf.Clamp(_currentLife, 0, _maxLife);

        if (TryGetComponent<VisualEffect_Controller>(out var effects)) effects.FlashPoison();

        ShowPopup("-" + damage, effectColor);
        Debug.Log($"{name} recibe {damage} de daño por efecto (Ignora Defensa).");

        if (_currentLife <= 0) Die();
    }

    protected virtual int DamageCalculator(int damage)
    {
        InitializeStatsIfNeeded();
        return Mathf.Max(1, damage - (int)(damage * _currentDefense));
    }

    public virtual IEnumerator ShowText(string message, TextMeshProUGUI text)
    {
        if (text == null)
        {
            Debug.Log($"{name} dice: {message}");
            yield return new WaitForSeconds(1f);
            yield break;
        }

        text.text = message;
        text.maxVisibleCharacters = 0;

        for (int i = 0; i <= message.Length; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    public void SetHighlight(bool value)
    {
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = value ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    public virtual void ConsumMana(int manaAmount)
    {
        InitializeStatsIfNeeded();
        _currentMana = Mathf.Max(_currentMana - manaAmount, 0);
    }

    // ==========================================
    // SISTEMA DE CURACIÓN Y REGENERACIÓN
    // ==========================================

    public virtual void HealCurrentLife(int amount)
    {

        InitializeStatsIfNeeded();

        _currentLife += amount;
        if (_currentLife > _maxLife) _currentLife = _maxLife; // Tope máximo

        // 1. Feedback Visual: Numerito verde flotando
        ShowPopup("+" + amount, Color.green);

        // 2. Actualizar la UI: Solo si es un aliado, buscamos su controlador y refrescamos la barra
        if (faction == Faction.Player)
        {
            if (TryGetComponent<Character_Controller>(out var controller))
            {
                controller.RefreshUI();
            }
        }
    }

    public virtual void RestoreCurrentMana(int amount)
    {
        InitializeStatsIfNeeded();
        _currentMana += amount;
        if (_currentMana > _maxMana) _currentMana = _maxMana;
    }

    public virtual void ApplyFloorRegeneration()
    {
        if (lifeRegenPerFloor > 0) HealCurrentLife(lifeRegenPerFloor);
        if (manaRegenPerFloor > 0) RestoreCurrentMana(manaRegenPerFloor);
    }

    // ==========================================
    // MODIFICADORES DE TOPES MÁXIMOS Y ESTADÍSTICAS
    // ==========================================

    public virtual void ControlMaxLife(int amount, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _maxLife += amount;
        else _maxLife -= amount;

        if (_currentLife > _maxLife) _currentLife = _maxLife;
    }

    public virtual void ControlMaxMana(int amount, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _maxMana += amount;
        else _maxMana -= amount;

        if (_currentMana > _maxMana) _currentMana = _maxMana;
    }

    public virtual void ControlDefense(float Defense, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _currentDefense += Defense;
        else _currentDefense -= Defense;
    }

    public virtual void ControlSpeed(int Speed, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _currentSpeed += Speed;
        else _currentSpeed -= Speed;
    }

    public virtual void ControlAttack(int Attack, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _currentAttack += Attack;
        else _currentAttack -= Attack;
    }

    public virtual void ControlCritChance(int amount, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _currentCritChance += amount;
        else _currentCritChance -= amount;

        _currentCritChance = Mathf.Clamp(_currentCritChance, 0, 100);
    }

    public virtual void ControlEvasion(int amount, bool trigger)
    {
        InitializeStatsIfNeeded();
        if (trigger) _currentEvasion += amount;
        else _currentEvasion -= amount;

        _currentEvasion = Mathf.Clamp(_currentEvasion, 0, 100);
    }
    public void SetStatsFromLoad(int vidaGuardada, int manaGuardado)
    {
        _currentLife = vidaGuardada;
        _currentMana = manaGuardado;

        // Si el personaje tenía 0 de vida en la base de datos, lo tumbamos directamente
        if (_currentLife <= 0)
        {
            Die();
        }
    }
}