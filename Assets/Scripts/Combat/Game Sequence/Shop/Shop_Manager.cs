using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking; // ¡Importante para la API!

public class Shop_Manager : MonoBehaviour
{
    public static Shop_Manager instance;

    [System.Serializable]
    public struct RarityBackground
    {
        public CardRarity rarity;
        public Sprite backgroundSprite;
    }

    [Header("Base de Datos API")]
    public string apiUrl = "https://blightedproperty.somee.com/api/cards";
    private List<CardModel> allDownloadedCards = new List<CardModel>();

    [Header("Configuración visual")]
    public List<RarityBackground> rarityVisuals;
    public GameObject cardPrefab;
    public Vector2[] spawnPoints;
    public GameObject confirmButton;
    public bool isShopOpen = false;

    private List<ShopCard> activeCards = new List<ShopCard>();
    private bool focusingButton = false;
    private int navIndex = 1;
    private bool canNavigate = false;
    private bool hasValidPurchase = false;

    private void Awake() => instance = this;

    private void Start()
    {
        // 1. Nada más empezar, descargamos la tienda en segundo plano
        StartCoroutine(DescargarCartasDeBDD());
    }

    private void Update()
    {
        if (isShopOpen && canNavigate && activeCards.Count > 0 && confirmButton.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) focusingButton = true;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) focusingButton = false;

