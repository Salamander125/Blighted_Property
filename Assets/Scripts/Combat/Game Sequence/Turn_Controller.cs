using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Turn_Controller : MonoBehaviour
{
    [Header("UI y Prefabs")]
    [SerializeField] private GameObject turnPopupPrefab;
    [SerializeField] private Sprite playerIconSprite;
    [SerializeField] private Sprite enemyIconSprite;

    [Header("Botones Globales")]
    [SerializeField] private Button globalAttackBtn;
    [SerializeField] private Button globalAbilityBtn;

    [Header("Combate")]
    [SerializeField] public List<Entity> allEntities = new();

    [Header("Economía")]
    public int totalGold = 0;

    // Cola que guarda el orden en que se ejecuta el turno.
    private Queue<Entity> turnQueue = new();

    // Variable que guarda la entidad actual
    public Entity currentEntity;

    public static Turn_Controller instance;

    private bool processingTurn = false;
    private Faction lastFaction;
    private bool gameEnded = false;
    public bool isInitializing = false;

    private void Awake()
    {
        instance = this;
    }

    public void Start()
    {
        MatchRequest Partida = PlayerSelectionData.PartidaCargada;
        Floor_Manager.instance.currentFloor = Partida.floorReached;
        Floor_Manager.instance.actualizarFloor();
        totalGold = Partida.goldCollected;
        UIManager_Combat.instance.UpdateGoldUI(totalGold);
    }

    public void StartCombatSequence()
    {
        gameEnded = false;
        processingTurn = false;

        FindAllEntities();
        DetermineTurnOrder();

        if (allEntities.Count > 0)
        {
            StartCoroutine(ProcessNextTurn());
        }
        else
        {
            Debug.LogWarning("No se puede iniciar combate: Lista de entidades vacía.");
        }
    }

    // =============================
    // GESTIÓN DE TURNOS
    // =============================

    // Funcion para encontrar a todas las entidades en escena
    public void FindAllEntities()
    {
        allEntities.Clear();

        // Buscamos todo lo que sea Entity
        Entity[] foundObjects = FindObjectsByType<Entity>(FindObjectsSortMode.None);

        foreach (Entity entity in foundObjects)
        {
            // Solo añadimos si NO está marcado para destrucción y tiene vida
            if (entity != null && entity.gameObject.activeInHierarchy && entity.CurrentLife > 0)
            {
                allEntities.Add(entity);
            }
        }
        Debug.Log($"<color=cyan>Combate Iniciado: {allEntities.Count} entidades listas.</color>");
    }

    // Funcion para determinar el orden en que se ejecutan los turnos
    public void DetermineTurnOrder()
    {
        // Vaciamos la cola de turnos anterior
        turnQueue.Clear();

        // Si no hay entidades, no podemos ordenar
        if (allEntities.Count == 0) return;

        // Ordenamos la lista de entidades de mayor a menor velocidad
        allEntities.Sort((a, b) => b.CurrentSpeed.CompareTo(a.CurrentSpeed));

        // Ahora que están ordenados, los metemos en la cola de turnos
        foreach (Entity entity in allEntities)
        {
            // Solo metemos a los que no sean nulos y sigan vivos
            if (entity != null && entity.CurrentLife > 0)
            {
                turnQueue.Enqueue(entity);
            }
        }
    }


    // Funcion que indica que hacer cuando se quiere finalizar turno.
    public void EndTurn()
    {
        // Si aun se esta prosesando un turno no hace nada.
        if (!processingTurn) return;

        // Le indicamos que ahora ya no se esta procesando un turno.
        processingTurn = false;

        // Llamamos a una funcion que se encargara de ocultar los botones
        if (UIManager_Combat.instance != null)
        {
            UIManager_Combat.instance.SetActiveCharacter(null);
        }

        // Limpiamos currentEntity para poder almacenar a una entidad diferente en el siguiente turno.
        if (currentEntity != null)
        {
            // Si la entidad es un enemigo le indicamos a dicho enemigo que ya no es su turno.
            if (currentEntity.TryGetComponent(out Enemy_Controller enemy))
                enemy.isTurn = false;
        }

        // Miramos si se han cumplido algunas de las condiciones de victoria
        CheckWinCondition();

        // Si no se ha acabado el juego tras chekear las condiciones de victoria se procesa el siguiente turno.
        if (!gameEnded) StartCoroutine(ProcessNextTurn());
    }

    // Couritina para procesar el siguiente turno tras el turno actual.
    private IEnumerator ProcessNextTurn()
    {
        // 1. Verificación de seguridad y LIMPIEZA DE MEMORIA (Evita el StackOverflow)
        if (gameEnded) yield break;
        yield return null; // <--- LA LÍNEA MÁGICA SALVA-VIDAS

        // Indicamos que estamos procesando un turno
        processingTurn = true;
        currentEntity = null;

        // 2. Buscar siguiente candidato VIVO en la cola (¡Esto lo habías borrado!)
        while (turnQueue.Count > 0 && currentEntity == null)
        {
            Entity candidate = turnQueue.Dequeue();
            if (candidate != null && candidate.CurrentLife > 0)
            {
                currentEntity = candidate;
            }
        }

        CheckWinCondition();

        // 3. Si no hay ningún candidato válido en la cola, ejecutamos la siguiente ronda
        if (currentEntity == null)
        {
            CheckWinCondition(); // Si esto pone gameEnded en true, el yield break de abajo nos sacará

            if (gameEnded) yield break;

            if (allEntities.Any(e => e.faction == Faction.Enemy && e.CurrentLife > 0))
            {
                DetermineTurnOrder();
                lastFaction = Faction.None;
                processingTurn = false;
                StartCoroutine(ProcessNextTurn());
            }
            else
            {
                // Si llegamos aquí y no hay enemigos, es victoria forzada
                CheckWinCondition();
            }
            yield break;
        }

        // --- (AQUÍ ESTABA EL POPUP, YA ELIMINADO PARA SIEMPRE) ---

        // 4. Procesa los estados que tenga la entidad actual (Veneno, etc)

        // CONTROL DE CC: SUEÑO
        if (currentEntity.activeEffects.Any(e => e.type == State.StateType.Sueno))
        {
            currentEntity.ShowPopup("Zzz...", Color.white);

            yield return new WaitForSeconds(0.5f);

            EndTurn();
            yield break;
        }
        if (State.instance != null)
        {
            State.instance.ProcessEffects(currentEntity);
            // Si el efecto hizo daño, esperamos un pelín para que se vean los números flotantes
            if (currentEntity.activeEffects.Count > 0) yield return new WaitForSeconds(0.6f);
        }

        // 5. Verifica si la entidad ha muerto por el daño del veneno/quemadura.
        if (currentEntity == null || currentEntity.CurrentLife <= 0)
        {
            processingTurn = false;
            CheckWinCondition();
            StartCoroutine(ProcessNextTurn());
            yield break;
        }

        // 6. Activar la entidad finalmente
        ActivateCurrentEntity();
    }

    // =============================
    // Muerte y Limpieza
    // =============================

    // Funcion para borrar la entidad de la lista de las entidades en caso de no haber sido borrada ya.
    public void RemoveEntity(Entity entity)
    {
        if (allEntities.Contains(entity))
        {
            if (entity.faction == Faction.Enemy)
            {
                if (entity.TryGetComponent(out Enemy enemyScript))
                {
                    // Llamamos a nuestra nueva función centralizada
                    AddGold(enemyScript.goldValue);
                }
            }
            allEntities.Remove(entity);
        }
        CheckWinCondition();
    }

    // =============================
    // UI Y POPUPS
    // =============================

    // Couritina para mostrar el mismo popup que el de arriba pero de forma mas generica y personalizado.
    private IEnumerator ShowRoundPopup(string message)
    {
        if (turnPopupPrefab == null) yield break;

        GameObject popupPrefab = Instantiate(turnPopupPrefab, Vector3.zero, Quaternion.identity);
        TurnPopup popup = popupPrefab.GetComponent<TurnPopup>();
        if (popup != null)
        {
            popup.Setup(message);
            yield return new WaitForSeconds(popup.TotalDuration);
        }
        Destroy(popupPrefab);
    }

    // Funcion para activar una nueva entidad
    private void ActivateCurrentEntity()
    {
        if (currentEntity == null) return;

        // Si es un player:
        if (currentEntity.TryGetComponent(out Character_Controller player))
        {
            // Le decimos al Manager quién manda ahora
            UIManager_Combat.instance.SetActiveCharacter(player);

            // Le indicamos al player que tiene que reiniciar turno.
            player.ResetTurn();

            // Reseteamos los controles y variables en el plauerTurnController de la entidad.
            if (player.TryGetComponent(out PlayerTurnController playerTurnController))
            {
                playerTurnController.ResetController();
            }
        }
        // Si es un enemigo:
        else if (currentEntity.TryGetComponent(out Enemy_Controller enemy))
        {
            // En turno enemigo, el Manager no tiene a ningun player activo
            UIManager_Combat.instance.SetActiveCharacter(null);
            enemy.isTurn = true;
        }

        if (TurnTimelineUI.instance != null)
        {
            TurnTimelineUI.instance.UpdateTimeline(turnQueue, currentEntity);
        }
    }

    public void AddGold(int amount)
    {
        totalGold += amount;

        // Le decimos al MANAGER único que actualice el oro de la pantalla.
        if (UIManager_Combat.instance != null)
        {
            UIManager_Combat.instance.UpdateGoldUI(totalGold);
        }
    }

    // Funcion para chekear si alguna faccion ha ganado.
    public void CheckWinCondition()
    {
        if (gameEnded || isInitializing) return;

        // Filtramos en tiempo real para no arrastrar "fantasmas" de la lista
        int enemiesAlive = allEntities.Count(e => e != null && e.faction == Faction.Enemy && e.CurrentLife > 0);
        int playersAlive = allEntities.Count(e => e != null && e.faction == Faction.Player && e.CurrentLife > 0);

        if (enemiesAlive <= 0)
        {
            gameEnded = true;
            StopAllCoroutines();
            StartCoroutine(EndBattleSequence("¡VICTORIA!"));
        }
        else if (playersAlive <= 0)
        {
            gameEnded = true;
            StopAllCoroutines();
            StartCoroutine(EndBattleSequence("DERROTA"));
        }
    }

    // Corrutina para finalizar el combate (Recuperada)
    private IEnumerator EndBattleSequence(string message)
    {
        yield return ShowRoundPopup(message);
        yield return new WaitForSeconds(1f);

        if (message == "¡VICTORIA!")
        {
            // Abrimos tienda en lugar de reiniciar para mantener el progreso de Natalia
            Shop_Manager.instance.OpenShop();
        }
        else
        {
            if (SaveManager.instance != null)
            {
                SaveManager.instance.GuardarPartida(true);
            }

            // 2. Esperamos un segundo extra para dar tiempo a que la petición HTTP salga
            // y para que el jugador asimile la derrota.
            yield return new WaitForSeconds(1f);

            // 3. Devolvemos al jugador a la escena del menú principal
            SceneManager.LoadScene("First_scene");
        }
    }

    // Prepara las variables para el siguiente piso
    public void PrepareNextFloor()
    {
        // 1. Sanamos pasivamente a los jugadores
        foreach (Entity ent in allEntities)
        {
            if (ent != null && ent.faction == Faction.Player && ent.CurrentLife > 0)
            {
                ent.ApplyFloorRegeneration();

                if (ent.TryGetComponent(out Character_Controller charController))
                {
                    charController.RefreshUI();
                }
            }
        }

        // 2. Reseteo normal de variables para el siguiente combate
        gameEnded = false;
        processingTurn = false;
        lastFaction = Faction.None;
        currentEntity = null;
        allEntities.Clear();
        turnQueue.Clear();

        // 3. Le pasamos el control al Floor_Manager para que genere el siguiente nivel
        if (Floor_Manager.instance != null)
        {
            Floor_Manager.instance.AdvanceToNextFloor();
        }
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}