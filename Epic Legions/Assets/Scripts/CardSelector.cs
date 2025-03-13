using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardSelector : MonoBehaviour
{
    [SerializeField] private HandCardHandler handCardHandler;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private DuelManager duelManager;

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
        if (((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)) && currentCard != null)
        {
            OnMouseDownCard(currentCard);
            isClickingCard = true;
            isHoldingCard = true;
        }

        // Detecta si el clic izquierdo del mouse se suelta
        if (((Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)) && isClickingCard)
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
            if (duelManager.SettingAttackTarget)
            {
                isAnyFocusedCard = false;
            }
            return;
        }

        Ray ray = new();

        // Detectar entrada del mouse
        if (Mouse.current != null)
        {
            // Convierte la posición del mouse en un rayo en el espacio de la cámara
            ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        }

        // Detectar entrada táctil
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            // Convierte la posición del toque en un rayo en el espacio de la cámara
            ray = Camera.main.ScreenPointToRay(Touchscreen.current.primaryTouch.position.ReadValue());
        }

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
            && currentFieldPosition.IsFree() && card.UsableCard()
            && !playerManager.isReady)
        {
            if (card.cardSO is HeroCardSO && currentFieldPosition.PositionIndex != -1)
            {
                PlaceCardOnTheField(card);
            }
            else if(card.cardSO is SpellCardSO && currentFieldPosition.PositionIndex == -1)
            {
                PlaceCardOnTheField(card);
            }
            else if(currentCard != null && isHoldingCard)
            {
                isHoldingCard = false;
                if (handCardHandler.CardInThePlayerHand(card))
                {
                    card.StopDragging(true);
                }
            }
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
        if(handCardHandler.GetCardInHandList().Contains(card))
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
        if (duelManager.SettingAttackTarget)
        {
            if (card.isAttackable)
            {
                duelManager.CardSelectingTarget.actionIsReady = true;

                if (duelManager.IsSinglePlayer)
                {
                    duelManager.ProcessAttack(card.FieldPosition.PositionIndex,
                        duelManager.CardSelectingTarget.cardSO is HeroCardSO,
                        duelManager.CardSelectingTarget.FieldPosition.PositionIndex,
                        duelManager.MovementToUse, 1);

                }
                else
                {
                    duelManager.ProcessAttackServerRpc(card.FieldPosition.PositionIndex,
                    NetworkManager.Singleton.LocalClientId,
                    duelManager.CardSelectingTarget.cardSO is HeroCardSO,
                    duelManager.CardSelectingTarget.FieldPosition.PositionIndex,
                    duelManager.MovementToUse);
                }

                duelManager.CardSelectingTarget.EndTurn();
                duelManager.DisableAttackableTargets();
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

        if (handCardHandler.CardInThePlayerHand(card) && !isAnyFocusedCard//La carta esta en la mano del jugador y no esta enfocada
            && !playerManager.isReady && !card.waitForServer //El jugador no puede estar listo para avanzar a la siguinte fase ni la carta estar esperando respuesta del servidor.
            && card.UsableCard()) //La carta puede ser usada. 
        {
            card.StartDragging();

            if(heldTime > clickHoldTime)
            {
                playerManager.ShowAvailablePositions(card);
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

        Ray ray = new();

        // Detectar entrada del mouse
        if (Mouse.current != null)
        {
            // Convierte la posición del mouse en un rayo en el espacio de la cámara
            ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        }

        // Detectar entrada táctil
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            // Convierte la posición del toque en un rayo en el espacio de la cámara
            ray = Camera.main.ScreenPointToRay(Touchscreen.current.primaryTouch.position.ReadValue());
        }

        
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
            if (currentCard.cardSO is HeroCardSO && fieldPosition.PositionIndex != -1)
            {
                StartCoroutine(currentCard.MoveToPosition(fieldPosition.transform.position + Vector3.up, Card.cardMovementSpeed, true, false));
                currentCard.RotateToAngle(new Vector3(90, 0, 0), Card.cardMovementSpeed, true);
            }
            else if(currentCard.cardSO is SpellCardSO && fieldPosition == playerManager.SpellFieldPosition)
            {
                StartCoroutine(currentCard.MoveToPosition(fieldPosition.transform.position + Vector3.up, Card.cardMovementSpeed, true, false));
                currentCard.RotateToAngle(new Vector3(90, 0, 0), Card.cardMovementSpeed, true);
            }
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

        if (duelManager.IsSinglePlayer)
        {
            duelManager.PlaceCardInField(duelManager.Player1Manager, true, handCardHandler.GetIdexOfCard(currentCard),currentFieldPosition.PositionIndex);
        }
        else
        {
            duelManager.PlaceCardOnFieldServerRpc(
                handCardHandler.GetIdexOfCard(currentCard),
                currentFieldPosition.PositionIndex,
                NetworkManager.Singleton.LocalClientId);
        }

        currentFieldPosition = null;

        if(card.cardSO is HeroCardSO)
        {
            handCardHandler.ShowHandCard();
        }
        else if(card.cardSO is SpellCardSO)
        {
            handCardHandler.HideHandCard();
        }
    }
}

