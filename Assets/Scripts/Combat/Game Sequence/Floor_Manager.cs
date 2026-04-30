using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BiomeConfig
{
    public string biomeName;
    [Tooltip("El color con el que se escribirá el nombre de esta zona en pantalla")]
    public Color textColor = Color.white;
    public int startFloor;
    public int endFloor;
    [Tooltip("El Prefab del fondo que se instanciará en este bioma")]
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
    [Tooltip("Define aquí los rangos de pisos, sus fondos y qué enemigos salen")]
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

    // --- VARIABLES INTERNAS ---
    private BiomeConfig activeBiome;
    private GameObject currentBackgroundInstance;

    // --- VARIABLES DE CONTROL PARA EL SKIP ---
    private Coroutine introCoroutine;
    private bool introTerminada = false;
    private float tiempoInicio;
    public float tiempoMinimoSinSkip = 2f;
    private bool pisoPreparado = false;
    private bool combateIniciado = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Registramos a qué hora empieza la cinemática
        tiempoInicio = Time.time;

        // Lanzamos la secuencia y guardamos su referencia para poder detenerla luego
        introCoroutine = StartCoroutine(SecuenciaCinematica());
    }

    private void Update()
    {
        // Si ya terminó o la saltamos, no hacemos nada más
        if (introTerminada) return;

        // Detectar si pulsamos Enter (Return) y si ya pasaron los 2 segundos de seguridad
        if (Input.GetKeyDown(KeyCode.Return) && Time.time >= tiempoInicio + tiempoMinimoSinSkip)
        {
            SaltarCinematica();
        }
    }

    private IEnumerator SecuenciaCinematica()
    {
        // 1. Empezamos a oscuras
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

        // Un pequeño respiro de medio segundo antes de empezar a escribir
        yield return new WaitForSeconds(0.5f);

        BiomeConfig primerBioma = GetBiomeForFloor(currentFloor);

        // 2. SECUENCIA CINEMATOGRÁFICA INICIAL
        if (textoNombreZona != null)
        {
            // Primero: El título del juego (con tiempo extendido)
            if (!string.IsNullOrEmpty(tituloDelJuego))
            {
                yield return StartCoroutine(MostrarTextoZonaCorrutina(tituloDelJuego, colorTitulo, true));
            }

            // Un segundo de silencio dramático en la oscuridad
            yield return new WaitForSeconds(1f);

            // Segundo: El nombre del primer Bioma (con tiempo normal)
            if (primerBioma != null)
            {
                yield return StartCoroutine(MostrarTextoZonaCorrutina(primerBioma.biomeName, primerBioma.textColor, false));
            }
        }

        // Comprobamos la bandera por si acaso
        if (!pisoPreparado)
        {
            PrepareFloor(currentFloor);
            pisoPreparado = true;
        }

        yield return new WaitForSeconds(0.2f);

        // Arrancamos el combate
        if (!combateIniciado && Turn_Controller.instance != null)
        {
            Turn_Controller.instance.StartCombatSequence();
            combateIniciado = true;
        }

        // 3. Fade In para aclarar la pantalla y empezar a jugar
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

        // Marcamos que terminó de forma natural
        introTerminada = true;
    }

    // --- FUNCIÓN MÁGICA DE SALTO ---
    private void SaltarCinematica()
    {
        introTerminada = true;

        // 1. Detenemos la corrutina principal en seco (esto también detiene el texto que se esté escribiendo)
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
        }

        // 2. Limpieza Visual: Apagamos los textos y quitamos la pantalla negra de golpe
        if (textoNombreZona != null) textoNombreZona.gameObject.SetActive(false);
        if (pantallaNegra != null) pantallaNegra.gameObject.SetActive(false);

        // 3. Ejecutamos la lógica crítica que falte por ejecutarse
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

        Debug.Log("Cinemática saltada por el jugador.");
    }

    public void AdvanceToNextFloor()
    {
        currentFloor++;
        Debug.Log($"<color=orange>--- DESCENDIENDO AL PISO {currentFloor} ---</color>");
        StartCoroutine(CambioDePisoCorrutina(currentFloor));
    }

    private IEnumerator CambioDePisoCorrutina(int floorNum)
    {
        // 1. FADE OUT (Esperamos a que la pantalla sea TOTALMENTE negra)
        if (pantallaNegra != null)
        {
            pantallaNegra.gameObject.SetActive(true);
            Color c = pantallaNegra.color;
            while (c.a < 1f)
            {
                c.a += Time.deltaTime * velocidadOscurecer;
                pantallaNegra.color = c;
                yield return null;
            }
        }

        // 2. LIMPIEZA ATÓMICA (Ahora que nadie ve nada)
        LimpiarEnemigosAntiguos();
        if (gridController != null) gridController.ResetGridForNewFloor();

        Turn_Controller.instance.isInitializing = true;

        // 3. GENERACIÓN
        PrepareFloor(floorNum);

        // 4. ESPERA CRÍTICA (Evita que el Turn_Controller crea que ha ganado)
        // Esperamos a que los 'Awake' de los nuevos enemigos se procesen
        yield return new WaitForSeconds(0.3f);

        Turn_Controller.instance.isInitializing = false;

        // 5. ARRANCAR COMBATE
        if (Turn_Controller.instance != null)
        {
            Turn_Controller.instance.StartCombatSequence();
        }
        // 6. FADE IN: Aclarar la pantalla
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
    }

    private void LimpiarEnemigosAntiguos()
    {
        Enemy[] enemigosEnEscena = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemigosEnEscena)
        {
            Destroy(e.gameObject);
        }
    }

    // --- CORRUTINA PARA DIBUJAR EL TEXTO ---
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
        while (textColor.a > 0f)
        {
            textColor.a -= Time.deltaTime * 2f;
            textoNombreZona.color = textColor;
            yield return null;
        }

        textoNombreZona.gameObject.SetActive(false);
        textColor.a = 1f;
        textoNombreZona.color = textColor;
    }

    private void PrepareFloor(int floorNum)
    {
        if (floorNum % 10 == 0)
        {
            Debug.Log($"<color=red>¡CUIDADO! EL PISO {floorNum} ES LA SALA DEL JEFE</color>");
        }

        // Actualizamos la referencia del bioma activo inmediatamente
        activeBiome = GetBiomeForFloor(floorNum);

        if (activeBiome != null)
        {
            // Gestión del fondo (Solo si ha cambiado el bioma)
            // He simplificado esto para que use activeBiome directamente
            ActualizarFondoVisual();

            // Sincronizamos la Pool de enemigos antes del Spawn
            if (enemySpawner != null)
            {
                enemySpawner.SetNewPool(activeBiome.enemiesForThisBiome);

                // Ejecutamos el spawn y el escalado
                enemySpawner.SpawnRandomEnemies();
                ScaleEnemiesDifficulty(floorNum);
            }
        }

        if (gridController != null) gridController.GenerateRandomFloorEffects(Random.Range(2, 5));
    }

    private void ActualizarFondoVisual()
    {
        // Esta lógica estaba dentro de PrepareFloor, la he extraído para que sea más limpia
        // Aquí podrías añadir una comprobación de si el prefab es distinto al actual
        if (activeBiome.biomeBackgroundPrefab != null)
        {
            // Comprobamos si el fondo que hay es el mismo que el del nuevo bioma
            // (Si usas el mismo fondo para varios biomas, esto evita parpadeos)
            if (currentBackgroundInstance == null || currentBackgroundInstance.name != activeBiome.biomeBackgroundPrefab.name + "(Clone)")
            {
                if (currentBackgroundInstance != null) Destroy(currentBackgroundInstance);

                currentBackgroundInstance = Instantiate(activeBiome.biomeBackgroundPrefab, Vector3.zero, Quaternion.identity);

                Renderer bgRenderer = currentBackgroundInstance.GetComponentInChildren<Renderer>();
                if (bgRenderer != null && CameraFocus.instance != null)
                {
                    CameraFocus.instance.SetMapBounds(bgRenderer);
                }
            }
        }
    }

    private IEnumerator WaitAndStartTurnController()
    {
        yield return null;

        if (Turn_Controller.instance != null)
        {
            Turn_Controller.instance.StartCombatSequence();
        }
    }

    private BiomeConfig GetBiomeForFloor(int floor)
    {
        foreach (var biome in biomes)
        {
            if (floor >= biome.startFloor && floor <= biome.endFloor)
            {
                return biome;
            }
        }
        Debug.LogWarning($"No hay bioma configurado para el piso {floor}. Cargando el último disponible.");
        return biomes[biomes.Length - 1];
    }

    private void ScaleEnemiesDifficulty(int floor)
    {
        if (activeBiome == null) return;

        Enemy[] spawnedEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        int multiplicador = floor - activeBiome.startFloor;
        if (multiplicador < 0) multiplicador = 0;

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

        if (multiplicador > 0)
            Debug.Log($"<color=yellow>Escalado del Bioma aplicado (Piso {multiplicador}): Enemigos reciben +{bonusHealth} Vida y +{bonusAttack} Ataque.</color>");
    }

    public void actualizarFloor()
    {
        textFloor.text = currentFloor.ToString();
    }
}