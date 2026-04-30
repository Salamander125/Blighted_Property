using UnityEngine;

public class StickyButtons : MonoBehaviour
{
    [Header("Posicionamiento Relativo")]
    [Tooltip("0.1 significa el 10% del ancho de la pantalla")]
    [SerializeField] private float horizontalOffsetPercent = 0.1f;
    [SerializeField] private float verticalOffsetPercent = 0.05f;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Canvas parentCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (Turn_Controller.instance != null && Turn_Controller.instance.currentEntity != null)
        {
            Entity current = Turn_Controller.instance.currentEntity;

            if (current.faction == Faction.Player)
            {
                SetChildrenActive(true);

                // 1. Posición del personaje en el Canvas
                Vector2 screenPos = Camera.main.WorldToScreenPoint(current.transform.position);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, parentCanvas.worldCamera, out localPoint);

                // 2. CALCULAMOS EL OFFSET DINÁMICO
                // Usamos el tamaño actual del Canvas (que cambia según la resolución)
                float dynamicX = canvasRect.sizeDelta.x * horizontalOffsetPercent;
                float dynamicY = canvasRect.sizeDelta.y * verticalOffsetPercent;
                Vector2 dynamicOffset = new Vector2(dynamicX, dynamicY);

                // 3. Aplicamos
                rectTransform.anchoredPosition = localPoint + dynamicOffset;
            }
            else
            {
                SetChildrenActive(false);
            }
        }
    }

    private void SetChildrenActive(bool active)
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf != active)
                child.gameObject.SetActive(active);
        }
    }
}