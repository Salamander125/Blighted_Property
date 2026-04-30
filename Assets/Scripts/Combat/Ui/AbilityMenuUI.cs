using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AbilityMenuUI : MonoBehaviour
{
    public static AbilityMenuUI instance;

    [Header("Configuración")]
    public GameObject panel;
    public Transform contenedor;
    public GameObject botonPrefab;

    [Header("Posicionamiento Relativo")]
    [SerializeField] private float horizontalOffsetPercent = 0.10f;
    [SerializeField] private float verticalOffsetPercent = 0.05f;

    [Header("Tamaño de botones")]
    [SerializeField] private Vector2 tamañoBoton = new Vector2(200, 60);
    [SerializeField] private Vector2 tamañoSeleccionado = new Vector2(220, 70);

    [Header("Previsualización")]
    public AbilityPreviewPanel previewPanel;

    [Header("Animación")]
    [SerializeField] private float tiempoAnimacion = 0.15f; // Lo rápido que hace el Pop

    private List<GameObject> botonesCreados = new List<GameObject>();
    private int indiceActual = 0;
    private Character_Controller controllerReferencia;

    private bool menuActivo = false;
    private bool isAnimating = false; // Bloquea el input mientras se anima

    // Usamos diccionarios para trackear las animaciones de cada panel por separado
    private Dictionary<Transform, Coroutine> animacionesActivas = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Vector3> escalaOriginalMap = new Dictionary<Transform, Vector3>();

    private Ability[] habilidadesActuales;

    public void Start()
    {
        instance = this;
    }

    // =============================
    // FLUJO DEL MENÚ
    // =============================
    public void AbrirMenu(Character_Controller pj, Ability[] habilidades)
    {
        // 1. Posicionamiento en el Canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform menuRect = panel.GetComponent<RectTransform>();

        Vector2 screenPos = Camera.main.WorldToScreenPoint(pj.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, parentCanvas.worldCamera, out Vector2 localPoint);

        float dynamicX = canvasRect.sizeDelta.x * horizontalOffsetPercent;
        float dynamicY = canvasRect.sizeDelta.y * verticalOffsetPercent;
        menuRect.anchoredPosition = localPoint + new Vector2(dynamicX, dynamicY);

        // 2. Inicialización de datos
        controllerReferencia = pj;
        habilidadesActuales = habilidades;
        indiceActual = 0;
        menuActivo = true;

        // 3. Generación de botones
        LimpiarBotones();

        for (int i = 0; i < habilidades.Length; i++)
        {
            GameObject nuevoBoton = Instantiate(botonPrefab, contenedor);
            botonesCreados.Add(nuevoBoton);

            RectTransform rt = nuevoBoton.GetComponent<RectTransform>();
            rt.sizeDelta = tamañoBoton;

            TextMeshProUGUI textoBoton = nuevoBoton.GetComponentInChildren<TextMeshProUGUI>();
            if (textoBoton != null) textoBoton.text = habilidades[i].nombre;
        }

        ActualizarVisualizacion();

        // 4. Lanzamos las animaciones de APERTURA para AMBOS paneles
        TogglePanelWithAnimation(panel.transform, true);

        if (previewPanel != null)
        {
            // Nota: MostrarInfo ya hace SetActive(true), pero necesitamos forzar la animación
            TogglePanelWithAnimation(previewPanel.transform, true);
        }
    }

    private void Update()
    {
        // Bloqueamos el input si el menú no está activo o se está animando
        if (!menuActivo || isAnimating) return;

        // Navegación
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) CambiarSeleccion(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) CambiarSeleccion(1);

        // Selección
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmarHabilidad();
        }

        // Cancelar
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
        {
            RegresarABotonesPrincipales();
        }
    }

    // =============================
    // SISTEMA DE ANIMACIÓN GENERAL
    // =============================
    private void TogglePanelWithAnimation(Transform target, bool abrir)
    {
        if (target == null) return;

        // Si ya se está animando este panel, paramos la animación anterior
        if (animacionesActivas.ContainsKey(target) && animacionesActivas[target] != null)
        {
            StopCoroutine(animacionesActivas[target]);
        }
        if (!escalaOriginalMap.ContainsKey(target))
        {
            escalaOriginalMap[target] = target.localScale;
        }

        // Aseguramos que el objeto esté activo para ver la animación
        target.gameObject.SetActive(true);

        // Lanzamos la nueva animación y la guardamos en el diccionario
        Coroutine nuevaAnimacion = StartCoroutine(AnimarPanelGenerico(target, abrir));
        animacionesActivas[target] = nuevaAnimacion;
    }

    private IEnumerator AnimarPanelGenerico(Transform target, bool abrir)
    {
        if (target == panel.transform) isAnimating = true;

        float elapsed = 0f;

        Vector3 escalaOriginal = escalaOriginalMap[target];
        Vector3 inicio = abrir ? Vector3.zero : escalaOriginal;
        Vector3 fin = abrir ? escalaOriginal : Vector3.zero;

        target.localScale = inicio;

        while (elapsed < tiempoAnimacion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / tiempoAnimacion;
            float curve = t * t * (3f - 2f * t);

            target.localScale = Vector3.Lerp(inicio, fin, curve);
            yield return null;
        }

        target.localScale = fin;

        if (target == panel.transform) isAnimating = false;

        if (!abrir)
        {
            target.gameObject.SetActive(false);

            if (target == panel.transform) LimpiarBotones();
        }

        animacionesActivas.Remove(target);
    }

    // =============================
    // LÓGICA INTERNA
    // =============================
    private void ConfirmarHabilidad()
    {
        menuActivo = false;
        // Lanzamos las animaciones de CIERRE para AMBOS paneles
        TogglePanelWithAnimation(panel.transform, false);

        if (previewPanel != null)
        {
            TogglePanelWithAnimation(previewPanel.transform, false);
            previewPanel.Ocultar();
        }

        // Avisamos al personaje
        controllerReferencia.OnAbilitySelectedFromMenu(indiceActual);
    }

    private void RegresarABotonesPrincipales()
    {
        CerrarMenu();
        if (controllerReferencia.TryGetComponent<PlayerTurnController>(out var nav))
        {
            nav.showingAbilityMenu = false;
        }
    }

    private void CambiarSeleccion(int direccion)
    {
        if (botonesCreados.Count == 0) return;
        indiceActual = (indiceActual + direccion + botonesCreados.Count) % botonesCreados.Count;
        ActualizarVisualizacion();
    }

    private void ActualizarVisualizacion()
    {
        for (int i = 0; i < botonesCreados.Count; i++)
        {
            RectTransform rt = botonesCreados[i].GetComponent<RectTransform>();
            rt.sizeDelta = (i == indiceActual) ? tamañoSeleccionado : tamañoBoton;

            TextMeshProUGUI txt = botonesCreados[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.color = (i == indiceActual) ? Color.red : Color.black;
        }

        if (previewPanel != null && habilidadesActuales != null && habilidadesActuales.Length > 0)
        {
            Ability habilidadSeleccionada = habilidadesActuales[indiceActual];
            int danoBasico = controllerReferencia.player.CurrentAttack;
            previewPanel.MostrarInfo(habilidadSeleccionada, danoBasico);
        }
    }

    public void CerrarMenu()
    {
        menuActivo = false;

        TogglePanelWithAnimation(panel.transform, false);

        if (previewPanel != null)
        {
            TogglePanelWithAnimation(previewPanel.transform, false);
            previewPanel.Ocultar();
        }
    }

    private void LimpiarBotones()
    {
        foreach (var b in botonesCreados) Destroy(b);
        botonesCreados.Clear();
    }
}