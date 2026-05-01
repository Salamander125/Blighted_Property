using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BiomeConfig
{
    public string biomeName;
    public Color textColor = Color.white;
    public int startFloor;
    public int endFloor;
    public GameObject biomeBackgroundPrefab;
    public Enemy_pool[] enemiesForThisBiome;
}

public class Floor_Manager : MonoBehaviour
{
    public static Floor_Manager instance;

    [Header("Estado Actual")]
    public TextMeshProUGUI textFloor;
    public int currentFloor = 1;

    [Header("Presentación del Juego")]
    public string tituloDelJuego = "Blighted Property";
    public Color colorTitulo = Color.white;

    [Header("Configuración de Biomas")]
    public BiomeConfig[] biomes;

    [Header("Referencias")]
    public Enemy_Spawn enemySpawner;
    public Grid_Controller gridController;

    [Header("Escalado de Dificultad")]
    public int extraHealthPerFloor = 5;
    public int extraAttackPerFloor = 1;

    [Header("Efecto de Transición")]
    public Image pantallaNegra;
    public float velocidadOscurecer = 3f;
    public float velocidadAclarar = 2f;

    [Header("Animación de Zona")]
    public TextMeshProUGUI textoNombreZona;
    public float velocidadEscritura = 0.08f;
    public float tiempoEsperaTexto = 1.5f;

    private BiomeConfig activeBiome;
    private GameObject currentBackgroundInstance;

    private Coroutine introCoroutine;
    private bool introTerminada = false;
    private float tiempoInicio;
    public float tiempoMinimoSinSkip = 2f;
    private bool pisoPreparado = false;
    private bool combateIniciado = false;

