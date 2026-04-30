using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la lógica de turno del jugador, leyendo el teclado para atacar, 
/// abrir habilidades, cancelar acciones o inspeccionar entidades en el tablero (aliados y enemigos).
/// </summary>
public class PlayerTurnController : MonoBehaviour
{
    [Header("Jugador")]
    private Character_Controller characterController;

    [Header("Botones de acciones")]
    private Button[] actionButtons;

    [Header("Ajustes Visuales")]
    [SerializeField] private float selectedScale = 0.26f;
    [SerializeField] private float normalScale = 0.23f;

    [Header("Grid Selector")]
    private GridSelector gridSelector;
    private Grid_Controller grid;

    [Header("Panel de Acciones")]
    private GameObject panelAccionesUI;

    [Tooltip("Distancia horizontal en píxeles para que el panel no tape al personaje")]
    [SerializeField] private float panelOffsetX = 150f;
    [Tooltip("Ajuste vertical en píxeles")]
    [SerializeField] private float panelOffsetY = 0f;

    [Header("Panel de Previsualización")]
    private AbilityPreviewPanel previewPanel;

    [Header("Animación Panel Principal")]
    [SerializeField] private float tiempoAnimacion = 0.15f;
    private Coroutine animacionPanelActivo;
    private bool isPanelClosing = false; // Evita que la animación de cierre se repita en bucle

    [Header("Variables de habilidades")]
    [HideInInspector] public bool selectingAllyTarget = false;
    private Ability abilityActiva;

    // --- ESTADOS DEL CONTROLADOR ---
    [HideInInspector] public bool showingAbilityMenu = false;
    [HideInInspector] public bool selectingTarget = false;

    // Estados de Inspección
    [HideInInspector] public bool inspecting = false;     // Modo inspección (Tab)
    [HideInInspector] public bool inspectingAlly = false; // Mirando la lista de aliados (Shift)

    // Variables para navegar entre aliados fuera del grid
    private int currentAllyIndex = 0;
    private List<Entity> aliveAllies = new List<Entity>();

    private int buttonIndex = 0;

    private void Start()
    {
        characterController = GetComponent<Character_Controller>();
        grid = FindFirstObjectByType<Grid_Controller>();
        gridSelector = FindFirstObjectByType<GridSelector>();
        panelAccionesUI = UIManager_Combat.instance.panelbuttons;
        previewPanel = UIManager_Combat.instance.abilityPreviewPanel;
        AssignGlobalButtons();
        ResetController();
        if (previewPanel != null) previewPanel.Ocultar();
    }

