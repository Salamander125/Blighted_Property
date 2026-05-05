using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI del Menú Principal de Pausa")]
    public GameObject pausePanel;
    public GameObject[] botonesOpciones; // 0: Continuar, 1: Guardar, 2: Salir

    [Header("UI de Modal de Sobrescritura")]
    public GameObject modalGuardadoPanel;
    public Transform contenedorSlots;
    public GameObject saveSlotPrefab;

    [Header("Base de Datos Visual")]
    public List<LoadMatchManager.ZonaUI> zonasPorPiso;
    public List<LoadMatchManager.RetratoPersonaje> retratosDisponibles;

    [Header("UI de Guardado (Cinemática)")]
    public Image pantallaNegra;
    public TextMeshProUGUI textoGuardando;

    private bool isPaused = false;
    private bool isSaving = false;
    private bool isInSaveModal = false;

    // Solo mantenemos la navegación para el menú principal (Continuar, Guardar, Salir)
    private int navMenuIndex = 0;
    private bool salirDespuesDeGuardar = false;

    private string apiUrl = "https://blightedproperty.somee.com/api/matches/active/";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete) && !isSaving)
        {
            if (isInSaveModal) CerrarModalGuardado();
            else if (isPaused) ResumeGame();
            else PauseGame();
        }

        // Si no está pausado, está guardando, o está en el modal de las tarjetas, el teclado se desactiva
        if (!isPaused || isSaving || isInSaveModal) return;

        // --- NAVEGACIÓN SOLO PARA EL MENÚ PRINCIPAL ---
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            navMenuIndex--;
            if (navMenuIndex < 0) navMenuIndex = botonesOpciones.Length - 1;
            ActualizarVisualesBotones();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            navMenuIndex++;
            if (navMenuIndex >= botonesOpciones.Length) navMenuIndex = 0;
            ActualizarVisualesBotones();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            EjecutarAccionMenu(navMenuIndex);
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        navMenuIndex = 0;
        ActualizarVisualesBotones();
    }

    private void ResumeGame()
    {
        StartCoroutine(RutinaResume());
    }

    private IEnumerator RutinaResume()
    {
        isPaused = false;
        isInSaveModal = false; // <-- LA CURA DEL BUG

        pausePanel.SetActive(false);
        modalGuardadoPanel.SetActive(false);
        yield return null;
        Speed_Manager.instance.ActualizarVelocidad();
    }

    private void ActualizarVisualesBotones()
    {
        for (int i = 0; i < botonesOpciones.Length; i++)
        {
            bool isFocused = (i == navMenuIndex);
            var img = botonesOpciones[i].GetComponent<Image>();
            if (img != null)
                img.color = isFocused ? Color.yellow : Color.white;
        }
    }

    private void EjecutarAccionMenu(int indice)
    {
        switch (indice)
        {
            case 0: ResumeGame(); break;
            case 1: BotonUI_GuardarPartida(); break;
            case 2: BotonUI_GuardarYSalir(); break;
        }
    }

    // ==========================================
    // LÓGICA DE DECISIÓN (Automático vs Modal)
    // ==========================================
    private void GestionarFlujoGuardado()
    {
        if (SaveManager.instance.currentMatchId != 0)
        {
            StartCoroutine(SecuenciaGuardado(salirDespuesDeGuardar));
        }
        else
        {
            StartCoroutine(AnalizarEstadoDePartidas());
        }
    }

    private IEnumerator AnalizarEstadoDePartidas()
    {
        isSaving = true;
        int userId = PlayerPrefs.GetInt("UserId", 1);
        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl + userId))
        {
            yield return www.SendWebRequest();

            isSaving = false;

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonString = "{\"matches\":" + www.downloadHandler.text + "}";
                MatchListWrapper wrapper = JsonUtility.FromJson<MatchListWrapper>(jsonString);
                int numPartidas = (wrapper != null && wrapper.matches != null) ? wrapper.matches.Length : 0;

                if (numPartidas < 3)
                {
                    SaveManager.instance.currentMatchId = 0;
                    StartCoroutine(SecuenciaGuardado(salirDespuesDeGuardar));
                }
                else
                {
                    AbrirModalSobrescritura(wrapper.matches);
                }
            }
        }
    }

    private void AbrirModalSobrescritura(MatchRequest[] partidasGuardadas)
    {
        isInSaveModal = true;
        pausePanel.SetActive(false);
        modalGuardadoPanel.SetActive(true);

        // Limpiamos la basura vieja
        foreach (Transform child in contenedorSlots)
        {
            // Solo por si acaso el prefab está en la jerarquía, no lo destruimos
            if (child.gameObject != saveSlotPrefab) Destroy(child.gameObject);
        }

        // Instanciamos las tarjetas limpiamente
        for (int i = 0; i < partidasGuardadas.Length; i++)
        {
            MatchRequest partida = partidasGuardadas[i];
            LoadMatchManager.ZonaUI zonaEncontrada = ResolverZona(partida.floorReached);

            List<Sprite> spritesCaras = new List<Sprite>();
            foreach (var character in partida.characters)
            {
                Sprite cara = BuscarRetrato(character.characterName);
                if (cara != null) spritesCaras.Add(cara);
            }

            GameObject nuevaTarjeta = Instantiate(saveSlotPrefab, contenedorSlots);
            nuevaTarjeta.SetActive(true); // Nos aseguramos de que esté visible

            SaveSlotUI slotUI = nuevaTarjeta.GetComponent<SaveSlotUI>();
            if (slotUI != null)
            {
                slotUI.modoGuardado = true;
                slotUI.ConfigurarTarjeta(
                    partida, zonaEncontrada.nombreZona, zonaEncontrada.imagenFondo,
                    zonaEncontrada.alineacionImagen, spritesCaras.ToArray()
                );
            }
        }
    }

    // Esta función es a la que llama tu SaveSlotUI.cs al hacer CLIC con el ratón
    public void SeleccionarSlotParaSobrescribir(int matchIdElegido)
    {
        SaveManager.instance.currentMatchId = matchIdElegido;
        modalGuardadoPanel.SetActive(false);
        StartCoroutine(SecuenciaGuardado(salirDespuesDeGuardar));
    }

    private void CerrarModalGuardado()
    {
        isInSaveModal = false;
        modalGuardadoPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // ==========================================
    // CINEMÁTICA Y HELPERS
    // ==========================================
    private IEnumerator SecuenciaGuardado(bool salirAlMenu)
    {
        isSaving = true;

        pantallaNegra.gameObject.SetActive(true);
        textoGuardando.gameObject.SetActive(true);
        pausePanel.gameObject.SetActive(false);
        modalGuardadoPanel.gameObject.SetActive(false); // Por si venimos del modal

        Color c = pantallaNegra.color;
        c.a = 1f;
        pantallaNegra.color = c;

        Coroutine animacionTexto = StartCoroutine(AnimarPuntosTexto());

        yield return StartCoroutine(SaveManager.instance.RutinaGuardar(false));

        if (animacionTexto != null) StopCoroutine(animacionTexto);

        textoGuardando.text = "¡Guardado!";

        yield return new WaitForSecondsRealtime(1f);

        if (salirAlMenu)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("First_scene");
        }
        else
        {
            pantallaNegra.gameObject.SetActive(false);
            textoGuardando.gameObject.SetActive(false);
            isSaving = false;
            ResumeGame();
        }
    }

    private IEnumerator AnimarPuntosTexto()
    {
        int puntos = 0;
        while (true)
        {
            textoGuardando.text = "Guardando" + new string('.', puntos);
            puntos = (puntos + 1) % 4;
            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private LoadMatchManager.ZonaUI ResolverZona(int piso)
    {
        if (zonasPorPiso == null || zonasPorPiso.Count == 0) return new LoadMatchManager.ZonaUI();
        LoadMatchManager.ZonaUI zonaElegida = zonasPorPiso[0];
        foreach (var zona in zonasPorPiso) if (piso >= zona.pisoMinimo) zonaElegida = zona;
        return zonaElegida;
    }

    private Sprite BuscarRetrato(string nombre)
    {
        foreach (var retrato in retratosDisponibles) if (nombre.Contains(retrato.nombreExacto)) return retrato.cara;
        return null;
    }

    public void BotonUI_ReanudarJuego() => ResumeGame();
    public void BotonUI_GuardarPartida() { salirDespuesDeGuardar = false; GestionarFlujoGuardado(); }
    public void BotonUI_GuardarYSalir() { salirDespuesDeGuardar = true; GestionarFlujoGuardado(); }

    [System.Serializable]
    private class MatchListWrapper { public MatchRequest[] matches; }
}