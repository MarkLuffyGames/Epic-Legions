using UnityEngine;

public class CardSelector : MonoBehaviour
{
    [SerializeField] private HandCardHandler handCardHandler;
    [SerializeField] private PlayerManager playerManager;

    // LayerMask para filtrar las cartas
    public LayerMask cardLayer;
    //Layer para filtrar las posiciones en el campo.
    public LayerMask fieldLayer;

    // Variable para almacenar la carta actualmente seleccionada
    private Card currentCard;
    // Variable para almacenar la posicion en el campo seleccionada
    private FieldPosition currentFieldPosition;

    // Bandera para verificar si se ha hecho clic en una carta
    private bool isClickingCard = false;
    private bool isHoldingCard = false;

    private float clickHoldTime = 0.2f; // Tiempo para diferenciar entre clic y arrastre
    private float mouseDownTime; // Tiempo cuando el mouse fue presionado

    private bool isAnyFocusedCard;

    void Update()
    {
        DetectCardUnderMouse();

        // Detecta si el clic izquierdo del mouse está presionado
        if (Input.GetMouseButtonDown(0) && currentCard != null)
        {
            OnMouseDownCard(currentCard);
            isClickingCard = true;
            isHoldingCard = true;
        }

        // Detecta si el clic izquierdo del mouse se suelta
        if (Input.GetMouseButtonUp(0) && isClickingCard)
        {
            OnMouseUpCard(currentCard);
            isClickingCard = false;
        }

        // Si se está manteniendo la carta, llama al método OnCardHeld
        if (isHoldingCard && currentCard != null)
        {
            OnCardHeld(currentCard);
        }
    }

    /// <summary>
    /// Detecta si el mouse está sobre una carta
    /// </summary>
    private void DetectCardUnderMouse()
    {
        if (isHoldingCard || isAnyFocusedCard) return;

        // Convierte la posición del mouse en un rayo en el espacio de la cámara
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Lanza el rayo y verifica si colisiona con una carta en la capa de cartas
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, cardLayer))
        {
            Card card = hit.collider.GetComponent<Card>();

            // Verifica si es una nueva carta bajo el mouse
            if (currentCard != card)
            {
                if (currentCard != null)
                {
                    // Llamar a un método para cuando el mouse sale de la carta anterior
                    OnMouseExitCard(currentCard);
                }

                // Actualiza la carta actual y llama a OnMouseEnterCard

                currentCard = card;
                OnMouseEnterCard(card);
            }
        }
        else
        {
            // Si no detecta ninguna carta y había una seleccionada, llama a OnMouseExitCard
            if (currentCard != null)
            {
                OnMouseExitCard(currentCard);
                currentCard = null;
            }
        }
    }

    /// <summary>
    /// Método para cuando el mouse entra en una carta
    /// </summary>
    /// <param name="card">Carta a la que entra el mouse</param>
    private void OnMouseEnterCard(Card card)
    {
        if (handCardHandler.IsCardOwnedByPlayer(card) && !isAnyFocusedCard)
        {
            card.Highlight();
        }
        
    }

    /// <summary>
    /// Método para cuando el mouse sale de una carta
    /// </summary>
    /// <param name="card">Carta de la que salio el mouse</param>
    private void OnMouseExitCard(Card card)
    {
        if (handCardHandler.IsCardOwnedByPlayer(card) && !isAnyFocusedCard)
        {
            card.RemoveHighlight();
        }
    }

    /// <summary>
    /// Método para cuando se hace clic en la carta
    /// </summary>
    /// <param name="card">Carta a la que se le hace clic</param>
    private void OnMouseDownCard(Card card)
    {
        mouseDownTime = Time.time; // Registra el tiempo en que se hizo clic
    }

    /// <summary>
    /// Método para cuando se suelta el clic sobre la carta
    /// </summary>
    /// <param name="card">Carta que se solto</param>
    private void OnMouseUpCard(Card card)
    {
        // Calcula el tiempo que el mouse se mantuvo presionado
        float heldTime = Time.time - mouseDownTime;

        // Si el tiempo es menor al tiempo de "clickHoldTime", considera como clic rápido para enfocar
        if (heldTime < clickHoldTime)
        {
            if (!isAnyFocusedCard && card.isVisible)
            {
                card.RemoveHighlight();
                isAnyFocusedCard = card.Enlarge();
            }
            else if(isAnyFocusedCard)
            {
                card.ResetSize();
                isAnyFocusedCard = false;
            }
        }

        if (currentCard != null && isHoldingCard)
        {
            isHoldingCard = false;
            if (handCardHandler.IsCardOwnedByPlayer(card))
            {
                card.StopDragging();
            }
        }

        if(currentFieldPosition != null && handCardHandler.IsCardOwnedByPlayer(currentCard) && currentFieldPosition.IsFree())
        {
            handCardHandler.QuitCard(currentCard);
            currentFieldPosition.SetCard(currentCard);
            currentFieldPosition = null;
        }

        playerManager.HideAvailablePositions();
        handCardHandler.ShowHandCard();
    }

    /// <summary>
    /// Método para cuando se mantiene la carta
    /// </summary>
    /// <param name="card">Carta que se esta manteniendo</param>
    private void OnCardHeld(Card card)
    {
        float heldTime = Time.time - mouseDownTime;

        if (handCardHandler.IsCardOwnedByPlayer(card) && !isAnyFocusedCard)
        {
            card.StartDragging();

            if(heldTime > clickHoldTime)
            {
                playerManager.ShowAvailablePositions();
                handCardHandler.HideHandCard();
                DetectPositionForPlaceCard();
            }
        }
    }

    /// <summary>
    /// Detecta la posicion del campo bajo el mouse.
    /// </summary>
    private void DetectPositionForPlaceCard()
    {
        // Convierte la posición del mouse en un rayo en el espacio de la cámara
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Lanza el rayo y verifica si colisiona con una posicion de campo.
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, fieldLayer))
        {
            FieldPosition fieldPosition = hit.collider.GetComponent<FieldPosition>();

            // Verifica si es una nueva posicion bajo el mouse
            if (currentFieldPosition != fieldPosition)
            {
                // Actualiza la posicion actual y llama a OnMouseEnterPosiction

                currentFieldPosition = fieldPosition;
                OnMouseEnterPosition(fieldPosition);
            }
        }
        else
        {
            // Si no detecta ninguna posicion y había una seleccionada, llama a OnMouseExitPosiction
            if (currentFieldPosition != null)
            {
                OnMouseExitPosition(currentFieldPosition);
                currentFieldPosition = null;
            }
        }
    }

    /// <summary>
    /// Cuando el mouse deja de estar sobre la posicion del campo.
    /// </summary>
    /// <param name="fieldPosition"></param>
    private void OnMouseExitPosition(FieldPosition fieldPosition)
    {
        currentCard.MoveToLastPosition();
    }

    /// <summary>
    /// Cuando el mouse entra en una posicion del campo.
    /// </summary>
    /// <param name="fieldPosition"></param>
    private void OnMouseEnterPosition(FieldPosition fieldPosition)
    {
        if (fieldPosition.IsFree())
        {
            currentCard.MoveToPosition(fieldPosition.transform.position + Vector3.up, 20, true, false);
            currentCard.RotateToAngle(new Vector3(90, 0, 0), 20);
        }
    }
}

