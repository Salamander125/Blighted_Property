using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Gestiona la UI del Modo Inspección. 
/// Muestra las estadísticas BASE de la entidad y resalta entre paréntesis
/// si su máximo ha sido modificado (+X o -X) por bufos o estados.
/// </summary>
public class InspectorManager : MonoBehaviour
{
    public static InspectorManager instance;

    [Header("Panel Principal y Estética")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Image panelBackground;

    [SerializeField] private Color colorAliado = new Color(0, 0.5f, 1f, 0.8f);
    [SerializeField] private Color colorEnemigo = new Color(1f, 0, 0, 0.8f);

    [SerializeField] private float offsetHorizontal = 400f;

    [Header("Barra de Vida Dinámica (Solo Enemigos)")]
    [Tooltip("El GameObject padre que contiene la barra y el texto para encenderlo/apagarlo de golpe")]
    [SerializeField] private GameObject healthBarContainer;
    [Tooltip("La imagen en modo 'Filled' que hace de barra de vida")]
    [SerializeField] private Image healthBarFill;
    [Tooltip("Texto opcional para mostrar los números exactos (Ej: 45/100)")]
    [SerializeField] private TextMeshProUGUI healthBarText;

    [Header("UI Casillas")]
    [SerializeField] private GameObject panelInfoSuelo;
    [SerializeField] private TextMeshProUGUI sueloInfoText;

    [Header("Textos de Estadísticas")]
    [SerializeField] private TextMeshProUGUI vidaBaseText;
    [SerializeField] private TextMeshProUGUI atkBaseText;
    [SerializeField] private TextMeshProUGUI manaBaseText;
    [SerializeField] private TextMeshProUGUI velBaseText;
    [SerializeField] private TextMeshProUGUI defBaseText;

    [SerializeField] private TextMeshProUGUI evasionBaseText;
    [SerializeField] private TextMeshProUGUI critBaseText;

    [Header("Iconos de Estados")]
    // --- DoTs ---
    [SerializeField] private Image imgQuemadura, imgVeneno, imgRadiacion, imgHemorragia;
    // --- HoTs ---
    [SerializeField] private Image imgVitalidad, imgLucidez;    
    // --- Buffs ---
    [SerializeField] private Image imgPrisa, imgCerteza, imgCoraza, imgFuria, imgEspejismo, imgSifon, imgBaluarte;
    // --- Debuffs ---
    [SerializeField] private Image imgFractura, imgPesadez, imgCeguera, imgFragilidad, imgFatiga, imgSilencio;
    // --- Control de Masas (CC) ---
    [SerializeField] private Image imgSueno, imgEscarcha, imgDescontrol;
  
    private Dictionary<State.StateType, Image> mapaIconos;

    private void Awake()
    {
        instance = this;
        InicializarDiccionario();
    }

    private void InicializarDiccionario()
    {
        mapaIconos = new Dictionary<State.StateType, Image>
        {
            // DoTs 
            { State.StateType.Quemadura, imgQuemadura },
            { State.StateType.Veneno, imgVeneno },
            { State.StateType.Radiacion, imgRadiacion },
            { State.StateType.Hemorragia, imgHemorragia },

            // HoTs 
            { State.StateType.Vitalidad, imgVitalidad },
            { State.StateType.Lucidez, imgLucidez },

            // Buffs 
            { State.StateType.Prisa, imgPrisa },
            { State.StateType.Certeza, imgCerteza },
            { State.StateType.Coraza, imgCoraza },
            { State.StateType.Furia, imgFuria },
            { State.StateType.Espejismo, imgEspejismo },
            { State.StateType.Sifon, imgSifon },
            { State.StateType.Baluarte, imgBaluarte },

            // Debuffs 
            { State.StateType.Fractura, imgFractura },
            { State.StateType.Pesadez, imgPesadez },
            { State.StateType.Ceguera, imgCeguera },
            { State.StateType.Fragilidad, imgFragilidad },
            { State.StateType.Fatiga, imgFatiga },
            { State.StateType.Silencio, imgSilencio },

            // CC 
            { State.StateType.Sueno, imgSueno },
            { State.StateType.Escarcha, imgEscarcha },
            { State.StateType.Cepo, imgDescontrol }
        };
    }

    public void AbrirInspector(Entity target)
    {
        if (target == null) return;

        mainPanel.SetActive(true);
        ActualizarDatos(target);

        RectTransform rect = mainPanel.GetComponent<RectTransform>();

        if (target.faction == Faction.Player)
        {
            rect.anchoredPosition = new Vector2(offsetHorizontal, 0);
            if (panelBackground != null) panelBackground.color = colorAliado;

            // Si es un aliado, APAGAMOS la barra de vida dinámica
            if (healthBarContainer != null) healthBarContainer.SetActive(false);
        }
        else
        {
            rect.anchoredPosition = new Vector2(-offsetHorizontal, 0);
            if (panelBackground != null) panelBackground.color = colorEnemigo;

            // Si es un enemigo, ENCENDEMOS la barra y la rellenamos
            if (healthBarContainer != null)
            {
                healthBarContainer.SetActive(true);
                ActualizarBarraVidaEnemigo(target);
            }
        }

        if (CameraFocus.instance != null) CameraFocus.instance.EnfocarEntidad(target);
    }

    public void CerrarInspector()
    {
        // Solo cerramos el panel de estadísticas de la entidad
        if (mainPanel != null) mainPanel.SetActive(false);

        // El texto del suelo lo apagamos solo cuando salimos del modo inspección (en el Controller)
        if (CameraFocus.instance != null) CameraFocus.instance.ResetearCamara();
    }

    private void ActualizarDatos(Entity target)
    {
        vidaBaseText.text = FormatearStat("Vida", target.BaseMaxLife, target.MaxLife);

        if (target.BaseMaxMana <= 0)
            manaBaseText.text = "Maná: <color=#888888>Sin Maná</color>";
        else
            manaBaseText.text = FormatearStat("Maná", target.BaseMaxMana, target.MaxMana);

        atkBaseText.text = FormatearStat("Ataque", target.BaseAttack, target.CurrentAttack);
        velBaseText.text = FormatearStat("Velocidad", target.BaseSpeed, target.CurrentSpeed);
        defBaseText.text = FormatearDefensa("Defensa", target.BaseDefense, target.CurrentDefense);

        if (evasionBaseText != null)
            evasionBaseText.text = FormatearStat("Evasión", target.BaseEvasion, target.CurrentEvasion) + "%";

        if (critBaseText != null)
            critBaseText.text = FormatearStat("Crítico", target.BaseCritChance, target.CurrentCritChance) + "%";

        ActualizarTodosLosIconos(target);
    }

    // --- NUEVA FUNCIÓN PARA LA BARRA ---
    private void ActualizarBarraVidaEnemigo(Entity target)
    {
        if (healthBarFill != null)
        {
            // Calculamos el porcentaje de vida actual
            healthBarFill.fillAmount = (float)target.CurrentLife / target.MaxLife;
        }

        if (healthBarText != null)
        {
            // Mostramos los números exactos para que el jugador pueda hacer matemáticas
            healthBarText.text = $"{target.CurrentLife} / {target.MaxLife}";
        }
    }

    private string FormatearStat(string etiqueta, int valorBase, int valorModificado)
    {
        int diferencia = valorModificado - valorBase;

        if (diferencia > 0)
            return $"{etiqueta}: {valorBase} <color=#00FF00>(+{diferencia})</color>";
        else if (diferencia < 0)
            return $"{etiqueta}: {valorBase} <color=#FF3333>({diferencia})</color>";

        return $"{etiqueta}: {valorBase}";
    }

    private string FormatearDefensa(string etiqueta, float valorBase, float valorModificado)
    {
        int basePorcentaje = Mathf.RoundToInt(valorBase * 100);
        int modificadoPorcentaje = Mathf.RoundToInt(valorModificado * 100);
        int diferencia = modificadoPorcentaje - basePorcentaje;

        string textoBase = basePorcentaje > 0 ? $"{basePorcentaje}%" : "<color=#888888>Sin Defensa</color>";

        if (diferencia > 0)
            return $"{etiqueta}: {textoBase} <color=#00FF00>(+{diferencia}%)</color>";
        else if (diferencia < 0)
            return $"{etiqueta}: {textoBase} <color=#FF3333>({diferencia}%)</color>";

        return $"{etiqueta}: {textoBase}";
    }

    private void ActualizarTodosLosIconos(Entity target)
    {
        foreach (var par in mapaIconos)
        {
            SetIconState(par.Value, false);
        }

        foreach (var status in target.activeEffects)
        {
            if (mapaIconos.ContainsKey(status.type))
            {
                SetIconState(mapaIconos[status.type], true);
            }
        }
    }

    private void SetIconState(Image img, bool activo)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = activo ? 1f : 0.2f;
        img.color = c;
    }