    private void AssignGlobalButtons()
    {
        if (UIManager_Combat.instance != null)
        {
            actionButtons = new Button[2];
            actionButtons[0] = UIManager_Combat.instance.attackBtn;
            actionButtons[1] = UIManager_Combat.instance.abilityBtn;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        // Si es el turno del enemigo, ocultamos suavemente el panel para limpiar la pantalla.
        if (Turn_Controller.instance.currentEntity != null && Turn_Controller.instance.currentEntity.faction == Faction.Enemy)
        {
            OcultarPanelAcciones();
            return;
        }

        // Si es el turno de un jugador aliado PERO NO SOY YO, ignoro la lógica.
        if (Turn_Controller.instance.currentEntity != characterController.player)
        {
            return;
        }

        // Si soy yo, pero la tienda está abierta o ya he actuado, apago el panel y espero.
        if (Shop_Manager.instance != null && Shop_Manager.instance.isShopOpen)
        {
            OcultarPanelAcciones();
            return;
        }

        if (characterController.GetHasActed())
        {
            OcultarPanelAcciones();
            return;
        }

        // Atajo de teclado para entrar/salir del modo inspección táctica
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInspection();
        }

        // --- MÁQUINA DE ESTADOS DE NAVEGACIÓN ---

        // ESTADO A: Inspeccionando el campo de batalla
        if (inspecting)
        {
            HandleInspectionNavigation();
            return; // Bloqueamos el resto del Update
        }

        // ESTADO B: Eligiendo a quién atacar
        if (selectingTarget)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
            {
                CancelSelection();
                return;
            }

            if (gridSelector != null)
                gridSelector.HandleSelectorInput(characterController);
        }
        else if (selectingAllyTarget)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
            {
                CancelSelection();
                return;
            }
            HandleAllyTargetingNavigation(); // Llamamos a la nueva función de movimiento
        }
        // ESTADO C: Navegando por el menú de habilidades
        else if (showingAbilityMenu)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
            {
                showingAbilityMenu = false;
                HighlightButton(buttonIndex);
            }
        }
        // ESTADO D: Menú principal (Atacar / Habilidades)
        else
        {
            MostrarPanelAcciones(); // Activamos con animación en vez de golpe
            HandleButtonNavigation();
        }
    }

    // ==========================================
    // LÓGICA DE MENÚS Y COMBATE
    // ==========================================

    private void ColocarPanelJuntoAlPersonaje()
    {
        if (panelAccionesUI == null || characterController.player == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(characterController.player.transform.position);
        screenPos.x += panelOffsetX;
        screenPos.y += panelOffsetY;

        panelAccionesUI.transform.position = screenPos;
    }

    private void HandleButtonNavigation()
    {
        if (actionButtons == null || actionButtons.Length == 0 || actionButtons[0] == null)
        {
            AssignGlobalButtons();
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) MoveInButtons(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) MoveInButtons(1);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ButtonPressed();
    }

    private void MoveInButtons(int delta)
    {
        buttonIndex += delta;
        if (buttonIndex < 0) buttonIndex = actionButtons.Length - 1;
        if (buttonIndex >= actionButtons.Length) buttonIndex = 0;
        HighlightButton(buttonIndex);
    }

    private void HighlightButton(int index)
    {
        if (actionButtons == null || actionButtons.Length == 0) return;
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] == null) continue;
            float scale = (i == index) ? selectedScale : normalScale;
            actionButtons[i].transform.localScale = Vector3.one * scale;
        }
    }

    private void ButtonPressed()
    {
        // Limpiamos la pantalla apagando el panel suavemente
        OcultarPanelAcciones();

        if (buttonIndex == 0)
        {
            characterController.ExecuteAttack();
            StartTargetSelection();
        }
        else if (buttonIndex == 1)
        {
            showingAbilityMenu = true;
            characterController.ExecuteAbilityMenu();
        }
    }

    public void StartTargetSelection(Ability skill = null)
    {
        StartCoroutine(InitSelectionRoutine(skill));
    }

    private IEnumerator InitSelectionRoutine(Ability skill)
    {
        abilityActiva = skill; // Guardamos la habilidad

        if (skill != null && skill.targetsAllies)
        {
            // --- RUTA ALIADA ---
            selectingAllyTarget = true;
            grid.ToggleGridVisuals(false); // Apagamos el suelo
            if (gridSelector != null) gridSelector.ShowSelector(false); // Apagamos el cursor

            // Llenamos la lista de aliados vivos 
            aliveAllies.Clear();
            foreach (var ent in Turn_Controller.instance.allEntities)
            {
                if (ent != null && ent.faction == Faction.Player && ent.CurrentLife > 0)
                {
                    aliveAllies.Add(ent);
                }
            }

            // Empezamos enfocando al primer aliado
            if (aliveAllies.Count > 0)
            {
                currentAllyIndex = 0;
                SeleccionarAliado(currentAllyIndex);
            }
        }
        else
        {
            // --- RUTA ENEMIGA
            selectingTarget = true;
            grid.ToggleGridVisuals(true);
            if (gridSelector != null)
            {
                yield return new WaitForEndOfFrame();
                gridSelector.ShowSelector(true, skill);
            }
        }
    }

    public void TargetSelected(Vector2Int currentpos, Entity specificTarget)
    {
        selectingTarget = false;
        showingAbilityMenu = false;
        gridSelector.ShowSelector(false);

        grid.ToggleGridVisuals(false);
        if (previewPanel != null) previewPanel.Ocultar();

        characterController.TargetSelected(currentpos, specificTarget);
        HighlightButton(buttonIndex);
    }

    public void CancelSelection()
    {
        selectingTarget = false;

        if (gridSelector != null) gridSelector.ShowSelector(false);

        FindAnyObjectByType<Grid_Controller>().ClearAllHighlights();
        grid.ToggleGridVisuals(false);

        if (previewPanel != null) previewPanel.Ocultar();

        foreach (var ent in Turn_Controller.instance.allEntities)
        {
            if (ent != null) ent.SetHighlight(false);
        }

        characterController.waitingForTarget = false;

        if (showingAbilityMenu)
        {
            characterController.ExecuteAbilityMenu();
        }
        else
        {
            UIManager_Combat.instance.UpdateButtonsState(true);
            HighlightButton(buttonIndex);

            // Reaparecemos el panel principal con animación
            MostrarPanelAcciones();
        }
        ExitTargetingMode();
    }

    public void ResetController()
    {
        selectingTarget = false;
        showingAbilityMenu = false;
        buttonIndex = 0;
        if (actionButtons == null || actionButtons.Length == 0) AssignGlobalButtons();
        HighlightButton(buttonIndex);
        if (gridSelector != null) gridSelector.ShowSelector(false);
    }

    // ==========================================
    // LÓGICA DE INSPECCIÓN (TAB Y SHIFT)
    // ==========================================

    private void ToggleInspection()
    {
        if (selectingTarget || showingAbilityMenu)
        {
            Debug.Log("No puedes inspeccionar mientras realizas una acción.");
            return;
        }

        if (selectingTarget || showingAbilityMenu) CancelSelection();

        inspecting = !inspecting;
        inspectingAlly = false;
        grid.ToggleGridVisuals(inspecting);

        gridSelector.ShowSelector(inspecting);

        if (!inspecting)
        {
            InspectorManager.instance.CerrarInspector();
            InspectorManager.instance.OcultarTextoSuelo();
            gridSelector.ResetAllEntitiesHighlights();
            grid.ClearAllHighlights();
            LimpiarBrilloAliados();
            aliveAllies.Clear();
        }
    }

    private void HandleInspectionNavigation()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
        {
            ToggleInspection();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            inspectingAlly = !inspectingAlly;

            if (inspectingAlly)
            {
                gridSelector.ShowSelector(false);
                gridSelector.ResetAllEntitiesHighlights();

                InspectorManager.instance.ActualizarInfoCasilla(null);

                aliveAllies.Clear();
                foreach (var ent in Turn_Controller.instance.allEntities)
                {
                    if (ent != null && ent.faction == Faction.Player && ent.CurrentLife > 0)
                    {
                        aliveAllies.Add(ent);
                    }
                }

                if (aliveAllies.Count > 0)
                {
                    currentAllyIndex = 0;
                    SeleccionarAliado(currentAllyIndex);
                }
            }
            else
            {
                LimpiarBrilloAliados();
                gridSelector.ShowSelector(true);
            }
        }

        if (inspectingAlly)
        {
            if (aliveAllies.Count > 0)
            {
                bool changed = false;
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    currentAllyIndex--;
                    if (currentAllyIndex < 0) currentAllyIndex = aliveAllies.Count - 1;
                    changed = true;
                }
                else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    currentAllyIndex++;
                    if (currentAllyIndex >= aliveAllies.Count) currentAllyIndex = 0;
                    changed = true;
                }

                if (changed) SeleccionarAliado(currentAllyIndex);
            }
            return;
        }

        gridSelector.HandleSelectorInput(characterController);

        Vector2Int posActual = gridSelector.GetCurrentPos();
        GridCell casillaActual = grid.matrix[posActual.x, posActual.y];
        InspectorManager.instance.ActualizarInfoCasilla(casillaActual);

        Entity target = gridSelector.GetSelectedEntity();

        if (target != null)
        {
            InspectorManager.instance.AbrirInspector(target);
        }
        else
        {
            InspectorManager.instance.CerrarInspector();
        }
    }

    private void SeleccionarAliado(int index)
    {
        LimpiarBrilloAliados();

        Entity targetAlly = aliveAllies[index];
        if (targetAlly != null)
        {
            targetAlly.SetHighlight(true);
            InspectorManager.instance.AbrirInspector(targetAlly);
        }
    }

    private void LimpiarBrilloAliados()
    {
        foreach (var ally in aliveAllies)
        {
            if (ally != null) ally.SetHighlight(false);
        }
    }

    // ==========================================
    // ANIMACIÓN DEL PANEL PRINCIPAL
    // ==========================================

    private void MostrarPanelAcciones()
    {
        if (panelAccionesUI == null) return;

        ColocarPanelJuntoAlPersonaje(); // Lo mantenemos pegado al jugador

        // Solo animamos si estaba apagado o si se estaba cerrando a medias
        if (!panelAccionesUI.activeSelf || isPanelClosing)
        {
            isPanelClosing = false;
            panelAccionesUI.SetActive(true);
            if (animacionPanelActivo != null) StopCoroutine(animacionPanelActivo);
            animacionPanelActivo = StartCoroutine(AnimarPanelUI(true));
        }
    }

    private void OcultarPanelAcciones()
    {
        if (panelAccionesUI == null || !panelAccionesUI.activeSelf || isPanelClosing) return;

        isPanelClosing = true;
        if (animacionPanelActivo != null) StopCoroutine(animacionPanelActivo);
        animacionPanelActivo = StartCoroutine(AnimarPanelUI(false));
    }

    private IEnumerator AnimarPanelUI(bool abrir)
    {
        float elapsed = 0f;
        Vector3 inicio = abrir ? Vector3.zero : Vector3.one;
        Vector3 fin = abrir ? Vector3.one : Vector3.zero;

        panelAccionesUI.transform.localScale = inicio;

        while (elapsed < tiempoAnimacion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / tiempoAnimacion;
            float curve = t * t * (3f - 2f * t); // Efecto "Pop"

            panelAccionesUI.transform.localScale = Vector3.Lerp(inicio, fin, curve);
            yield return null;
        }

        panelAccionesUI.transform.localScale = fin;

        // Desactivamos por completo el objeto al acabar de cerrarlo
        if (!abrir)
        {
            panelAccionesUI.SetActive(false);
            isPanelClosing = false;
        }
    }

    private void HandleAllyTargetingNavigation()
    {
        if (aliveAllies.Count == 0) return;

        bool changed = false;

        // Moverse por la lista
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentAllyIndex--;
            if (currentAllyIndex < 0) currentAllyIndex = aliveAllies.Count - 1;
            changed = true;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentAllyIndex++;
            if (currentAllyIndex >= aliveAllies.Count) currentAllyIndex = 0;
            changed = true;
        }

        // Si cambiamos, actualizamos el brillo y el inspector
        if (changed) SeleccionarAliado(currentAllyIndex);

        // Confirmar objetivo
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            Entity objetivoAliado = aliveAllies[currentAllyIndex];

            ExitTargetingMode();
            TargetSelected(Vector2Int.zero, objetivoAliado);

            // Y le pasamos el objetivoAliado
            TargetSelected(Vector2Int.zero, objetivoAliado);
        }
    }

    private void ExitTargetingMode()
    {
        selectingTarget = false;
        selectingAllyTarget = false;

        if (gridSelector != null)
            gridSelector.ShowSelector(false);

        grid.ToggleGridVisuals(false);

        if (previewPanel != null)
            previewPanel.Ocultar();

        InspectorManager.instance.CerrarInspector();

        LimpiarBrilloAliados();
    }
}