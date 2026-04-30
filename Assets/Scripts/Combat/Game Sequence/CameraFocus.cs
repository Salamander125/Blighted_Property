using UnityEngine;

// Gestiona el enfoque cinemático de la cámara sobre las entidades.
public class CameraFocus : MonoBehaviour
{
    public static CameraFocus instance;

    [Header("Ajustes Cinemáticos (Inspección)")]
    [SerializeField] private float suavizado = 6f;
    [SerializeField] private float zoomSize = 4.5f;
    [SerializeField] private float desplazamientoX = 2.5f;

    [Header("Ajustes Cine (Ataque)")]
    [SerializeField] private float zoomAtaqueCine = 3.5f;

    // YA NO ES SERIALIZEFIELD. Ahora lo recibirá por código.
    private Renderer mapRenderer;

    private Camera mainCam;
    private Vector3 posOriginal;
    private float sizeOriginal;

    private bool enfocando = false;
    private Vector3 targetPos;
    private float targetZoom;

    private void Awake()
    {
        instance = this;
        mainCam = GetComponent<Camera>();

        posOriginal = transform.position;
        sizeOriginal = mainCam.orthographicSize;
        targetZoom = sizeOriginal;
    }

    // ==========================================
    // NUEVO: ACTUALIZADOR DE LÍMITES DINÁMICO
    // ==========================================
    public void SetMapBounds(Renderer newBackgroundRenderer)
    {
        mapRenderer = newBackgroundRenderer;
        Debug.Log("<color=cyan>Cámara: Nuevos límites de bioma recibidos.</color>");
    }

    public void EnfocarEntidad(Entity target)
    {
        enfocando = true;
        targetZoom = zoomSize;

        float offsetCamara = target.faction == Faction.Player ? -desplazamientoX : desplazamientoX;

        targetPos = new Vector3(target.transform.position.x + offsetCamara,
                                target.transform.position.y,
                                posOriginal.z);
    }

    public void EnfocarAtacanteCine(Entity attacker)
    {
        enfocando = true;
        targetZoom = zoomAtaqueCine;

        targetPos = new Vector3(attacker.transform.position.x,
                                attacker.transform.position.y,
                                posOriginal.z);
    }

    public void ResetearCamara()
    {
        enfocando = false;
        targetZoom = sizeOriginal;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, CalculateSafeTargetPosition(), Time.deltaTime * suavizado);
        mainCam.orthographicSize = Mathf.Lerp(mainCam.orthographicSize, targetZoom, Time.deltaTime * suavizado);
    }

    private Vector3 CalculateSafeTargetPosition()
    {
        Vector3 destinoTeorico = enfocando ? targetPos : posOriginal;

        // Si el Floor_Manager aún no nos ha dado el fondo, nos quedamos quietos
        if (mapRenderer == null) return destinoTeorico;

        Bounds mapBounds = mapRenderer.bounds;
        float currentHalfHeight = mainCam.orthographicSize;
        float currentHalfWidth = currentHalfHeight * mainCam.aspect;

        float clampedX = Mathf.Clamp(destinoTeorico.x, mapBounds.min.x + currentHalfWidth, mapBounds.max.x - currentHalfWidth);
        float clampedY = Mathf.Clamp(destinoTeorico.y, mapBounds.min.y + currentHalfHeight, mapBounds.max.y - currentHalfHeight);

        return new Vector3(clampedX, clampedY, posOriginal.z);
    }
}