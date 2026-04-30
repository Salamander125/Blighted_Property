using UnityEngine;
using TMPro;
using System.Collections;

// Gestiona el texto cinemático que aparece durante los ataques de los enemigos.
public class BattleLogUI : MonoBehaviour
{
    public static BattleLogUI instance;

    [Header("Referencias UI")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI logText;

    [Header("Ajustes Cinemáticos")]
    [SerializeField] private float typewriterSpeed = 0.03f;
    [Tooltip("Píxeles que el panel flotará por encima del centro del enemigo")]
    [SerializeField] private float offsetVertical = 150f; // Bajado un poco por defecto

    private Transform currentAttacker;
    private Canvas parentCanvas;

    private void Awake()
    {
        instance = this;
        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(false);
            // Buscamos el Canvas padre una sola vez al empezar
            parentCanvas = panelRect.GetComponentInParent<Canvas>();
        }
    }

    public IEnumerator ShowLogRoutine(string message, Transform attacker)
    {
        if (panelRect == null || logText == null) yield break;

        currentAttacker = attacker;

        // 1. Teletransporte instantáneo
        ActualizarPosicionPanel(1f);

        // 2. Encendemos el panel y empezamos a escribir
        panelRect.gameObject.SetActive(true);
        logText.text = message;
        logText.maxVisibleCharacters = 0;

        for (int i = 0; i <= message.Length; i++)
        {
            logText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    public void ClearLog()
    {
        if (panelRect != null) panelRect.gameObject.SetActive(false);
        currentAttacker = null;
    }

    private void LateUpdate()
    {
        if (panelRect.gameObject.activeSelf && currentAttacker != null)
        {
            ActualizarPosicionPanel(Time.deltaTime * 15f);
        }
    }

    private void ActualizarPosicionPanel(float velocidadLerp)
    {
        if (parentCanvas == null || Camera.main == null) return;

        // 1. Dónde está el enemigo en la pantalla de la cámara
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(currentAttacker.position);

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // 2. MAGIA: Convertimos ese punto de la pantalla a las coordenadas exactas de TU Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 localPoint);

        // 3. Le sumamos tu offset
        localPoint.y += offsetVertical;

        // 4. Movemos el panel suavemente (usamos localPosition para no romper la UI)
        panelRect.localPosition = Vector3.Lerp(panelRect.localPosition, localPoint, velocidadLerp);
    }
}