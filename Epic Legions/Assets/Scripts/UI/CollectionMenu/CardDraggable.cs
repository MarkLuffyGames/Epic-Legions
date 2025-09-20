using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Opcional")]
    [Tooltip("Canvas (overlay) donde se dibuja el clon durante el drag. Si lo dejas vacío, se crea uno automáticamente.")]
    public Canvas dragCanvas;

    [Tooltip("Prefab visual a usar como clon. Si está vacío, clona esta misma carta.")]
    public GameObject ghostPrefab;

    [Range(0.1f, 1f)]
    public float ghostAlpha = 0.95f;

    public bool isClone;

    private GameObject ghost;                // instancia visual que sigue al puntero
    private RectTransform ghostRect;
    private Canvas effectiveCanvas;          // canvas realmente usado para el drag
    private CanvasGroup originalCg;          // para bajar/subir alpha mientras arrastras

    void Awake()
    {
        originalCg = GetComponent<CanvasGroup>();
        if (!originalCg) originalCg = gameObject.AddComponent<CanvasGroup>();
    }

    // ========== IBeginDrag ==========
    public void OnBeginDrag(PointerEventData eventData)
    {
        if(DeckBuilder.Instance.currentState != DeckBuilderState.CreatingDeck
            && DeckBuilder.Instance.currentState != DeckBuilderState.EditingDeck) return;
        if (isClone) return; // los clones no se pueden arrastrar
        EnsureDragCanvas();

        // 1) Elegimos la fuente del clon (prefab o la propia carta)
        GameObject src = ghostPrefab ? ghostPrefab : gameObject;

        // 2) Instanciamos el clon dentro del canvas superior
        ghost = Instantiate(src, effectiveCanvas.transform);
        ghost.name = $"{gameObject.name}__DRAG_GHOST";
        ghostRect = ghost.transform as RectTransform;
        ghostRect.anchorMin = ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z) * 1.2f;

        // 3) Aseguramos que el clon NO bloquee raycasts (para que las zonas de drop funcionen)
        var ghostCg = ghost.GetComponent<CanvasGroup>();
        if (!ghostCg) ghostCg = ghost.AddComponent<CanvasGroup>();
        ghostCg.blocksRaycasts = false;
        ghostCg.alpha = ghostAlpha;

        ghost.GetComponent<CardDraggable>().isClone = true;

        // 5) Dejamos la carta original un poco translúcida (feedback)
        originalCg.alpha = 0.6f;

        originalCg.blocksRaycasts = false;

        // 6) Posicionamos el clon bajo el puntero
        FollowPointer(eventData);
    }

    // ========== IDrag ==========
    public void OnDrag(PointerEventData eventData)
    {
        if (DeckBuilder.Instance.currentState != DeckBuilderState.CreatingDeck
            && DeckBuilder.Instance.currentState != DeckBuilderState.EditingDeck) return;
        if (isClone) return; // los clones no se pueden arrastrar
        FollowPointer(eventData);
    }

    // ========== IEndDrag ==========
    public void OnEndDrag(PointerEventData eventData)
    {
        if (DeckBuilder.Instance.currentState != DeckBuilderState.CreatingDeck
            && DeckBuilder.Instance.currentState != DeckBuilderState.EditingDeck) return;
        if (isClone) return; // los clones no se pueden arrastrar
        // 7) Al soltar: destruimos el clon y restauramos la carta original
        if (ghost) Destroy(ghost);
        ghost = null;
        ghostRect = null;

        originalCg.alpha = 1f;
        originalCg.blocksRaycasts = true;
    }

    // ----------------------------------------------------------

    private void FollowPointer(PointerEventData eventData)
    {
        if (!ghostRect || !effectiveCanvas) return;

        if (effectiveCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Overlay: posición de pantalla directa
            ghostRect.position = eventData.position;
        }
        else
        {
            // ScreenSpace-Camera o World: convertir a mundo
            Camera cam = effectiveCanvas.worldCamera;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                effectiveCanvas.transform as RectTransform,
                eventData.position,
                cam,
                out var worldPos
            );
            ghostRect.position = worldPos;
        }
        //ghostRect.localScale = Vector3.one; // por si el layout/escala del canvas difiere
    }

    private void EnsureDragCanvas()
    {
        if (effectiveCanvas) return;

        // Usa el canvas asignado si hay
        if (dragCanvas)
        {
            effectiveCanvas = dragCanvas;
            ForceCanvasOnTop(effectiveCanvas);
            return;
        }

        // Busca uno llamado "DragCanvas" (por si ya lo tienes en la escena)
        var found = GameObject.Find("DragCanvas");
        if (found && found.TryGetComponent<Canvas>(out var c))
        {
            effectiveCanvas = c;
            ForceCanvasOnTop(effectiveCanvas);
            return;
        }

        // Si no existe, lo creamos automáticamente (Overlay + orden alto)
        var go = new GameObject("DragCanvas (Auto)", typeof(Canvas));
        effectiveCanvas = go.GetComponent<Canvas>();
        effectiveCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ForceCanvasOnTop(effectiveCanvas);
        // Este canvas solo es para el drag; no necesita GraphicRaycaster.
    }

    private void ForceCanvasOnTop(Canvas c)
    {
        c.overrideSorting = true;
        c.sortingOrder = 5000; // bien arriba de todos los canvases normales
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //throw new System.NotImplementedException();
    }
}

