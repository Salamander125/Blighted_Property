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
    [SerializeField] private float tiempoAnimacion = 0.15f;

    private List<GameObject> botonesCreados = new List<GameObject>();
    private int indiceActual = 0;
    private Character_Controller controllerReferencia;

    private bool menuActivo = false;
    private bool isAnimating = false;

    private Dictionary<Transform, Coroutine> animacionesActivas = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Vector3> escalaOriginalMap = new Dictionary<Transform, Vector3>();

    private Ability[] habilidadesActuales;

    public void Start()
    {
        instance = this;
    }

    public void AbrirMenu(Character_Controller pj, Ability[] habilidades)
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        RectTransform menuRect = panel.GetComponent<RectTransform>();

        Vector2 screenPos = Camera.main.WorldToScreenPoint(pj.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, parentCanvas.worldCamera, out Vector2 localPoint);

        float dynamicX = canvasRect.sizeDelta.x * horizontalOffsetPercent;
        float dynamicY = canvasRect.sizeDelta.y * verticalOffsetPercent;
        menuRect.anchoredPosition = localPoint + new Vector2(dynamicX, dynamicY);

        controllerReferencia = pj;
        habilidadesActuales = habilidades;
        indiceActual = 0;
        menuActivo = true;

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

        TogglePanelWithAnimation(panel.transform, true);

        // Retrasamos un frame la visualización para asegurar que Unity instanció todo
        StartCoroutine(EsperarYActualizarVisualizacion());
    }

    private IEnumerator EsperarYActualizarVisualizacion()
    {
        yield return new WaitForEndOfFrame();
        ActualizarVisualizacion();
    }

    private void Update()
    {
        if (!menuActivo || isAnimating) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) CambiarSeleccion(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) CambiarSeleccion(1);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmarHabilidad();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.B))
        {
            RegresarABotonesPrincipales();
        }
    }

    private void TogglePanelWithAnimation(Transform target, bool abrir)
    {
        if (target == null) return;

        if (animacionesActivas.ContainsKey(target) && animacionesActivas[target] != null)
        {
            StopCoroutine(animacionesActivas[target]);
        }
        if (!escalaOriginalMap.ContainsKey(target))
        {
            escalaOriginalMap[target] = target.localScale;
        }

        target.gameObject.SetActive(true);

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

    private void ConfirmarHabilidad()
    {
        menuActivo = false;
        TogglePanelWithAnimation(panel.transform, false);

        if (previewPanel != null)
        {
            previewPanel.Ocultar();
        }

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

        // ¡AQUÍ ES DONDE LE DECIMOS A LA PERSIANA QUÉ HABILIDAD ES!
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

        if (previewPanel == null)
        {
            Debug.LogError("¡Falta asignar el Preview Panel en el inspector de AbilityMenuUI!");
            return;
        }

        if (habilidadesActuales != null && habilidadesActuales.Length > 0)
        {
            Ability habilidadSeleccionada = habilidadesActuales[indiceActual];
            int danoBasico = controllerReferencia.player.CurrentAttack;

            // LLAMADA A LA PERSIANA CON LA HABILIDAD SELECCIONADA
            previewPanel.MostrarInfo(habilidadSeleccionada, danoBasico);
        }
    }

    public void CerrarMenu()
    {
        menuActivo = false;
        TogglePanelWithAnimation(panel.transform, false);

        if (previewPanel != null)
        {
            previewPanel.Ocultar();
        }
    }

    private void LimpiarBotones()
    {
        foreach (var b in botonesCreados) Destroy(b);
        botonesCreados.Clear();
    }
}