using Unity.Netcode;
using UnityEditor;
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
        if(NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost) return;

        DetectCardUnderMouse();

        if (HandCardHandler.IsMouseOverButton()) return;

        // Detecta si el clic izquierdo del mouse está presionado sobre una carta
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

        if (isHoldingCard || isAnyFocusedCard)
        {
            if (DuelManager.instance.settingAttackTarget)
            {
                isAnyFocusedCard = false;
            }
            return;
        } 

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
        HighlightCardInHand(card);
    }

    /// <summary>
    /// Método para cuando el mouse sale de una carta
    /// </summary>
    /// <param name="card">Carta de la que salio el mouse</param>
    private void OnMouseExitCard(Card card)
    {
        RemoveHighlightCardInHand(card);
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

        // Si el tiempo es menor al tiempo de "clickHoldTime", considera como clic rápido
        if (heldTime < clickHoldTime)
        {
            OnQuickClick(card);
        }

        
        //Cuando se suelta el click y se cuplen las condiciones coloca la carta en la pocion en el campo.
        if(currentFieldPosition != null && handCardHandler.CardInThePlayerHand(currentCard) 
            && currentFieldPosition.IsFree() && DuelManager.instance.GetDuelPhase() == DuelPhase.Preparation
            && !playerManager.isReady)
        {
            PlaceCardOnTheField(card);
        }
        //Si se suelta la carta pero no se puede colocar en el campo dejar de arrastrar.
        else if (currentCard != null && isHoldingCard)
        {
            isHoldingCard = false;
            if (handCardHandler.CardInThePlayerHand(card))
            {
                card.StopDragging(true);
            }
        }

        //En cualquier caso se ocultan las pocisiones disponibles en el campo.
        playerManager.HideAvailablePositions();
        //Si la carta que se solto no esta pocisionada en el campo mostrar las cartas de la mano.
        if(card.FieldPosition == null)
        {
            handCardHandler.ShowHandCard();
        }
    }

    /// <summary>
    /// Se llama a este metodo cuando se realiza un clic rapido sobre una carta.
    /// </summary>
    private void OnQuickClick(Card card)
    {
        //Si se esta estableciendo el objetivo de ataque la carta seleccionada es la carta a la que se debe atacar.
        if (DuelManager.instance.settingAttackTarget)
        {
            if (!playerManager.GetFieldPositionList().Contains(card.FieldPosition))
            {
                DuelManager.instance.HeroAttackServerRpc(card.FieldPosition.PositionIndex, NetworkManager.Singleton.LocalClientId);
            }
            
        }
        //Si no hay ninguna carta enfocada enfocar la carta seleccionada.
        else if (!isAnyFocusedCard && card.isVisible)
        {
            card.RemoveHighlight();
            isAnyFocusedCard = card.Enlarge();
        }
        //Si hay una carta enfocada desenfocar la carta.
        else if (isAnyFocusedCard)
        {
            card.ResetSize();
            isAnyFocusedCard = false;
        }
    }

    /// <summary>
    /// Método para cuando se mantiene la carta
    /// </summary>
    /// <param name="card">Carta que se esta manteniendo</param>
    private void OnCardHeld(Card card)
    {
        float heldTime = Time.time - mouseDownTime;

        if (handCardHandler.CardInThePlayerHand(card) && !isAnyFocusedCard
            && DuelManager.instance.GetDuelPhase() == DuelPhase.Preparation
            && !playerManager.isReady && !card.waitForServer)
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

    ////////////////////////////////////////////////////////////////

    private void HighlightCardInHand(Card card)
    {
        if (handCardHandler.CardInThePlayerHand(card) && !isAnyFocusedCard)
        {
            card.Highlight();
        }
    }

    private void RemoveHighlightCardInHand(Card card)
    {
        if (handCardHandler.CardInThePlayerHand(card))
        {
            card.RemoveHighlight();
        }
    }

    private void PlaceCardOnTheField(Card card)
    {
        card.waitForServer = true;
        isHoldingCard = false;
        card.StopDragging(false);

        DuelManager.instance.PlaceCardOnTheFieldServerRpc(
            handCardHandler.GetIdexOfCard(currentCard),
            currentFieldPosition.GetPositionIndex(),
            NetworkManager.Singleton.LocalClientId);

        currentFieldPosition = null;
    }
}