            if (!focusingButton)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) navIndex = Mathf.Max(0, navIndex - 1);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) navIndex = Mathf.Min(activeCards.Count - 1, navIndex + 1);

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                    activeCards[navIndex].ToggleSelection();
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    OnConfirmNextFloor();                  
                }
            }

            ActualizarVisualesTienda();
        }
    }

    // ==========================================
    // CONEXIÓN API
    // ==========================================
    private IEnumerator DescargarCartasDeBDD()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Envolvemos el JSON recibido para que Unity lo entienda
                string jsonString = "{\"cards\":" + webRequest.downloadHandler.text + "}";
                CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>(jsonString);

                if (wrapper != null && wrapper.cards != null)
                {
                    allDownloadedCards = wrapper.cards.ToList();
                    Debug.Log($"Éxito: Se han descargado {allDownloadedCards.Count} cartas de la API.");
                }
            }
            else
            {
                Debug.LogError("Error al conectar con la API de cartas: " + webRequest.error);
            }
        }
    }

    public void OpenShop()
    {
        if (allDownloadedCards.Count == 0)
        {
            Debug.LogWarning("Las cartas aún no se han descargado o la BD está vacía.");
            return;
        }

        isShopOpen = true;
        canNavigate = false;

        foreach (var c in activeCards) if (c) Destroy(c.gameObject);
        activeCards.Clear();
        confirmButton.SetActive(true);

        // 2. Extraemos el piso actual para calcular la rareza
        int pisoActual = Floor_Manager.instance != null ? Floor_Manager.instance.currentFloor : 1;

        // 3. Obtenemos las 3 cartas dinámicas
        List<CardModel> seleccion = GetRandomCardsForFloor(pisoActual, 3);

        StartCoroutine(AnimarAparicionCartas(seleccion));
    }

    // ==========================================
    // ALGORITMO DE RAREZA
    // ==========================================
    private List<CardModel> GetRandomCardsForFloor(int floor, int count)
    {
        List<CardModel> finalSelection = new List<CardModel>();

        // Pesos matemáticos: A mayor piso, más probables las épicas y míticas
        float weightNormal = Mathf.Max(5, 100 - (floor * 5));
        float weightEspecial = 10 + (floor * 5);
        float weightEpica = floor >= 5 ? (floor * 2) : 0;
        float weightMitica = floor >= 15 ? (floor * 1.5f) : 0;

        float totalWeight = weightNormal + weightEspecial + weightEpica + weightMitica;

        for (int i = 0; i < count; i++)
        {
            float roll = Random.Range(0, totalWeight);
            CardRarity selectedRarity;

            if (roll < weightNormal) selectedRarity = CardRarity.Normal;
            else if (roll < weightNormal + weightEspecial) selectedRarity = CardRarity.Especial;
            else if (roll < weightNormal + weightEspecial + weightEpica) selectedRarity = CardRarity.Epica;
            else selectedRarity = CardRarity.Mitica;

            var poolDeRareza = allDownloadedCards.Where(c => c.rarity == selectedRarity).ToList();

            // Respaldo de seguridad: si no hay cartas de esa rareza en la BDD, baja un nivel
            while (poolDeRareza.Count == 0 && selectedRarity > CardRarity.Normal)
            {
                selectedRarity--;
                poolDeRareza = allDownloadedCards.Where(c => c.rarity == selectedRarity).ToList();
            }

            if (poolDeRareza.Count > 0)
            {
                CardModel chosenCard = poolDeRareza[Random.Range(0, poolDeRareza.Count)];
                finalSelection.Add(chosenCard);
            }
        }

        return finalSelection;
    }

    // ==========================================
    // ANIMACIONES Y LÓGICA DE COMPRA
    // ==========================================
    private IEnumerator AnimarAparicionCartas(List<CardModel> seleccion)
    {
        for (int i = 0; i < seleccion.Count; i++)
        {
            Vector3 startPos = (Vector3)spawnPoints[i] + Vector3.down * 5f;
            GameObject go = Instantiate(cardPrefab, startPos, Quaternion.identity);

            ShopCard sc = go.GetComponent<ShopCard>();
            Sprite spriteToUse = rarityVisuals.Find(v => v.rarity == seleccion[i].rarity).backgroundSprite;
            sc.SetupCard(seleccion[i], spriteToUse);

            activeCards.Add(sc);
            StartCoroutine(MoverCartaSuave(go.transform, (Vector3)spawnPoints[i]));
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.4f);
        canNavigate = true;
    }

    private IEnumerator MoverCartaSuave(Transform objeto, Vector3 targetPos)
    {
        float duracion = 0.5f;
        float transcurrido = 0;
        Vector3 posInicial = objeto.position;
        Vector3 escalaOriginal = new Vector3(10f, 10f, 10f);

        objeto.localScale = Vector3.zero;

        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            float t = transcurrido / duracion;
            float curve = t * t * (3f - 2f * t);

            objeto.position = Vector3.Lerp(posInicial, targetPos, curve);
            objeto.localScale = Vector3.Lerp(Vector3.zero, escalaOriginal, curve);
            yield return null;
        }

        objeto.position = targetPos;
        objeto.localScale = escalaOriginal;
    }

    public void OnConfirmNextFloor() => StartCoroutine(ConfirmAndRestartRoutine());

    private IEnumerator ConfirmAndRestartRoutine()
    {
        var cartasCompradas = activeCards.Where(c => c.isSelected).ToList();
        int total = cartasCompradas.Sum(c => c.cost);

        // 1. Validar el oro PRIMERO
        if (Turn_Controller.instance.totalGold < total)
        {
            Debug.LogWarning("No tienes oro suficiente. Ajusta tu selección.");
            hasValidPurchase = false;

            // Desenfocamos el botón para que vuelva a las cartas y deje de estar amarillo
            focusingButton = false;

            // Salimos de la corrutina sin bloquear nada
            yield break;
        }

        canNavigate = false;
        hasValidPurchase = true;
        focusingButton = true;

        if (confirmButton != null)
            confirmButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

        // Procesar la compra si hay cartas
        if (cartasCompradas.Count > 0)
        {
            Turn_Controller.instance.totalGold -= total;
            UIManager_Combat.instance.UpdateGoldUI(Turn_Controller.instance.totalGold);

            foreach (var carta in cartasCompradas)
            {
                AplicarBufoAlEquipo(carta.data);
                SaveManager.instance.currentRunCards.Add(carta.data.id);
            }
        }
        else
        {
            Debug.Log("No has seleccionado nada. Saltando la tienda...");
        }

        // Transición y cierre de la tienda
        yield return new WaitForSeconds(1.5f);
        FinalizarTienda();
    }

    private void AplicarBufoAlEquipo(CardModel carta) // <-- Recibe CardModel
    {
        foreach (Entity entidad in Turn_Controller.instance.allEntities)
        {
            if (entidad != null && entidad.faction == Faction.Player)
            {
                switch (carta.buffType)
                {
                    case BuffType.MaxLife: entidad.ControlMaxLife((int)carta.amount, true); break;
                    case BuffType.MaxMana: entidad.ControlMaxMana((int)carta.amount, true); break;
                    case BuffType.Attack: entidad.ControlAttack((int)carta.amount, true); break;
                    case BuffType.Speed: entidad.ControlSpeed((int)carta.amount, true); break;
                    case BuffType.Defense: entidad.ControlDefense(carta.amount / 100f, true); break;
                    case BuffType.HealCurrentLife: entidad.HealCurrentLife((int)carta.amount); break;
                    case BuffType.RestoreCurrentMana: entidad.RestoreCurrentMana((int)carta.amount); break;
                    case BuffType.BonusLifeRegenPerFloor: entidad.lifeRegenPerFloor += (int)carta.amount; break;
                    case BuffType.BonusManaRegenPerFloor: entidad.manaRegenPerFloor += (int)carta.amount; break;
                    case BuffType.CriticalChance: entidad.ControlCritChance((int)carta.amount, true); break;
                    case BuffType.Evasion: entidad.ControlEvasion((int)carta.amount, true); break;                    
                }

                if (carta.appliesState)
                {
                    State.ActiveStatus nuevoEstado = new State.ActiveStatus(carta.stateToApply, carta.stateDuration, carta.stateIntensity);
                    entidad.activeEffects.Add(nuevoEstado);
                }

                if (entidad.TryGetComponent(out Character_Controller charController))
                {
                    charController.RefreshUI();
                }
            }
        }
    }

    private IEnumerator RestartCombatLoop()
    {
        if (confirmButton != null) confirmButton.GetComponent<UnityEngine.UI.Button>().interactable = true;
        yield return new WaitForEndOfFrame();
        Floor_Manager.instance.actualizarFloor();
        Turn_Controller.instance.FindAllEntities();
        Turn_Controller.instance.StartCoroutine("ProcessNextTurn");
        isShopOpen = false;
    }

    private void ActualizarVisualesTienda()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            if (activeCards[i] == null) continue;

            bool isFocused = (i == navIndex && !focusingButton);
            float targetY = isFocused ? spawnPoints[i].y + 0.8f : spawnPoints[i].y;
            float targetScale = isFocused ? 11f : 10f;

            Vector3 targetPos = new Vector3(spawnPoints[i].x, targetY, 0);
            activeCards[i].transform.position = Vector3.Lerp(activeCards[i].transform.position, targetPos, Time.deltaTime * 10f);
            activeCards[i].transform.localScale = Vector3.Lerp(activeCards[i].transform.localScale, Vector3.one * targetScale, Time.deltaTime * 10f);
        }

        if (confirmButton != null)
        {
            float btnScale = focusingButton ? 2.58f : 2.38f;
            confirmButton.transform.localScale = Vector3.Lerp(confirmButton.transform.localScale, Vector3.one * btnScale, Time.deltaTime * 10f);
            confirmButton.GetComponent<UnityEngine.UI.Image>().color = focusingButton ? Color.yellow : Color.white;
        }
    }

    private void FinalizarTienda()
    {
        foreach (var c in activeCards)
            if (c != null) Destroy(c.gameObject);

        activeCards.Clear();
        confirmButton.SetActive(false);

        Turn_Controller.instance.PrepareNextFloor();

        Enemy_Spawn spawner = FindAnyObjectByType<Enemy_Spawn>();
        spawner.SpawnRandomEnemies();

        StartCoroutine(RestartCombatLoop());
    }
}