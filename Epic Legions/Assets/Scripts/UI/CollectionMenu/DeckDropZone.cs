using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Dónde van las cartas del mazo")]
    public Transform deckContainer;          // Si lo dejas vacío, usa este mismo objeto

    [Header("Prefab opcional para el mazo")]
    [Tooltip("Si lo dejas vacío, clonará el objeto original arrastrado.")]
    public GameObject deckItemPrefab;

    [Header("Feedback opcional")]
    public Image highlight;                  // Un borde/imagen para resaltar el área de drop

    public bool isDeck = true;               // Si es falso, destruye la carta arrastrada (para eliminar del mazo)
    void Awake()
    {
        if (!deckContainer) deckContainer = transform;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedGO = eventData.pointerDrag;
        if (!draggedGO || !draggedGO.GetComponent<CardUI>()) return;
        if(deckContainer == draggedGO.transform.parent) return; // No te puedes dropear a ti mismo
        

        // ¿Qué instanciamos en el mazo?
        GameObject prefab = deckItemPrefab ? deckItemPrefab : draggedGO;

        if(!prefab.GetComponent<CardDraggable>()) return; // Sólo aceptamos cartas

        if (isDeck)
        {
            if (!DeckBuilder.Instance.CanAddCardToDeck(draggedGO.GetComponent<CardUI>().CurrentCard)) return;

            // Instanciar dentro del contenedor del mazo
            var instance = Instantiate(prefab, deckContainer);
            instance.GetComponent<CardUI>().SetCard(draggedGO.GetComponent<CardUI>().CurrentCard);
            var rect = instance.transform as RectTransform;
            rect.localScale = Vector3.one * 100;

            DeckBuilder.Instance.AddCardToDeck(instance);

            // Asegurar anclas/pivote razonables (centro)
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // (Opcional) apaga el highlight
            if (highlight) highlight.enabled = false;
        }
        else
        {
            DeckBuilder.Instance.RemoveCardFromDeck(draggedGO);
            Destroy(draggedGO, 0.01f);
        }


        // Aquí podrías: actualizar contadores, validar reglas, reproducir sonido, etc.
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlight) highlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlight) highlight.enabled = false;
    }
}

