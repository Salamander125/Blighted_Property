using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Character_Controller : MonoBehaviour
{
    [HideInInspector]
    public Character_Ability characterAbility;
    [HideInInspector]
    public Grid_Controller grid;
    [HideInInspector]
    public Entity player;
    [HideInInspector]
    public Combat_Controller combat;
    [HideInInspector]
    public AbilityMenuUI abilityMenuUI;

    [HideInInspector] public bool waitingForTarget = false;

    // Guardamos directamente la clase Ability que hemos descargado
    [HideInInspector] public Ability selectedAbility;

    private bool hasActed = false;
    // Esta variable nos dice si estamos pegando un golpe normal o usando una skill
    private bool isExecutingBasicAttack = false;

    private void Start()
    {
        characterAbility = GetComponent<Character_Ability>();
        grid = FindFirstObjectByType<Grid_Controller>();
        player = GetComponent<Entity>();
        abilityMenuUI = UIManager_Combat.instance.abilityMenuUI;
    }

    // --- MÉTODOS QUE LLAMA EL UIManager_Combat ---

    // Prepara al personaje para elegir a quién darle un espadazo normal
    public void ExecuteAttack()
    {
        if (hasActed) return;

        isExecutingBasicAttack = true; // Marcamos que es ataque básico
        waitingForTarget = true; // Activamos la espera para que el jugador elija en el suelo
        selectedAbility = null; // Limpiamos cualquier skill que estuviera marcada

        // Bloqueamos los botones de la pantalla mientras elegimos objetivo
        UIManager_Combat.instance.UpdateButtonsState(false);
    }

    // Abre la lista de habilidades para que elijamos una
    public void ExecuteAbilityMenu()
    {
        if (hasActed) return;
        if (abilityMenuUI != null)
        {
            isExecutingBasicAttack = false; // Si entramos aquí, no es un ataque normal

            // Pasamos la lista a Array por si tu UI todavía espera recibir un Array[]
            abilityMenuUI.AbrirMenu(this, characterAbility.abilities.ToArray());
        }
    }

    // --- LÓGICA INTERNA ---

    // Resetea las variables al empezar turno para poder volver a atacar
    public void ResetTurn()
    {
        hasActed = false;
        waitingForTarget = false;
        isExecutingBasicAttack = false;
        RefreshUI();
    }

    // Actualiza las barritas de vida y maná que vemos en el HUD
    public void RefreshUI()
    {
        if (combat != null && player != null)
        {
            float lifePercent = (float)player.CurrentLife / player.MaxLife;
            float manaPercent = (float)player.CurrentMana / player.MaxMana;
            combat.UpdateLifeUI(player.CurrentLife, lifePercent);
            combat.UpdateManaUI(player.CurrentMana, manaPercent);
        }
    }

    // Recibe el aviso del PlayerTurnController cuando ya hemos elegido a quién pegar
    public void TargetSelected(Vector2Int targetGridPos, Entity specificTarget)
    {
        if (!waitingForTarget || hasActed) return;
        hasActed = true;
        waitingForTarget = false;
        UIManager_Combat.instance.UpdateButtonsState(false);

        // Si era ataque básico, usamos el enemigo concreto que brillaba en rojo
        if (isExecutingBasicAttack)
        {
            StartCoroutine(ActionAttack(targetGridPos, specificTarget));
        }
        else
        {
            // Si era una habilidad, usamos la lógica de área normal
            StartCoroutine(UseAbilityOnTarget(targetGridPos, specificTarget));
        }
    }

    // La rutina para hacer el daño del ataque normal
    private IEnumerator ActionAttack(Vector2Int targetGridPos, Entity specificTarget)
    {
        // Priorizamos al enemigo que seleccionamos con el nuevo sistema de sub-selección
        Entity target = specificTarget != null ? specificTarget : grid.GetEntityAt(targetGridPos);

        // Si el objetivo es un enemigo y sigue vivo, le damos el golpe
        if (target != null && target.faction == Faction.Enemy && target.CurrentLife > 0)
        {
            player.DoDamage(player.CurrentAttack, target);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            // Si por error elegimos un sitio vacío o un aliado, avisamos
            yield return new WaitForSeconds(0.6f);
        }

        FinishTurn();
    }

    // Se ejecuta cuando elegimos una skill de la lista de la UI
    public void OnAbilitySelectedFromMenu(int index)
    {
        // Recogemos la habilidad dinámica de nuestra lista
        Ability skill = characterAbility.abilities[index];

        // Comprobamos si nos queda maná antes de dejar que apunte al suelo
        if (player.CurrentMana < skill.manaCost)
        {
            abilityMenuUI.AbrirMenu(this, characterAbility.abilities.ToArray());
            return;
        }

        // Si hay maná, cerramos el menú y encendemos el selector del grid
        if (TryGetComponent<PlayerTurnController>(out var nav))
        {
            nav.showingAbilityMenu = false;

            // Guardamos la referencia a la habilidad real que vamos a usar
            selectedAbility = skill;

            waitingForTarget = true;
            isExecutingBasicAttack = false;

            // Le pasamos la skill al selector para que dibuje el área correcta (Cruz, X, Single...)
            nav.StartTargetSelection(skill);
        }
    }

    // Rutina para disparar los efectos y animaciones de la habilidad elegida
    private IEnumerator UseAbilityOnTarget(Vector2Int targetGridPos, Entity target) 
    {
        if (characterAbility != null && selectedAbility != null)
        {
            // --- LE PASAMOS EL 'target' A LA RUTINA FINAL ---
            yield return characterAbility.UseAbilityRoutine(selectedAbility, targetGridPos, grid, target);
        }
        FinishTurn();
    }

    // Limpia las variables y le dice al Turn_Controller que pase al siguiente personaje
    private void FinishTurn()
    {
        isExecutingBasicAttack = false;
        RefreshUI();
        Turn_Controller.instance.EndTurn();
    }

    public bool GetHasActed()
    {
        return hasActed;
    }
}