    [HideInInspector] public bool isLoadedGame = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Ya no buscamos PartidaCargada aquí. El Combat_Spawn_Manager nos avisará.
        tiempoInicio = Time.time;
        introCoroutine = StartCoroutine(SecuenciaCinematica());
    }

    private void Update()
    {
        if (introTerminada) return;

        if (Input.GetKeyDown(KeyCode.Return) && Time.time >= tiempoInicio + tiempoMinimoSinSkip)
        {
            SaltarCinematica();
        }
    }

    // --- NUEVO: FUNCIÓN DE EMERGENCIA PARA CARGAR ENEMIGOS ---
    public void ForcePoolSetup(int floor)
    {
        activeBiome = GetBiomeForFloor(floor);
        if (enemySpawner != null)
        {
            enemySpawner.SetNewPool(activeBiome.enemiesForThisBiome);
        }
    }

    private IEnumerator SecuenciaCinematica()
    {
        if (pantallaNegra != null)
        {
            pantallaNegra.gameObject.SetActive(true);
            Color c = pantallaNegra.color;
            c.a = 1f;
            pantallaNegra.color = c;
        }

        if (textoNombreZona != null)
        {
            textoNombreZona.gameObject.SetActive(false);
            Color c = textoNombreZona.color;
            c.a = 1f;
            textoNombreZona.color = c;
        }

        yield return new WaitForSeconds(0.5f);

        BiomeConfig primerBioma = GetBiomeForFloor(currentFloor);

        if (textoNombreZona != null)
        {
            if (!string.IsNullOrEmpty(tituloDelJuego))
                yield return StartCoroutine(MostrarTextoZonaCorrutina(tituloDelJuego, colorTitulo, true));

            yield return new WaitForSeconds(1f);

            if (primerBioma != null)
                yield return StartCoroutine(MostrarTextoZonaCorrutina(primerBioma.biomeName, primerBioma.textColor, false));
        }

        if (!pisoPreparado)
        {
            PrepareFloor(currentFloor);
            pisoPreparado = true;
        }

        yield return new WaitForSeconds(0.2f);

        if (!combateIniciado && Turn_Controller.instance != null)
        {
            Turn_Controller.instance.StartCombatSequence();
            combateIniciado = true;
        }

        if (pantallaNegra != null)
        {
            Color c = pantallaNegra.color;
            while (c.a > 0f)
            {
                c.a -= Time.deltaTime * velocidadAclarar;
                pantallaNegra.color = c;
                yield return null;
            }
            pantallaNegra.gameObject.SetActive(false);
        }

        introTerminada = true;
    }

    private void SaltarCinematica()
    {
        introTerminada = true;
        if (introCoroutine != null) StopCoroutine(introCoroutine);

        if (textoNombreZona != null) textoNombreZona.gameObject.SetActive(false);
        if (pantallaNegra != null) pantallaNegra.gameObject.SetActive(false);

        if (!pisoPreparado)
        {
            PrepareFloor(currentFloor);
            pisoPreparado = true;
        }

        if (!combateIniciado && Turn_Controller.instance != null)
        {
            Turn_Controller.instance.StartCombatSequence();
            combateIniciado = true;
        }
    }

    public void AdvanceToNextFloor()
    {
        currentFloor++;
        StartCoroutine(CambioDePisoCorrutina(currentFloor));
    }

    private IEnumerator CambioDePisoCorrutina(int floorNum)
    {
        if (pantallaNegra != null)
        {
            pantallaNegra.gameObject.SetActive(true);
            Color c = pantallaNegra.color;
            while (c.a < 1f) { c.a += Time.deltaTime * velocidadOscurecer; pantallaNegra.color = c; yield return null; }
        }

        LimpiarEnemigosAntiguos();
        if (gridController != null) gridController.ResetGridForNewFloor();

        Turn_Controller.instance.isInitializing = true;
        PrepareFloor(floorNum);
        yield return new WaitForSeconds(0.3f);
        Turn_Controller.instance.isInitializing = false;

        if (Turn_Controller.instance != null) Turn_Controller.instance.StartCombatSequence();

        if (pantallaNegra != null)
        {
            Color c = pantallaNegra.color;
            while (c.a > 0f) { c.a -= Time.deltaTime * velocidadAclarar; pantallaNegra.color = c; yield return null; }
            pantallaNegra.gameObject.SetActive(false);
        }
    }

    private void LimpiarEnemigosAntiguos()
    {
        Enemy[] enemigosEnEscena = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemigosEnEscena) Destroy(e.gameObject);
    }

    private IEnumerator MostrarTextoZonaCorrutina(string nombreZona, Color colorTexto, bool esIntroDelJuego)
    {
        float esperaFinal = esIntroDelJuego ? (tiempoEsperaTexto * 2f) : tiempoEsperaTexto;
        textoNombreZona.gameObject.SetActive(true);
        textoNombreZona.text = nombreZona;
        colorTexto.a = 1f;
        textoNombreZona.color = colorTexto;
        textoNombreZona.maxVisibleCharacters = 0;

        for (int i = 0; i <= nombreZona.Length; i++)
        {
            textoNombreZona.maxVisibleCharacters = i;
            yield return new WaitForSeconds(velocidadEscritura);
        }

        yield return new WaitForSeconds(esperaFinal);
        Color textColor = textoNombreZona.color;
        while (textColor.a > 0f) { textColor.a -= Time.deltaTime * 2f; textoNombreZona.color = textColor; yield return null; }
        textoNombreZona.gameObject.SetActive(false);
    }

    private void PrepareFloor(int floorNum)
    {
        activeBiome = GetBiomeForFloor(floorNum);

        if (activeBiome != null)
        {
            ActualizarFondoVisual();

            if (enemySpawner != null)
            {
                enemySpawner.SetNewPool(activeBiome.enemiesForThisBiome);

                if (!isLoadedGame)
                {
                    enemySpawner.SpawnRandomEnemies();
                    ScaleEnemiesDifficulty(floorNum);
                }
            }
        }

        if (gridController != null && !isLoadedGame)
        {
            gridController.GenerateRandomFloorEffects(Random.Range(2, 5));
        }

        // Apagamos el seguro para el siguiente piso
        isLoadedGame = false;
    }

    private void ActualizarFondoVisual()
    {
        if (activeBiome.biomeBackgroundPrefab != null)
        {
            if (currentBackgroundInstance == null || currentBackgroundInstance.name != activeBiome.biomeBackgroundPrefab.name + "(Clone)")
            {
                if (currentBackgroundInstance != null) Destroy(currentBackgroundInstance);
                currentBackgroundInstance = Instantiate(activeBiome.biomeBackgroundPrefab, Vector3.zero, Quaternion.identity);
                Renderer bgRenderer = currentBackgroundInstance.GetComponentInChildren<Renderer>();
                if (bgRenderer != null && CameraFocus.instance != null) CameraFocus.instance.SetMapBounds(bgRenderer);
            }
        }
    }

    private BiomeConfig GetBiomeForFloor(int floor)
    {
        foreach (var biome in biomes) if (floor >= biome.startFloor && floor <= biome.endFloor) return biome;
        return biomes[biomes.Length - 1];
    }

    private void ScaleEnemiesDifficulty(int floor)
    {
        if (activeBiome == null) return;
        Enemy[] spawnedEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int multiplicador = Mathf.Max(0, floor - activeBiome.startFloor);
        int bonusHealth = extraHealthPerFloor * multiplicador;
        int bonusAttack = extraAttackPerFloor * multiplicador;

        foreach (Enemy enemy in spawnedEnemies)
        {
            if (enemy.CurrentLife > 0)
            {
                enemy.ControlMaxLife(bonusHealth, true);
                enemy.HealCurrentLife(bonusHealth);
                enemy.ControlAttack(bonusAttack, true);
            }
        }
    }

    public void actualizarFloor() { textFloor.text = currentFloor.ToString(); }
}