    public void ActualizarInfoCasilla(GridCell casilla)
    {
        if (casilla == null || sueloInfoText == null) return;

        // Activamos el texto (asegúrate de que el objeto padre del texto esté activo)
        panelInfoSuelo.SetActive(true);
        sueloInfoText.text = TraducirEfecto(casilla.currentEffect);
    }  

    // Añade esta para limpiar el texto del suelo al salir de TAB
    public void OcultarTextoSuelo()
    {
        if (panelInfoSuelo != null) panelInfoSuelo.SetActive(false);
    }

    private string TraducirEfecto(GridCell.CellEffectType efecto)
    {
        switch (efecto)
        {
            case GridCell.CellEffectType.Normal:
                return "Suelo Normal";

            case GridCell.CellEffectType.Fire:
                return "<color=#FF4500>Aplica: Quemadura</color>";

            case GridCell.CellEffectType.Poison:
                return "<color=#9400D3>Aplica: Veneno</color>";

            case GridCell.CellEffectType.Heal:
                return "<color=#00FF00>Efecto: Curación Inmediata</color>";

            case GridCell.CellEffectType.BuffAttack:
                return "<color=#FF0000>Aplica: Furia</color>";

            case GridCell.CellEffectType.DebuffAttack:
                return "<color=#A52A2A>Aplica: Ceguera</color>";

            case GridCell.CellEffectType.BuffSpeed:
                return "<color=#00FFFF>Efecto: +Velocidad Inmediata</color>";

            case GridCell.CellEffectType.DebuffSpeed:
                return "<color=#4169E1>Aplica: Pesadez</color>";

            case GridCell.CellEffectType.BuffDefense:
                return "<color=#C0C0C0>Efecto: +Defensa Inmediata</color>";

            case GridCell.CellEffectType.DebuffDefense:
                return "<color=#8B4513>Aplica: Fractura</color>";

            case GridCell.CellEffectType.DoubleGold:
                return "<color=#FFD700>Bono: Oro Doble</color>";

            default:
                return efecto.ToString();
        }
    }
}