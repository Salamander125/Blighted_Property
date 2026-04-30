using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnTimelineUI : MonoBehaviour
{
    public static TurnTimelineUI instance;

    [Header("Configuración UI")]
    public GameObject iconPrefab;
    public Transform container;

    [Header("Límites y Escalas")]
    public int maxVisibleIcons = 6;
    public Vector2 tamañoIcono = new Vector2(80, 80);
    public float normalScale = 1f;
    public float activeScale = 1.2f;

    [Header("Animación Fluida")]
    public float animDuration = 0.35f;

    // ¡NUEVO! Pool de iconos. Nunca creamos ni destruimos, solo reciclamos.
    private List<GameObject> activeIconsPool = new List<GameObject>();
    private Entity lastTurnEntity;
    private Coroutine currentAnimCoroutine;

    private void Awake()
    {
        instance = this;
    }

    public void UpdateTimeline(Queue<Entity> currentQueue, Entity activeEntity)
    {
        // Paramos cualquier animación a medias de forma segura
        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);

        // 1. RECOPILAR LOS PRÓXIMOS TURNOS
        List<Entity> upcomingEntities = new List<Entity>();

        if (activeEntity != null && activeEntity.CurrentLife > 0)
        {
            upcomingEntities.Add(activeEntity);
        }

        foreach (Entity ent in currentQueue)
        {
            if (ent != null && ent.CurrentLife > 0 && upcomingEntities.Count < maxVisibleIcons)
            {
                upcomingEntities.Add(ent);
            }
        }

        // Predicción infinita si sobran huecos
        if (upcomingEntities.Count < maxVisibleIcons && Turn_Controller.instance != null)
        {
            var turnOrder = Turn_Controller.instance.allEntities
                .Where(e => e != null && e.CurrentLife > 0)
                .OrderByDescending(e => e.CurrentSpeed)
                .ToList();

            if (turnOrder.Count > 0)
            {
                int index = 0;
                while (upcomingEntities.Count < maxVisibleIcons)
                {
                    upcomingEntities.Add(turnOrder[index]);
                    index++;
                    if (index >= turnOrder.Count) index = 0;
                }
            }
        }

        // 2. PREPARAR LA POOL DE ICONOS (Fuerza Bruta)
        // Aseguramos que siempre hay exactamente 6 iconos, ni más ni menos
        while (activeIconsPool.Count < maxVisibleIcons)
        {
            GameObject newIcon = Instantiate(iconPrefab, container);
            ConfigurarIconoBase(newIcon);
            activeIconsPool.Add(newIcon);
        }

        // Si por algún milagro hay más de 6, los borramos (no debería pasar)
        while (activeIconsPool.Count > maxVisibleIcons)
        {
            Destroy(activeIconsPool[activeIconsPool.Count - 1]);
            activeIconsPool.RemoveAt(activeIconsPool.Count - 1);
        }

        // 3. ACTUALIZAR LAS FOTOS Y PREPARAR LA ANIMACIÓN
        bool isFirstTurnEver = (lastTurnEntity == null);
        bool turnChanged = (lastTurnEntity != null && lastTurnEntity != activeEntity);

        // Actualizamos todas las fotos a la nueva ronda
        for (int i = 0; i < maxVisibleIcons; i++)
        {
            GameObject iconObj = activeIconsPool[i];
            Image img = iconObj.GetComponent<Image>();

            Entity ent = upcomingEntities[i];
            if (ent != null && ent.portraitIcon != null) img.sprite = ent.portraitIcon;

            // Colores: El primero en blanco, el resto gris
            img.color = (i == 0) ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

            // Escalas Base: El primero grande, el resto normales
            iconObj.transform.localScale = (i == 0) ? Vector3.one * activeScale : Vector3.one * normalScale;
        }

        // 4. ANIMACIÓN DE DESLIZAMIENTO
        if (turnChanged)
        {
            // Ocultamos temporalmente el último icono para que nazca de 0
            activeIconsPool[maxVisibleIcons - 1].transform.localScale = Vector3.zero;

            // El nuevo activo tiene que crecer, así que lo empezamos pequeño
            activeIconsPool[0].transform.localScale = Vector3.one * normalScale;

            // Lanzamos la corrutina especial
            currentAnimCoroutine = StartCoroutine(AnimatePoolSlide(lastTurnEntity));
        }

        lastTurnEntity = activeEntity;
    }

    private void ConfigurarIconoBase(GameObject iconObj)
    {
        RectTransform rt = iconObj.GetComponent<RectTransform>();
        rt.sizeDelta = tamañoIcono;

        LayoutElement le = iconObj.GetComponent<LayoutElement>();
        if (le == null) le = iconObj.AddComponent<LayoutElement>();
        le.minWidth = tamañoIcono.x;
        le.minHeight = tamañoIcono.y;
        le.preferredWidth = tamañoIcono.x;
        le.preferredHeight = tamañoIcono.y;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        Image img = iconObj.GetComponent<Image>();
        img.preserveAspect = true;
    }

    private IEnumerator AnimatePoolSlide(Entity oldEntity)
    {
        // 1. CREAMOS EL FANTASMA LIMPIO (Fuera del Layout Group)
        GameObject ghost = null;
        if (oldEntity != null && oldEntity.portraitIcon != null)
        {
            ghost = new GameObject("GhostIcon", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            ghost.transform.SetParent(container.parent, false); // Nace en el panel general, no en el Layout Group

            RectTransform ghostRect = ghost.GetComponent<RectTransform>();
            ghostRect.sizeDelta = tamañoIcono;

            // Lo ponemos exactamente encima del primer icono
            ghostRect.position = activeIconsPool[0].transform.position;

            Image img = ghost.GetComponent<Image>();
            img.sprite = oldEntity.portraitIcon;
            img.preserveAspect = true;

            ghost.transform.localScale = Vector3.one * activeScale;
        }

        // 2. EL EFECTO TOBOGÁN
        float elapsed = 0f;
        CanvasGroup ghostCanvas = ghost ? ghost.GetComponent<CanvasGroup>() : null;
        RectTransform ghostRt = ghost ? ghost.GetComponent<RectTransform>() : null;
        Vector2 ghostStartPos = ghostRt ? ghostRt.anchoredPosition : Vector2.zero;
        Vector2 ghostEndPos = ghostStartPos + new Vector2(0, tamañoIcono.y * 1.5f);

        GameObject newActiveIcon = activeIconsPool[0];
        GameObject newBottomIcon = activeIconsPool[maxVisibleIcons - 1];

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float curve = 1f - Mathf.Pow(1f - t, 3f); // Ease-Out Cúbico

            // El fantasma vuela y se desvanece
            if (ghostRt != null && ghostCanvas != null)
            {
                ghostRt.anchoredPosition = Vector2.Lerp(ghostStartPos, ghostEndPos, curve);
                ghostCanvas.alpha = Mathf.Lerp(1f, 0f, curve);
            }

            // El nuevo turno crece
            newActiveIcon.transform.localScale = Vector3.Lerp(Vector3.one * normalScale, Vector3.one * activeScale, curve);

            // El último turno asoma de la nada
            newBottomIcon.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * normalScale, curve);

            yield return null;
        }

        // 3. REMATE PERFECTO
        newActiveIcon.transform.localScale = Vector3.one * activeScale;
        newBottomIcon.transform.localScale = Vector3.one * normalScale;

        if (ghost != null) Destroy(ghost);
        currentAnimCoroutine = null;
    }
}